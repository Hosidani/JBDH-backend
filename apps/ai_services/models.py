
# --- MVP: ATSResult för sales demo ---
class ATSResult(models.Model):
    from django.conf import settings
    user = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='ats_results')
    cv_text = models.TextField()
    job_text = models.TextField()
    ats_json = models.JSONField()
    overall_score = models.FloatField(null=True)
    created_at = models.DateTimeField(auto_now_add=True)

    class Meta:
        indexes = [models.Index(fields=['user', 'overall_score'])]
        ordering = ['-created_at']

    def __str__(self):
        return f"ATSResult(user={self.user_id}, score={self.overall_score}, created={self.created_at})"
# apps/ai_services/models.py
from django.db import models
from django.utils import timezone
from apps.users.models import User


class UserAIResponse(models.Model):
    """Store AI analysis responses for user CVs"""
    
    user = models.ForeignKey(
        User,
        on_delete=models.CASCADE,
        related_name='ai_responses',
        help_text='User who owns this AI response'
    )
    response_text = models.TextField(
        help_text='Raw JSON response from AI service'
    )
    created_at = models.DateTimeField(
        default=timezone.now,
        db_index=True
    )
    
    class Meta:
        db_table = 'user_ai_responses'
        verbose_name = 'User AI Response'
        verbose_name_plural = 'User AI Responses'
        ordering = ['-created_at']
        indexes = [
            models.Index(fields=['user', '-created_at']),
        ]
    
    def __str__(self):
        return f"AI Response for {self.user.email} at {self.created_at}"
    
    @property
    def analysis(self):
        """Parse and return the CV analysis result"""
        if not self.response_text:
            return None
        
        import json
        try:
            return json.loads(self.response_text)
        except json.JSONDecodeError:
            return None


class JobAIResponse(models.Model):
    class JobStatus(models.TextChoices):
        PENDING = "Pending", "Pending"
        PROCESSING = "Processing", "Processing"
        COMPLETED = "Completed", "Completed"
        FAILED = "Failed", "Failed"

    job_id = models.IntegerField(unique=True)
    response_text = models.TextField(default="")
    status = models.CharField(max_length=20, choices=JobStatus.choices, default=JobStatus.PENDING)
    error_message = models.TextField(blank=True, null=True)
    attempt_count = models.IntegerField(default=0)
    created_at = models.DateTimeField(default=timezone.now, db_index=True)
    updated_at = models.DateTimeField(auto_now=True)

    class Meta:
        db_table = "job_ai_responses"

    def __str__(self):
        return f"JobAIResponse(job_id={self.job_id}, status={self.status})"


class ExternalCV(models.Model):
    """External CV text uploaded by a user for AI comparison"""

    user = models.ForeignKey(
        User,
        on_delete=models.CASCADE,
        related_name='external_cvs',
        help_text='User who owns this external CV',
    )
    name = models.CharField(max_length=255, default='', help_text='Display name for this CV')
    text = models.TextField(help_text='Raw CV text content')
    created_at = models.DateTimeField(default=timezone.now, db_index=True)

    class Meta:
        db_table = 'external_cvs'
        verbose_name = 'External CV'
        verbose_name_plural = 'External CVs'
        ordering = ['-created_at']

    def __str__(self):
        return f"ExternalCV(id={self.pk}, user={self.user.email})"


class ExternalJobAd(models.Model):
    """External job advertisement text uploaded by a user for AI comparison"""

    user = models.ForeignKey(
        User,
        on_delete=models.CASCADE,
        related_name='external_job_ads',
        help_text='User who owns this external job ad',
    )
    name = models.CharField(max_length=255, default='', help_text='Display name for this job ad')
    text = models.TextField(help_text='Raw job advertisement text content')
    created_at = models.DateTimeField(default=timezone.now, db_index=True)

    class Meta:
        db_table = 'external_job_ads'
        verbose_name = 'External Job Ad'
        verbose_name_plural = 'External Job Ads'
        ordering = ['-created_at']

    def __str__(self):
        return f"ExternalJobAd(id={self.pk}, user={self.user.email})"
