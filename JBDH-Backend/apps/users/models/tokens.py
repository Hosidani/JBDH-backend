import uuid
from django.db import models
from django.utils import timezone


class EmailVerificationToken(models.Model):
    """
    Token for email verification during user registration.
    Expires after 24 hours.
    """
    
    id = models.UUIDField(
        primary_key=True,
        default=uuid.uuid4,
        editable=False,
        help_text='Unique token identifier'
    )
    
    token = models.UUIDField(
        default=uuid.uuid4,
        unique=True,
        db_index=True,
        editable=False,
        help_text='Verification token sent to user'
    )
    
    user = models.ForeignKey(
        'users.User',
        on_delete=models.CASCADE,
        related_name='verification_tokens',
        help_text='User who needs to verify email'
    )
    
    expiration_date = models.DateTimeField(
        help_text='When this token expires'
    )
    
    is_used = models.BooleanField(
        default=False,
        db_index=True,
        help_text='Token has been used'
    )
    
    created_at = models.DateTimeField(
        default=timezone.now,
        db_index=True
    )
    
    class Meta:
        db_table = 'email_verification_tokens'
        verbose_name = 'Email Verification Token'
        verbose_name_plural = 'Email Verification Tokens'
        ordering = ['-created_at']
        indexes = [
            models.Index(fields=['token']),
            models.Index(fields=['user', 'is_used']),
            models.Index(fields=['expiration_date']),
        ]
    
    def __str__(self):
        return f"Verification token for {self.user.email}"
    
    def is_valid(self):
        """Check if token is valid (not used and not expired)"""
        return not self.is_used and self.expiration_date > timezone.now()
    
    def mark_as_used(self):
        """Mark token as used"""
        self.is_used = True
        self.save(update_fields=['is_used'])
    
    @property
    def is_expired(self):
        """Check if token has expired"""
        return self.expiration_date <= timezone.now()


class PasswordResetToken(models.Model):
    """
    Token for password reset functionality.
    Expires after 1 hour.
    """
    
    id = models.UUIDField(
        primary_key=True,
        default=uuid.uuid4,
        editable=False,
        help_text='Unique token identifier'
    )
    
    token = models.UUIDField(
        default=uuid.uuid4,
        unique=True,
        db_index=True,
        editable=False,
        help_text='Password reset token sent to user'
    )
    
    user = models.ForeignKey(
        'users.User',
        on_delete=models.CASCADE,
        related_name='password_reset_tokens',
        help_text='User who requested password reset'
    )
    
    expiration_date = models.DateTimeField(
        help_text='When this token expires'
    )
    
    is_used = models.BooleanField(
        default=False,
        db_index=True,
        help_text='Token has been used'
    )
    
    created_at = models.DateTimeField(
        default=timezone.now,
        db_index=True
    )
    
    class Meta:
        db_table = 'password_reset_tokens'
        verbose_name = 'Password Reset Token'
        verbose_name_plural = 'Password Reset Tokens'
        ordering = ['-created_at']
        indexes = [
            models.Index(fields=['token']),
            models.Index(fields=['user', 'is_used']),
            models.Index(fields=['expiration_date']),
        ]
    
    def __str__(self):
        return f"Password reset token for {self.user.email}"
    
    def is_valid(self):
        """Check if token is valid (not used and not expired)"""
        return not self.is_used and self.expiration_date > timezone.now()
    
    def mark_as_used(self):
        """Mark token as used"""
        self.is_used = True
        self.save(update_fields=['is_used'])
    
    @property
    def is_expired(self):
        """Check if token has expired"""
        return self.expiration_date <= timezone.now()