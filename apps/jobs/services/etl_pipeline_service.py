import ijson
import logging
import traceback
from collections import defaultdict

from apps.jobs.models import Job, RawJob
from apps.jobs.services.etl_pipeline_helpers import (
    JOB_UPDATE_FIELDS,
    backfill_missing_job_vectors,
    backfill_missing_line_of_business,
    build_job_objects,
    build_vectors,
    download_snapshot_or_stream_to_file,
    flush_raw_batch,
    get_latest_timestamp_from_file,
    get_progress,
    normalize_json,
    save_progress,
    transform_single_job,
    validate_raw_job,
)

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s %(levelname)s %(name)s: %(message)s",
)
logger = logging.getLogger(__name__)

class ETLPipelineService:
    BATCH_SIZE = 3000

    # ──────────────────────────────────────────────────────────────────────────
    # 1️⃣  EXTRACT
    # ──────────────────────────────────────────────────────────────────────────

    @staticmethod
    def extract(json_file: str, max_jobs: int | None = None) -> int:
        """Strömmar json_file och sparar råjobb i RawJob-tabellen."""
        print(f"📂 Streaming extract i batchar om {ETLPipelineService.BATCH_SIZE} från {json_file}")
        total_saved = 0
        batch = []

        with open(json_file, "rb") as f:
            for job in ijson.items(f, "item"):
                job = normalize_json(job)
                job_id = job.get("id")
                if not job_id:
                    continue

                batch.append(RawJob(job_id=job_id, raw_data=job))

                if len(batch) >= ETLPipelineService.BATCH_SIZE:
                    total_saved += flush_raw_batch(batch)
                    print(f"💾 Extract saved: {total_saved}")

                if max_jobs and total_saved >= max_jobs:
                    break

        total_saved += flush_raw_batch(batch)
        print(f"✅ [EXTRACT] klart: {total_saved}")
        return total_saved

    # ──────────────────────────────────────────────────────────────────────────
    # 2️⃣  TRANSFORM
    # ──────────────────────────────────────────────────────────────────────────

    @staticmethod
    def transform_batch(start_idx: int, batch_size: int) -> list:
        """Läser ett slice ur RawJob, validerar och omvandlar till Job-dicts."""
        print(f"🔄 [BATCH TRANSFORM] {start_idx} → {start_idx + batch_size}")

        queryset = RawJob.objects.order_by("job_id")[start_idx:start_idx + batch_size]
        transformed_jobs = []
        seen_job_ids = set()
        skip = defaultdict(int)

        for rawjob in queryset:
            raw_data = rawjob.raw_data
            job_id = rawjob.job_id

            skip_reason = validate_raw_job(raw_data, job_id, seen_job_ids)
            if skip_reason:
                skip[skip_reason] += 1
                continue
            seen_job_ids.add(job_id)

            job_data = transform_single_job(raw_data)
            job_data["job_id"] = job_id
            job_data.update(build_vectors(job_data, raw_data))

            logger.debug(
                "embedding skapad för job_id=%s (dim=%s)" if job_data.get("text_vector") else "ingen embedding skapad för job_id=%s",
                job_id,
                len(job_data["text_vector"]) if job_data.get("text_vector") else "",
            )

            transformed_jobs.append(job_data)

        logger.info(
            "🔍 transform debug | slice=%s..%s | raw_in_slice=%s | transformed=%s | skipped=%s",
            start_idx, start_idx + batch_size, len(queryset), len(transformed_jobs), dict(skip)
        )
        return transformed_jobs

    # ──────────────────────────────────────────────────────────────────────────
    # 3️⃣  LOAD
    # ──────────────────────────────────────────────────────────────────────────

    @staticmethod
    def load(transformed_jobs: list) -> int:
        """Skriver transformerade jobb till Job-tabellen med ett bulk_create."""
        print("🏗️  [LOAD] Sparar transformerade jobb i Job-tabellen...")
        job_objects = build_job_objects(transformed_jobs)
        Job.objects.bulk_create(
            job_objects,
            update_conflicts=True,
            update_fields=JOB_UPDATE_FIELDS,
            unique_fields=["job_id"],
        )
        saved = len(job_objects)
        print(f"✅ [LOAD] Klart! Sparade {saved} jobb.")
        return saved

    # ──────────────────────────────────────────────────────────────────────────
    # Orchestrators
    # ──────────────────────────────────────────────────────────────────────────

    @classmethod
    def run_pipeline_batched(cls, batch_size: int = 3000) -> None:
        """🚀 HUVUDMETOD: Processa batchvis med resume-stöd."""
        print("🚀 BATCHED ETL med Memory Control!")
        batch_size = batch_size or cls.BATCH_SIZE

        latest_timestamp = get_latest_timestamp_from_file("Rawjobs.json")
        if latest_timestamp:
            source, date = "stream", latest_timestamp
            logger.info("🔁 BATCHED kör stream från timestamp: %s", latest_timestamp)
        else:
            source, date = "snapshot", None
            logger.info("⏳ Ingen timestamp hittad för BATCHED, kör snapshot.")

        downloaded_count = download_snapshot_or_stream_to_file("Rawjobs.json", source=source, date=date)
        progress = get_progress()
        processed = progress.get("processed", 0)

        if source == "stream":
            extracted_count = cls.extract("Rawjobs.json")
            logger.info("ℹ️ Stream extract result | extracted=%s", extracted_count)

        raw_count = RawJob.objects.count()
        total_raw = raw_count
        job_count = Job.objects.count()
        jobs_with_vector_count = (
            Job.objects.exclude(text_vector__isnull=True).exclude(text_vector=[]).count()
        )
        logger.info(
            "🧭 state before batch | source=%s downloaded=%s processed=%s "
            "total_raw=%s raw_count=%s job_count=%s jobs_with_vector=%s",
            source, downloaded_count, processed, total_raw,
            raw_count, job_count, jobs_with_vector_count,
        )

        if raw_count == 0:
            logger.info("ℹ️ RawJob är tom - kör EXTRACT från fil...")
            cls.extract("Rawjobs.json")
            raw_count = RawJob.objects.count()
            logger.info("ℹ️ raw_count_after=%s", raw_count)

        if raw_count == 0:
            logger.error("❌ RawJob fortfarande tom efter extract - avbryter batch")
            return

        if total_raw == 0:
            total_raw = raw_count
            logger.warning("⚠️ total_raw var 0, fallback till raw_count=%s", total_raw)

        backfill_missing_line_of_business(limit=min(batch_size, 5000))
        backfill_missing_job_vectors(limit=min(batch_size, 5000))

        if processed > raw_count:
            logger.warning("⚠️ Ogiltig progress (%s) > RawJob-count (%s) - reset", processed, raw_count)
            processed = 0
            save_progress(0, total_raw, 0)

        if processed >= total_raw:
            needs_backfill = (job_count == 0 and raw_count > 0) or (
                job_count > 0 and jobs_with_vector_count == 0 and raw_count > 0
            )
            if needs_backfill:
                logger.warning(
                    "⚠️ Progress säger klart men data behöver backfill "
                    "(job_count=%s, jobs_with_vector=%s) - reset till 0",
                    job_count, jobs_with_vector_count,
                )
                processed = 0
                save_progress(0, total_raw, 0)
            else:
                print("✅ ALLT klart!")
                return

        batch_start = processed
        batch_jobs = cls.transform_batch(batch_start, batch_size)
        saved = cls.load(batch_jobs)

        new_processed = min(processed + batch_size, total_raw)
        save_progress(new_processed, total_raw, batch_start)

        pct = (new_processed / total_raw) * 100 if total_raw else 100
        logger.info("✅ batch done | saved=%s processed=%s/%s (%.1f%%)", saved, new_processed, total_raw, pct)
        print(f"✅ BATCH KLAR: {new_processed}/{total_raw} ({pct:.1f}%)")

    @classmethod
    def run_pipeline(cls, source=None, date=None, max_jobs=None) -> dict:
        """Kör komplett pipeline: Extract → Transform → Load (crash-proof)."""
        print("🚀 Startar robust ETL-pipeline!")

        latest_timestamp = get_latest_timestamp_from_file("Rawjobs.json")
        if latest_timestamp:
            source, date = "stream", latest_timestamp
            logger.info("🔁 Kör stream från timestamp: %s", latest_timestamp)
        else:
            source = "snapshot"
            logger.info("⏳ Ingen timestamp hittad, kör snapshot.")

        download_snapshot_or_stream_to_file("Rawjobs.json", source=source, date=date, max_jobs=max_jobs)

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
            transformed_jobs = cls.transform_batch(start_idx=0, batch_size=3000)
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


