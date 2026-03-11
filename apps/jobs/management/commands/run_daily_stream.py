from django.core.management.base import BaseCommand
from apps.jobs.models import Job
from apps.jobs.services.etl_pipeline_service import ETLPipelineService

class Command(BaseCommand):
    help = "Kör Platsbanken ETL stream-import från senaste posted_date."

    def handle(self, *args, **options):
        latest_job = Job.objects.order_by('-posted_date').first()
        if latest_job and latest_job.posted_date:
            date_str = latest_job.posted_date.strftime('%Y-%m-%d')
        else:
            date_str = None

        if date_str:
            self.stdout.write(f"🚀 STREAM from {date_str}...")
        else:
            self.stdout.write("🚀 STREAM from beginning...")

        ETLPipelineService.run_pipeline(source="stream", date=date_str)
        self.stdout.write(self.style.SUCCESS("✅ STREAM completed!"))
