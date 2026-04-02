from django.db import models
from apps.users.models import User
from pgvector.django import VectorField

class RawJob(models.Model):
    job_id = models.CharField(max_length=100, primary_key=True)  # Platsbankens/JobTechs id
    raw_data = models.JSONField(null=True, blank=True)

    def __str__(self):
        return str(self.job_id)

class Job(models.Model):
    # Djangos id används som primärnyckel (skapas automatiskt)
    job_id = models.CharField(max_length=100, unique=True)  # Platsbankens/JobTechs id
    title = models.CharField(max_length=200, null=True, blank=True)
    description = models.TextField(max_length=5000, null=True, blank=True)
    company = models.CharField(max_length=200, null=True, blank=True)
    location = models.CharField(max_length=200, null=True, blank=True)
    posted_date = models.DateTimeField(null=True, blank=True)
    webpage_url = models.CharField(max_length=500, null=True, blank=True)
    work_type = models.CharField(max_length=50, null=True, blank=True)
    employment_type = models.CharField(max_length=50, null=True, blank=True)
    line_of_business = models.CharField(max_length=200, null=True, blank=True)
    application_deadline = models.DateTimeField(null=True, blank=True)
    required_skills = models.CharField(max_length=1000, null=True, blank=True)
    text_vector = models.JSONField(null=True, blank=True)
    text_vector_new = VectorField(dimensions=384, null=True, blank=True)
    recruiter = models.ForeignKey(User, related_name='recruited_jobs', null=True, blank=True, on_delete=models.SET_NULL)
    owner = models.ForeignKey(User, related_name='owned_jobs', null=True, blank=True, on_delete=models.SET_NULL)

    def __str__(self):
        return self.title or f"Job {self.pk}"

class SavedJob(models.Model):
    user = models.ForeignKey(User, on_delete=models.CASCADE, related_name='saved_jobs')
    job = models.ForeignKey('Job', on_delete=models.CASCADE, related_name='saved_by_users')
    saved_at = models.DateTimeField(auto_now_add=True)
    notes = models.TextField(blank=True, null=True)

    class Meta:
        unique_together = ('user', 'job')
        verbose_name = 'Saved Job'
        verbose_name_plural = 'Saved Jobs'

    def __str__(self):
        return f"{self.user} saved {self.job}"

class NotInterestedJob(models.Model):
    user = models.ForeignKey(User, on_delete=models.CASCADE, related_name='not_interested_jobs')
    job = models.ForeignKey('Job', on_delete=models.CASCADE, related_name='not_interested_by_users')
    marked_at = models.DateTimeField(auto_now_add=True)

    class Meta:
        unique_together = ('user', 'job')
        verbose_name = 'Not Interested Job'
        verbose_name_plural = 'Not Interested Jobs'

    def __str__(self):
        return f"{self.user} not interested in {self.job}"

class JobApplication(models.Model):
    user = models.ForeignKey(User, on_delete=models.CASCADE, related_name='job_applications')
    job = models.ForeignKey('Job', on_delete=models.CASCADE, related_name='applications')
    notes = models.TextField(blank=True, null=True)
    applied_at = models.DateTimeField(auto_now_add=True)

    class Meta:
        unique_together = ('user', 'job')
        verbose_name = 'Job Application'
        verbose_name_plural = 'Job Applications'

    def __str__(self):
        return f"{self.user} applied to {self.job}"


