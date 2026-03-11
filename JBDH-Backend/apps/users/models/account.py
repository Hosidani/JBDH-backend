from django.db import models
from django.utils import timezone


class AccountType(models.TextChoices):
    """Account subscription types"""
    BASIC = 'Basic', 'Basic'
    PREMIUM = 'Premium', 'Premium'


class Account(models.Model):
    """
    Account model for user billing and subscription management.
    One user can have multiple accounts (e.g., personal and business).
    """
    
    user = models.ForeignKey(
        'users.User',
        on_delete=models.CASCADE,
        related_name='accounts',
        help_text='User who owns this account'
    )
    
    # Account details
    account_name = models.CharField(
        max_length=100,
        help_text='Display name for this account'
    )
    account_email = models.EmailField(
        max_length=255,
        help_text='Contact email for this account'
    )
    account_phone = models.CharField(
        max_length=20,
        null=True,
        blank=True,
        help_text='Contact phone for this account'
    )
    
    # Preferences
    preferred_language = models.CharField(
        max_length=10,
        default='en',
        help_text='Preferred language code (e.g., en, sv, es)'
    )
    
    # Subscription
    account_type = models.CharField(
        max_length=20,
        choices=AccountType.choices,
        default=AccountType.BASIC,
        db_index=True,
        help_text='Account subscription type'
    )
    
    # Account status
    is_terminated = models.BooleanField(
        default=False,
        db_index=True,
        help_text='Account has been terminated/closed'
    )
    terminated_at = models.DateTimeField(
        null=True,
        blank=True,
        help_text='When the account was terminated'
    )
    
    # Terms and conditions
    accepted_terms_and_conditions = models.BooleanField(
        default=False,
        help_text='User has accepted T&C'
    )
    terms_accepted_at = models.DateTimeField(
        null=True,
        blank=True,
        help_text='When T&C were accepted'
    )
    
    # Timestamps
    created_at = models.DateTimeField(default=timezone.now, db_index=True)
    updated_at = models.DateTimeField(auto_now=True)
    
    class Meta:
        db_table = 'accounts'
        verbose_name = 'Account'
        verbose_name_plural = 'Accounts'
        ordering = ['-created_at']
        indexes = [
            models.Index(fields=['user', 'is_terminated']),
            models.Index(fields=['account_type']),
            models.Index(fields=['created_at']),
        ]
    
    def __str__(self):
        return f"{self.account_name} ({self.account_email})"
    
    @property
    def is_active(self):
        """Check if account is active (not terminated)"""
        return not self.is_terminated
    
    @property
    def is_premium(self):
        """Check if account has premium subscription"""
        return self.account_type == AccountType.PREMIUM
    
    def terminate(self):
        """Terminate this account"""
        self.is_terminated = True
        self.terminated_at = timezone.now()
        self.save(update_fields=['is_terminated', 'terminated_at'])
    
    def accept_terms(self):
        """Mark terms and conditions as accepted"""
        self.accepted_terms_and_conditions = True
        self.terms_accepted_at = timezone.now()
        self.save(update_fields=['accepted_terms_and_conditions', 'terms_accepted_at'])