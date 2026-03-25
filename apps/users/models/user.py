import uuid
from django.db import models
from django.contrib.auth.models import AbstractBaseUser, BaseUserManager, PermissionsMixin
from django.utils import timezone
from .roles import UserRole


class UserManager(BaseUserManager):
    """Custom user manager for email-based authentication"""
    
    def create_user(self, email, password=None, **extra_fields):
        """Create and return a regular user with email and password"""
        if not email:
            raise ValueError('Email is required')
        
        email = self.normalize_email(email)
        user = self.model(email=email, **extra_fields)
        user.set_password(password)
        user.save(using=self._db)
        return user

    def create_superuser(self, email, password=None, **extra_fields):
        """Create and return a superuser with email and password"""
        extra_fields.setdefault('is_staff', True)
        extra_fields.setdefault('is_superuser', True)
        extra_fields.setdefault('is_active', True)
        
        if extra_fields.get('is_staff') is not True:
            raise ValueError('Superuser must have is_staff=True.')
        if extra_fields.get('is_superuser') is not True:
            raise ValueError('Superuser must have is_superuser=True.')
        
        return self.create_user(email, password, **extra_fields)


class User(AbstractBaseUser, PermissionsMixin):
    """
    Custom user model using email as the unique identifier.
    Supports multiple roles and comprehensive profile information.
    """
    
    # Unique identifier
    id = models.AutoField(primary_key=True)
    
    # Authentication
    email = models.EmailField(
        unique=True,
        max_length=255,
        db_index=True,
        help_text='Email address used for login'
    )
    
    # Basic info
    name = models.CharField(max_length=255, null=True, blank=True)
    location = models.CharField(max_length=255, blank=True, default='')
    phone_number = models.CharField(max_length=20, null=True, blank=True)
    about_me = models.TextField(null=True, blank=True)
    
    # Role (primary discriminator)
    role = models.CharField(
        max_length=50,
        choices=UserRole.choices,
        default=UserRole.USER,
        db_index=True,
        help_text='Primary user role'
    )
    
    # Job preferences - JSON fields for flexibility
    desired_employment_types = models.JSONField(
        null=True,
        blank=True,
        default=list,
        help_text='List of desired employment types (e.g., ["Full-time", "Part-time"])'
    )
    desired_work_modes = models.JSONField(
        null=True,
        blank=True,
        default=list,
        help_text='List of desired work modes (e.g., ["Remote", "Hybrid"])'
    )
    
    # Job search details
    desired_job_title = models.CharField(max_length=255, null=True, blank=True)
    travel_availability = models.BooleanField(default=False)
    available_from = models.DateTimeField(null=True, blank=True)
    desired_city = models.CharField(max_length=255, null=True, blank=True)
    work_place = models.CharField(max_length=255, null=True, blank=True)
    competencies = models.TextField(
        null=True,
        blank=True,
        help_text='User skills and competencies'
    )
    
    # Files
    cv_path = models.CharField(
        max_length=500,
        null=True,
        blank=True,
        help_text='Path to uploaded CV file'
    )
    
    # User preferences and status
    developer_mode = models.BooleanField(
        default=False,
        null=True,
        blank=True,
        help_text='Enable developer/debug features'
    )
    searchable = models.BooleanField(
        default=False,
        null=True,
        blank=True,
        help_text='Make profile searchable by recruiters'
    )
    is_banned = models.BooleanField(
        default=False,
        db_index=True,
        help_text='User is banned from the platform'
    )
    
    # Hierarchical relationship (for recruiter-owned users)
    owner = models.ForeignKey(
        'self',
        on_delete=models.SET_NULL,
        null=True,
        blank=True,
        related_name='owned_users',
        help_text='Recruiter or manager who owns this user'
    )
    
    # Django auth fields
    is_active = models.BooleanField(
        default=False,
        help_text='User has verified their email'
    )
    is_staff = models.BooleanField(
        default=False,
        help_text='User can access admin site'
    )
    is_superuser = models.BooleanField(
        default=False,
        help_text='User has all permissions'
    )
    
    # Timestamps
    created_at = models.DateTimeField(default=timezone.now, db_index=True)
    updated_at = models.DateTimeField(auto_now=True)
    
    # Manager
    objects = UserManager()
    
    # Auth configuration
    USERNAME_FIELD = 'email'
    EMAIL_FIELD = 'email'
    REQUIRED_FIELDS = []  # Email is already required as USERNAME_FIELD
    
    class Meta:
        db_table = 'users'
        verbose_name = 'User'
        verbose_name_plural = 'Users'
        ordering = ['-created_at']
        indexes = [
            models.Index(fields=['email']),
            models.Index(fields=['role']),
            models.Index(fields=['is_active', 'is_banned']),
            models.Index(fields=['created_at']),
        ]
    
    def __str__(self):
        return self.email
    
    def get_full_name(self):
        """Return the user's full name or email if name is not set"""
        return self.name if self.name else self.email
    
    def get_short_name(self):
        """Return the user's short name (first name) or email"""
        if self.name:
            return self.name.split()[0] if ' ' in self.name else self.name
        return self.email.split('@')[0]
    
    @property
    def is_verified(self):
        """Check if user has verified their email"""
        return self.is_active
    
    @property
    def can_login(self):
        """Check if user is allowed to login"""
        return self.is_active and not self.is_banned
    
    def has_role(self, role_name):
        """Check if user has a specific role"""
        return self.role_assignments.filter(role=role_name).exists()
    
    def get_roles(self):
        """Get list of all assigned roles"""
        return list(self.role_assignments.values_list('role', flat=True))