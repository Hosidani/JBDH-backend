# apps/organisations/models.py
from django.db import models
from apps.users.models import User

class Organisation(models.Model):
    name = models.CharField(max_length=100)
    managers = models.ManyToManyField(User, related_name='managed_organisations')
    members = models.ManyToManyField(User, related_name='organisations', blank=True)

    def __str__(self):
        return self.name
