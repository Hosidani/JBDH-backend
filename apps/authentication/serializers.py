from rest_framework import serializers
from apps.users.models import User, Account, UserRole, AccountType
from django.contrib.auth.password_validation import validate_password
from django.core.exceptions import ValidationError as DjangoValidationError

class AnalyzeCVSerializer(serializers.Serializer):
    cv_file = serializers.FileField()
    language = serializers.CharField(required=False)

    
class RegisterUserSerializer(serializers.Serializer):
    email = serializers.CharField(required=True)
    password = serializers.CharField(
        write_only=True,
        required=True,
        min_length=1,
        style={'input_type': 'password'}
    )
    confirm_password = serializers.CharField(
        write_only=True,
        required=True,
        style={'input_type': 'password'}
    )
    location = serializers.CharField(required=False, allow_blank=True)
    open = serializers.BooleanField(default=False)
    preferred_language = serializers.CharField(default='en', max_length=10)
    selected_roles = serializers.ListField(
        child=serializers.CharField(),
        required=False,
        allow_empty=True
    )
    cv_file = serializers.FileField(required=False, allow_null=True)
    
    def validate_email(self, value):
        if User.objects.filter(email=value.lower()).exists():
            raise serializers.ValidationError("This email is already registered.")
        return value.lower()
    
    def validate(self, data):
        if data.get('password') != data.get('confirm_password'):
            raise serializers.ValidationError({
                "password": "Passwords do not match."
            })
        
        # Validate password strength
        #try:
        #    validate_password(data.get('password'))
        #except DjangoValidationError as e:
        #    raise serializers.ValidationError({
        #        "password": list(e.messages)
        #    })
        
        return data
    
    def validate_selected_roles(self, value):
        if value:
            valid_roles = [UserRole.USER, UserRole.RECRUITER, UserRole.MANAGER]
            for role in value:
                if role not in valid_roles:
                    raise serializers.ValidationError(f"Invalid role: {role}")
        return value


class LoginSerializer(serializers.Serializer):
    email = serializers.CharField(required=True)  # Ändrat från EmailField
    password = serializers.CharField(
        write_only=True,
        required=True,
        style={'input_type': 'password'}
    )


class UserSerializer(serializers.ModelSerializer):
    selected_roles = serializers.SerializerMethodField()
    
    class Meta:
        model = User
        fields = [
            'id', 'email', 'location', 'role', 'name',
            'desired_employment_types', 'desired_work_modes',
            'phone_number', 'travel_availability', 'available_from',
            'desired_city', 'work_place', 'competencies', 'cv_path',
            'developer_mode', 'searchable', 'is_banned', 'about_me',
            'owner_id', 'created_at', 'selected_roles'
        ]
        read_only_fields = ['id', 'created_at']
    
    def get_selected_roles(self, obj):
        return list(obj.role_assignments.values_list('role', flat=True))


class AccountSerializer(serializers.ModelSerializer):
    class Meta:
        model = Account
        fields = [
            'id', 'user_id', 'account_name', 'account_email',
            'account_phone', 'preferred_language', 'account_type',
            'is_terminated', 'terminated_at', 'accepted_terms_and_conditions',
            'terms_accepted_at', 'created_at', 'updated_at'
        ]
        read_only_fields = ['id', 'created_at', 'updated_at']


