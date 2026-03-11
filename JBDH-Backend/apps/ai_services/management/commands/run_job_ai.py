from django.core.management.base import BaseCommand

from apps.ai_services.services.job_ai_analysis_service import JobAIAnalysisService


class Command(BaseCommand):
    # Manual command entrypoint for Job AI processing runs.
    help = "Run Job AI analysis manually with optional keyword filter on job title."

    def add_arguments(self, parser):
        # Batch size controls jobs processed per loop.
        parser.add_argument(
            "--batch-size",
            type=int,
            default=10,
            help="Number of jobs per processing batch (default: 10).",
        )
        # Delay helps avoid aggressive API bursts.
        parser.add_argument(
            "--delay",
            type=int,
            default=3,
            help="Delay in seconds between batches (default: 3).",
        )
        # Optional keyword filter on Job.title for scoped runs.
        parser.add_argument(
            "--keyword",
            type=str,
            default=None,
            help="Optional keyword filter applied to Job.title (icontains).",
        )
        # Optional cap for total number of jobs in a run.
        parser.add_argument(
            "--max-jobs",
            type=int,
            default=None,
            help="Optional hard limit of jobs to process.",
        )
        # Retry mode: process only jobs previously marked FAILED.
        parser.add_argument(
            "--only-failed",
            action="store_true",
            help="Process only jobs with FAILED status in job_ai_responses.",
        )
        # Optional exact job id targeting for frontend/manual single-job analysis.
        parser.add_argument(
            "--job-id",
            type=int,
            default=None,
            help="Process exactly one job by job_id.",
        )
        # By default, process only jobs missing a completed non-empty analysis.
        parser.add_argument(
            "--process-all",
            action="store_true",
            help="Include jobs even if they already have completed analysis.",
        )

    def handle(self, *args, **options):
        # Read command options.
        batch_size = options["batch_size"]
        delay = options["delay"]
        keyword = options["keyword"]
        max_jobs = options["max_jobs"]
        only_failed = options["only_failed"]
        job_id = options["job_id"]
        missing_only = not options["process_all"]

        # Emit start message with full run configuration.
        self.stdout.write(
            self.style.NOTICE(
                f"Starting Job AI run (batch_size={batch_size}, delay={delay}, keyword={keyword}, max_jobs={max_jobs}, only_failed={only_failed}, job_id={job_id}, missing_only={missing_only})"
            )
        )

        # Execute service orchestration.
        service = JobAIAnalysisService()
        service.process_all_jobs_with_ai(
            batch_size=batch_size,
            delay_between_batches_sec=delay,
            keyword=keyword,
            max_jobs=max_jobs,
            only_failed=only_failed,
            target_job_id=job_id,
            missing_only=missing_only,
        )

        # Emit completion message.
        self.stdout.write(self.style.SUCCESS("Job AI run completed."))
