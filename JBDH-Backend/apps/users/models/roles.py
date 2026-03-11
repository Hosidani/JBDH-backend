from django.db import models


class UserRole(models.TextChoices):
    """Available user roles in the system"""
    USER = 'User', 'User'
    RECRUITER = 'Recruiter', 'Recruiter'
    MANAGER = 'Manager', 'Manager'


class UserRoleAssignment(models.Model):
    """
    Many-to-many relationship between users and roles.
    Allows users to have multiple roles simultaneously.
    """
    
    user = models.ForeignKey(
        'users.User',
        on_delete=models.CASCADE,
        related_name='role_assignments',
        help_text='User who has this role'
    )
    
    role = models.CharField(
        max_length=50,
        choices=UserRole.choices,
        help_text='Role assigned to the user'
    )
    
    assigned_at = models.DateTimeField(
        auto_now_add=True,
        help_text='When this role was assigned'
    )
    
    class Meta:
        db_table = 'user_role_assignments'
        verbose_name = 'User Role Assignment'
        verbose_name_plural = 'User Role Assignments'
        unique_together = ('user', 'role')
        indexes = [
            models.Index(fields=['user', 'role']),
            models.Index(fields=['role']),
        ]
    
    def __str__(self):
        return f"{self.user.email} - {self.role}"