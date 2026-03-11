import requests
import json
import ijson
from datetime import datetime
from django.utils import timezone
from apps.jobs.models import Job, RawJob
import pprint
import logging
import traceback
import os
from collections import defaultdict
from decimal import Decimal
from core.utils import vectorize_job_data

logging.basicConfig(
    level=logging.INFO,  # Byt till DEBUG för ännu mer info
    format="%(asctime)s %(levelname)s %(name)s: %(message)s"
)
logger = logging.getLogger(__name__)

class ETLPipelineService:
    STREAM_URL = "https://jobstream.api.jobtechdev.se/stream"
    SNAPSHOT_URL = "https://jobstream.api.jobtechdev.se/snapshot"

    BATCH_SIZE = 3000
    PROGRESS_FILE = "etl_progress.json"

    @staticmethod
    def get_progress():
        if os.path.exists(ETLPipelineService.PROGRESS_FILE):
            with open(ETLPipelineService.PROGRESS_FILE, "r", encoding="utf-8") as f:
                return json.load(f)
        return {"processed": 0, "total": 0, "last_batch": 0}

    @staticmethod
    def save_progress(processed, total, last_batch):
        progress = {"processed": processed, "total": total, "last_batch": last_batch}
        with open(ETLPipelineService.PROGRESS_FILE, "w", encoding="utf-8") as f:
            json.dump(progress, f, ensure_ascii=False, indent=2)

    @staticmethod
    def get_latest_timestamp_from_file(json_file):
        if not os.path.exists(json_file):
            return None
        with open(json_file, "r", encoding="utf-8") as f:
            data = json.load(f)
        timestamps = []
        for job in data:
            ts = job.get("timestamp") or job.get("publication_date")
            if ts:
                # Om ts är ett heltal (Unix-timestamp i ms)
                if isinstance(ts, int) or (isinstance(ts, str) and ts.isdigit()):
                    # Konvertera till sekund och ISO-sträng
                    ts = datetime.fromtimestamp(int(ts) / 1000.0).strftime('%Y-%m-%dT%H:%M:%S')
                # Om ts är ISO-sträng, ta bara första 19 tecken
                elif isinstance(ts, str):
                    ts = ts[:19]
                timestamps.append(ts)
        if not timestamps:
            return None
        # Returnera senaste (max) timestamp som ISO-sträng
        return max(timestamps)
    
    @classmethod
    def download_snapshot_or_stream_to_file(cls, filename, source="snapshot", date=None, max_jobs=None):
        # NYTT: Skippa download om progress redan finns
        progress = cls.get_progress()

        # only skip download if DB raw table actually has data
        if (
            progress.get("total", 0) > 0
            and os.path.exists(filename)
            and RawJob.objects.exists()
        ):
            logger.info(f"⏭️ Skippar download - {progress['processed']}/{progress['total']} redan processade")
            return progress.get("total", 0)

        if source == "snapshot":
            url = cls.SNAPSHOT_URL
        else:
            url = cls.STREAM_URL
            if date:
                url += f"?date={date}"

        logger.info(f"🔎 Hämtar källa: {source.upper()} | URL: {url} | Fil: {filename}")
        print(f"📥 Laddar {source} till {filename}...")

        response = requests.get(url, timeout=1800, stream=False)
        response.raise_for_status()
        data = response.json()

        if max_jobs:
            data = data[:max_jobs]

        with open(filename, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)

        logger.info(f"✅ {source.upper()} sparad till {filename} ({len(data)} jobb)")
        print(f"✅ {source} sparad till {filename} ({len(data)} jobb)")
        return len(data)

    # 1️⃣ EXTRACT - Spara RAW i RawJob
    @staticmethod
    def _normalize_json(value):
        if isinstance(value, Decimal):
            return int(value) if value % 1 == 0 else float(value)
        if isinstance(value, dict):
            return {k: ETLPipelineService._normalize_json(v) for k, v in value.items()}
        if isinstance(value, list):
            return [ETLPipelineService._normalize_json(v) for v in value]
        return value

    @staticmethod
    def extract(json_file, max_jobs=None):
        print(f"📂 Streaming extract i batchar om {ETLPipelineService.BATCH_SIZE} från {json_file}")
        total_saved = 0
        batch = []

        with open(json_file, "rb") as f:
            for job in ijson.items(f, "item"):
                job = ETLPipelineService._normalize_json(job)
                job_id = job.get("id")
                if not job_id:
                    continue

                batch.append(RawJob(job_id=job_id, raw_data=job))

                if len(batch) >= ETLPipelineService.BATCH_SIZE:
                    RawJob.objects.bulk_create(batch, update_conflicts=True, update_fields=["raw_data"], unique_fields=["job_id"])
                    total_saved += len(batch)
                    print(f"💾 Extract saved: {total_saved}")
                    batch.clear()

                if max_jobs and total_saved >= max_jobs:
                    break

        if batch:
            RawJob.objects.bulk_create(batch, update_conflicts=True, update_fields=["raw_data"], unique_fields=["job_id"])
            total_saved += len(batch)

        print(f"✅ [EXTRACT] klart: {total_saved}")
        return total_saved

    # 2️⃣ TRANSFORM - Körs ALLTID på RawJob (oberoende av extract)
    @staticmethod
    def transform_batch(start_idx, batch_size):
        print(f"🔄 [BATCH TRANSFORM] {start_idx} → {start_idx + batch_size}")

        queryset = RawJob.objects.order_by("job_id")[start_idx:start_idx + batch_size]
        transformed_jobs = []
        seen_job_ids = set()
        skip = defaultdict(int)

        for rawjob in queryset:
            raw_data = rawjob.raw_data
            job_id = rawjob.job_id

            required_fields = ["headline", "employer", "publication_date"]
            if not all(raw_data.get(f) for f in required_fields):
                skip["missing_required_fields"] += 1
                continue

            if not isinstance(raw_data.get("publication_date"), str):
                skip["bad_publication_date_type"] += 1
                continue

            if job_id in seen_job_ids:
                skip["duplicate_in_batch"] += 1
                continue
            seen_job_ids.add(job_id)

            description = raw_data.get("description", {}).get("text", "")
            if not description or len(description.strip()) < 20:
                skip["short_or_missing_description"] += 1
                continue

            location = raw_data.get("workplace_address", {}).get("municipality", "")
            if not location:
                skip["missing_location"] += 1
                continue

            try:
                datetime.fromisoformat(raw_data["publication_date"].replace('Z', '+00:00'))
            except Exception:
                skip["bad_publication_date_format"] += 1
                continue

            job_data = ETLPipelineService._transform_single_job(raw_data)
            job_data["job_id"] = job_id

            text_vector = vectorize_job_data(job_data)
            job_data["text_vector"] = text_vector
            if text_vector:
                logger.debug("embedding skapad för job_id=%s (dim=%s)", job_id, len(text_vector))
            else:
                logger.debug("ingen embedding skapad för job_id=%s", job_id)

            transformed_jobs.append(job_data)

        logger.info(
            "🔍 transform debug | slice=%s..%s | raw_in_slice=%s | transformed=%s | skipped=%s",
            start_idx, start_idx + batch_size, len(queryset), len(transformed_jobs), dict(skip)
        )
        return transformed_jobs

    @staticmethod
    def _transform_single_job(raw_data):
        """Hjälpfunktion: raw_data → Job dictionary"""
        # Hantera datum
        raw_date = raw_data.get("publication_date")
        posted_date = None
        if raw_date:
            try:
                dt = datetime.fromisoformat(raw_date.replace('Z', '+00:00'))
                posted_date = timezone.make_aware(dt) if dt.tzinfo is None else dt
            except:
                pass

        # Hantera application_deadline
        deadline_str = raw_data.get("application_deadline")
        application_deadline = None
        if deadline_str:
            try:
                # Hantera ISO-format med eller utan tidszon
                if deadline_str.endswith('Z'):
                    dt = datetime.fromisoformat(deadline_str.replace('Z', '+00:00'))
                else:
                    dt = datetime.fromisoformat(deadline_str)
                # Gör naiv datetime aware om den saknar tidszon
                if dt.tzinfo is None:
                    application_deadline = timezone.make_aware(dt)
                else:
                    application_deadline = dt
            except Exception:
                application_deadline = None

        return {
            "title": raw_data.get("headline", ""),
            "description": raw_data.get("description", {}).get("text", "") if raw_data.get("description") else "",
            "company": raw_data.get("employer", {}).get("name", "") if raw_data.get("employer") else "",
            "location": raw_data.get("workplace_address", {}).get("municipality", "") if raw_data.get("workplace_address") else "",
            "posted_date": posted_date,
            "webpage_url": raw_data.get("webpage_url"),
            "work_type": raw_data.get("working_hours_type", {}).get("label", "") if raw_data.get("working_hours_type") else "",
            "employment_type": raw_data.get("employment_type", {}).get("label", "") if raw_data.get("employment_type") else "",
            "application_deadline": application_deadline,
            "required_skills": json.dumps({
                "needs": raw_data.get("needs", []),
                "requirements": raw_data.get("requirements", [])
            })
        }

    # 3️⃣ LOAD - Minimal (inget att krascha på)
    @staticmethod
    def load(transformed_jobs):
        print("🏗️  [LOAD] Sparar transformerade jobb i Job-tabellen...")
        saved = 0
        for job_data in transformed_jobs:
            job_data = dict(job_data)
            job_id = job_data.pop("job_id")
            Job.objects.update_or_create(
                job_id=job_id,
                defaults=job_data
            )
            saved += 1
        print(f"✅ [LOAD] Klart! Sparade {saved} jobb.")
        return saved

    @classmethod
    def run_pipeline_batched(cls, batch_size=3000):
        """🚀 HUVUDMETOD: Processa batchvis med resume"""
        print("🚀 BATCHED ETL med Memory Control!")
        batch_size = batch_size or cls.BATCH_SIZE

        total_raw = cls.download_snapshot_or_stream_to_file("Rawjobs.json", source="snapshot", date=None)
        progress = cls.get_progress()
        processed = progress.get("processed", 0)

        raw_count = RawJob.objects.count()
        job_count = Job.objects.count()
        jobs_with_vector_count = Job.objects.exclude(text_vector__isnull=True).exclude(text_vector=[]).count()
        logger.info(
            "🧭 state before batch | processed=%s total_raw=%s raw_count=%s job_count=%s jobs_with_vector=%s",
            processed, total_raw, raw_count, job_count, jobs_with_vector_count
        )

        # CRITICAL FIX: ensure RawJob is populated before transform
        if raw_count == 0:
            logger.info("ℹ️ RawJob är tom - kör EXTRACT från fil...")
            extracted_count = cls.extract("Rawjobs.json")
            raw_count = RawJob.objects.count()
            logger.info("ℹ️ Extract result | extracted=%s raw_count_after=%s", extracted_count, raw_count)

        if raw_count == 0:
            logger.error("❌ RawJob fortfarande tom efter extract - avbryter batch")
            return

        if total_raw == 0:
            total_raw = raw_count
            logger.warning("⚠️ total_raw var 0, fallback till raw_count=%s", total_raw)

        if processed > raw_count:
            logger.warning("⚠️ Ogiltig progress (%s) > RawJob-count (%s) - reset till 0", processed, raw_count)
            processed = 0
            cls.save_progress(0, total_raw, 0)

        if processed >= total_raw:
            if (job_count == 0 and raw_count > 0) or (job_count > 0 and jobs_with_vector_count == 0 and raw_count > 0):
                logger.warning(
                    "⚠️ Progress säger klart men data behöver backfill (job_count=%s, jobs_with_vector=%s) - reset till 0",
                    job_count,
                    jobs_with_vector_count,
                )
                processed = 0
                cls.save_progress(0, total_raw, 0)
            else:
                print("✅ ALLT klart!")
                return

        batch_start = processed
        batch_jobs = cls.transform_batch(batch_start, batch_size)
        saved = cls.load(batch_jobs)

        new_processed = min(processed + batch_size, total_raw)
        cls.save_progress(new_processed, total_raw, batch_start)

        pct = (new_processed / total_raw) * 100 if total_raw else 100
        logger.info("✅ batch done | saved=%s processed=%s/%s (%.1f%%)", saved, new_processed, total_raw, pct)
        print(f"✅ BATCH KLAR: {new_processed}/{total_raw} ({pct:.1f}%)")

    @classmethod
    def run_pipeline(cls, source=None, date=None, max_jobs=None):
        """Kör komplett pipeline: Extract → Transform → Load (crash-proof!)"""
        print("🚀 Startar robust ETL-pipeline!")

        # Kolla om Rawjobs.json finns och hämta senaste timestamp
        latest_timestamp = cls.get_latest_timestamp_from_file("Rawjobs.json")
        if latest_timestamp:
            source = "stream"
            date = latest_timestamp
            logger.info(f"🔁 Kör stream från timestamp: {latest_timestamp}")
        else:
            source = "snapshot"
            logger.info("⏳ Ingen timestamp hittad, kör snapshot.")

        # Ladda ner data
        cls.download_snapshot_or_stream_to_file("Rawjobs.json", source=source, date=date, max_jobs=max_jobs)

        stats = {"extracted": 0, "transformed": 0, "errors": 0}
        
        # 1️⃣ EXTRACT - Kan krascha, men sparar jobb för jobb!
        print("\n🐘 [1/3] EXTRACT...")
        try:
            stats["extracted"] = cls.extract("Rawjobs.json", max_jobs)
            print(f"✅ Extract klar: {stats['extracted']} jobb sparade i RawJob")
        except Exception as e:
            print(f"⚠️  Extract kraschade: {e}")
            print("✅ Fortsätter ändå - transform körs på befintliga RawJob!")
            stats["errors"] += 1
        
        # 2️⃣ TRANSFORM - Körs ALLTID på RawJob (oberoende av extract)
        print("\n🔄 [2/3] TRANSFORM...")
        try:
            transformed_jobs = cls.transform()
            stats["transformed"] = len(transformed_jobs)
            logger.info("✅ Transform klar: %d jobb transformerade", stats["transformed"])
        except Exception as e:
            logger.error("❌ Transform kraschade: %s", e)
            logger.error(traceback.format_exc())
            stats["errors"] += 1
            transformed_jobs = []

        # 3️⃣ LOAD - Minimal (inget att krascha på)
        print("\n🏗️  [3/3] LOAD...")
        try:
            cls.load(transformed_jobs)
            print("✅ Load klar")
        except Exception as e:
            print(f"⚠️  Load kraschade: {e}")
            stats["errors"] += 1
        
        # 📊 SAMMANFATTNING
        print(f"\n🎉 PIPELINE KLAR!")
        print(f"   📈 Extraherade: {stats['extracted']}")
        print(f"   🔄 Transformerade: {stats['transformed']}")
        print(f"   ❌ Fel: {stats['errors']}")
        
        if stats["errors"] == 0:
            print("✅ ALLT lyckades!")
        else:
            print("⚠️  Vissa fel, men pipeline kördes!")
        
        return stats



