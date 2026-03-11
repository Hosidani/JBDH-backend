from django.core.management.base import BaseCommand
from apps.jobs.services.etl_pipeline_service import ETLPipelineService

class Command(BaseCommand):
	help = "Kör Platsbanken ETL snapshot-import."

	def handle(self, *args, **options):
		self.stdout.write("🚀 Starting SNAPSHOT...")
		ETLPipelineService.run_pipeline(source="snapshot")
		self.stdout.write(self.style.SUCCESS("✅ SNAPSHOT completed!"))
