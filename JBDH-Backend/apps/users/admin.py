from django.contrib import admin
from django.contrib.auth.admin import UserAdmin as BaseUserAdmin
from apps.users.models import (
    User,
    Account,
    UserRoleAssignment,
    EmailVerificationToken,
    PasswordResetToken
)


@admin.register(User)
class UserAdmin(BaseUserAdmin):
    list_display = ['email', 'name', 'role', 'is_active', 'is_banned', 'created_at']
    list_filter = ['role', 'is_active', 'is_banned', 'created_at']
    search_fields = ['email', 'name']
    ordering = ['-created_at']
    
    fieldsets = (
        (None, {'fields': ('email', 'password')}),
        ('Personal Info', {'fields': ('name', 'location', 'phone_number', 'about_me')}),
        ('Role & Status', {'fields': ('role', 'is_active', 'is_banned')}),
        ('Job Preferences', {'fields': (
            'desired_employment_types', 'desired_work_modes',
            'travel_availability', 'available_from', 'desired_city',
            'work_place', 'competencies', 'searchable'
        )}),
        ('Files', {'fields': ('cv_path',)}),
        ('Permissions', {'fields': ('is_staff', 'is_superuser', 'groups', 'user_permissions')}),
        ('Relationships', {'fields': ('owner',)}),
        ('Metadata', {'fields': ('developer_mode', 'created_at', 'updated_at')}),
    )
    
    readonly_fields = ['created_at', 'updated_at']


@admin.register(Account)
class AccountAdmin(admin.ModelAdmin):
    list_display = ['account_email', 'user', 'account_type', 'is_terminated', 'created_at']
    list_filter = ['account_type', 'is_terminated', 'accepted_terms_and_conditions']
    search_fields = ['account_email', 'account_name', 'user__email']
    ordering = ['-created_at']


@admin.register(UserRoleAssignment)
class UserRoleAssignmentAdmin(admin.ModelAdmin):
    list_display = ['user', 'role']
    list_filter = ['role']
    search_fields = ['user__email']


@admin.register(EmailVerificationToken)
class EmailVerificationTokenAdmin(admin.ModelAdmin):
    list_display = ['user', 'token', 'expiration_date', 'is_used', 'created_at']
    list_filter = ['is_used', 'created_at']
    search_fields = ['user__email', 'token']
    readonly_fields = ['token', 'created_at']
