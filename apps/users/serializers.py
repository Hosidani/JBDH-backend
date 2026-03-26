from rest_framework import serializers
from apps.users.models import User
from apps.ai_services.models import UserAIResponse
import json
import logging
from .models import Account
from apps.organisations.models import Organisation  # Importera modellen här
from rest_framework_simplejwt.serializers import TokenObtainPairSerializer
from rest_framework import serializers

class MyTokenObtainPairSerializer(TokenObtainPairSerializer):
    def validate(self, attrs):
        data = super().validate(attrs)
        user = self.user
        # Kontrollera om användarens konto är terminerat
        if hasattr(user, 'account') and user.account.is_terminated:
            raise serializers.ValidationError('Account is terminated.')
        return data

class AccountSettingsSerializer(serializers.ModelSerializer):
    userId = serializers.IntegerField(source='user.id')
    accountName = serializers.CharField(source='account_name')
    accountEmail = serializers.CharField(source='account_email')
    accountPhone = serializers.CharField(source='account_phone')
    preferredLanguage = serializers.CharField(source='preferred_language')
    accountType = serializers.SerializerMethodField()
    isTerminated = serializers.BooleanField(source='is_terminated')
    acceptedTermsAndConditions = serializers.BooleanField(source='accepted_terms_and_conditions')
    roles = serializers.ListField(child=serializers.CharField(), required=False, default=list)
    organisations = serializers.SerializerMethodField()

    class Meta:
        model = Account
        fields = [
            'id', 'userId', 'accountName', 'accountEmail', 'accountPhone',
            'preferredLanguage', 'accountType', 'isTerminated',
            'acceptedTermsAndConditions', 'roles', 'organisations'
        ]

    def get_accountType(self, obj):
        # Map enum to int: 0=Basic, 1=Premium
        return 0 if obj.account_type == 'Basic' else 1

    def to_representation(self, instance):
        data = super().to_representation(instance)
        data['roles'] = list(instance.user.role_assignments.values_list('role', flat=True))
        return data

    def update(self, instance, validated_data):
        from apps.users.models.roles import UserRoleAssignment
        roles = validated_data.pop('roles', None)
        instance = super().update(instance, validated_data)
        if roles is not None:
            UserRoleAssignment.objects.filter(user=instance.user).delete()
            for role in roles:
                UserRoleAssignment.objects.get_or_create(user=instance.user, role=role)
        return instance

    def get_organisations(self, obj):
        user = obj.user
        from apps.organisations.models import Organisation
        organisations = Organisation.objects.filter(members=user)
        return [
            {
                "id": org.id,
                "name": org.name,
                "role": "member"  # Anpassa om du har rollinformation
            }
            for org in organisations
        ]


class RegisterUserSerializer(serializers.Serializer):
    """Serializer for user registration"""
    email = serializers.EmailField(required=True)
    password = serializers.CharField(write_only=True, required=True, min_length=1, allow_blank=False)
    confirm_password = serializers.CharField(write_only=True, required=True, allow_blank=False)
    location = serializers.CharField(required=False, allow_blank=True, default='')
    open = serializers.BooleanField(required=False, default=False)
    preferred_language = serializers.CharField(required=False, allow_blank=True, default='en')
    selected_roles = serializers.ListField(
        child=serializers.CharField(),
        required=False,
        allow_empty=True,
        default=list
    )
    cv_file = serializers.FileField(required=False, allow_null=True)
    
    def validate(self, data):
        """Validate registration data"""
        logger = logging.getLogger(__name__)
        logger.info(f"Serializer received data: {data}")
        
        # Check passwords match
        password = data.get('password')
        confirm_password = data.get('confirm_password')
        
        if not password or not confirm_password:
            raise serializers.ValidationError('Password and confirm password are required.')
        
        if password != confirm_password:
            raise serializers.ValidationError({
                'confirm_password': 'Passwords do not match.'
            })
        
        # Check if email already exists
        email = data.get('email')
        if not email:
            raise serializers.ValidationError({'email': 'Email is required.'})
            
        if User.objects.filter(email=email).exists():
            raise serializers.ValidationError({
                'email': 'User with this email already exists.'
            })
        
        return data


class ChangePasswordSerializer(serializers.Serializer):
    """Serializer for changing user password"""
    currentPassword = serializers.CharField()
    newPassword = serializers.CharField()
    confirmNewPassword = serializers.CharField()


class CVAnalysisResultSerializer(serializers.Serializer):
    """Serializer for parsed CV analysis"""
    best_job_titles_for_user = serializers.ListField(
        child=serializers.CharField()
    )
    reasoning_behind_job_titles = serializers.CharField(allow_blank=True)
    hard_skills = serializers.ListField(child=serializers.CharField())
    soft_skills = serializers.ListField(child=serializers.CharField())
    experiences = serializers.ListField(child=serializers.DictField())
    education_and_certifications = serializers.ListField(
        child=serializers.CharField()
    )


class UserAIResponseSerializer(serializers.ModelSerializer):
    """Serializer for AI Response with parsed analysis"""
    analysis = serializers.SerializerMethodField()
    
    class Meta:
        model = UserAIResponse
        fields = ['id', 'user_id', 'response_text', 'created_at', 'analysis']
        read_only_fields = ['id', 'created_at']
    
    def get_analysis(self, obj):
        """Parse and return analysis object"""
        if not obj.response_text:
            return None
        
        try:
            # Handle potential JSON escaping issues
            raw_text = obj.response_text
            
            # Remove code block markers if present
            if raw_text.startswith('```'):
                lines = raw_text.split('\n')
                raw_text = '\n'.join(lines[1:-1]) if len(lines) > 2 else raw_text
            
            # Try to parse the full JSON first
            try:
                parsed = json.loads(raw_text)
                return parsed
            except json.JSONDecodeError as e:
                # If full JSON parsing fails (e.g., truncated response), try to extract just the English section
                # The AI response structure is: {"English": {...}, "Swedish": {...}}
                logger = logging.getLogger(__name__)
                logger.warning(f"Full JSON parsing failed: {e}. Attempting to extract English section.")
                
                # Try to find and extract the English section if it's complete
                english_start = raw_text.find('"English"')
                if english_start != -1:
                    # Find the start of the English object
                    english_obj_start = raw_text.find('{', english_start)
                    if english_obj_start != -1:
                        # Try to find a complete English object by counting braces
                        brace_count = 0
                        english_end = english_obj_start
                        for i in range(english_obj_start, len(raw_text)):
                            if raw_text[i] == '{':
                                brace_count += 1
                            elif raw_text[i] == '}':
                                brace_count -= 1
                                if brace_count == 0:
                                    english_end = i + 1
                                    break
                        
                        if brace_count == 0 and english_end > english_obj_start:
                            # We found a complete English object
                            english_json = raw_text[english_obj_start:english_end]
                            try:
                                english_data = json.loads(english_json)
                                logger.info("Successfully extracted English section from truncated JSON")
                                return {"English": english_data}
                            except json.JSONDecodeError:
                                pass
                
                # If we can't extract a partial structure, return None
                logger.error(f"Could not parse JSON or extract partial data. Error: {e}")
                return None
        except Exception as e:
            logger = logging.getLogger(__name__)
            logger.error(f"Unexpected error parsing analysis: {e}")
            return None


class PublicUserProfileSerializer(serializers.ModelSerializer):
    """Serializer for viewing other users' profiles (limited data)"""
    has_cv = serializers.SerializerMethodField()
    
    class Meta:
        model = User
        fields = [
            'id', 'name', 'email', 'location', 'about_me',
            'has_cv', 'created_at'
        ]
        read_only_fields = fields
    
    def get_has_cv(self, obj):
        return bool(obj.cv_path)


class MyProfileSerializer(serializers.ModelSerializer):
    """Serializer for user's own profile (full data)"""
    selectedRoles = serializers.SerializerMethodField()
    aiResponses = serializers.SerializerMethodField()
    ownedUsers = serializers.SerializerMethodField()
    owner = serializers.SerializerMethodField()
    phoneNumber = serializers.CharField(source='phone_number', allow_null=True, required=False)
    travelAvailability = serializers.BooleanField(source='travel_availability', required=False)
    desiredWorkModes = serializers.ListField(source='desired_work_modes', required=False, allow_null=True)
    desiredEmploymentTypes = serializers.ListField(source='desired_employment_types', required=False, allow_null=True)
    desiredCity = serializers.CharField(source='desired_city', allow_null=True, required=False)
    desiredEmploymentType = serializers.CharField(source='desired_job_title', allow_null=True, required=False)
    createdAt = serializers.DateTimeField(source='created_at', read_only=True)

    class Meta:
        model = User
        fields = [
            'id', 'email', 'location', 'name',
            'desiredEmploymentTypes', 'desiredWorkModes',
            'phoneNumber', 'travelAvailability', 'available_from',
            'desiredCity', 'work_place', 'competencies', 'cv_path',
            'developer_mode', 'searchable', 'is_banned', 'role',
            'selectedRoles', 'owner_id', 'about_me', 'createdAt',
            'aiResponses', 'ownedUsers', 'owner', 'desiredEmploymentType'
        ]
        read_only_fields = ['id', 'createdAt', 'is_banned']

    def get_selectedRoles(self, obj):
        return list(obj.role_assignments.values_list('role', flat=True))

    def get_aiResponses(self, obj):
        responses = obj.ai_responses.all()
        return UserAIResponseSerializer(responses, many=True).data

    def get_ownedUsers(self, obj):
        owned = obj.owned_users.all()
        return [{'id': u.id, 'name': u.name} for u in owned]

    def get_owner(self, obj):
        if obj.owner:
            return {'id': obj.owner.id, 'name': obj.owner.name}
        return None


class UserSettingsSerializer(serializers.ModelSerializer):
    email = serializers.CharField(required=True)
    searchable = serializers.BooleanField(required=False)
    travelAvailability = serializers.BooleanField(source='travel_availability', required=False)
    desiredWorkModes = serializers.ListField(
        source='desired_work_modes',
        child=serializers.ChoiceField(choices=["På plats", "Hybrid", "Distans"]),
        required=False
    )
    desiredCity = serializers.CharField(source='desired_city', allow_null=True, required=False, allow_blank=True)
    desiredEmploymentTypes = serializers.ListField(source='desired_employment_types', required=False)
    desiredEmploymentType = serializers.CharField(source='desired_job_title', allow_null=True, required=False, allow_blank=True)
    location = serializers.CharField(required=False, allow_blank=True)

    class Meta:
        model = User
        fields = [
            'name', 'email', 'phone_number', 'location',
            'searchable', 'travelAvailability', 'desiredWorkModes',
            'desiredCity', 'desiredEmploymentTypes', 'desiredEmploymentType',
        ]
        extra_kwargs = {
            'name': {'required': False},
            'email': {'required': True},
            'phone_number': {'required': False, 'allow_null': True, 'allow_blank': True},
        }

# Så här kan du förhindra inloggning för terminerade konton i Django REST Framework + SimpleJWT:

# 1. Skapa en egen TokenObtainPairSerializer:
from rest_framework_simplejwt.serializers import TokenObtainPairSerializer
from rest_framework import serializers

class MyTokenObtainPairSerializer(TokenObtainPairSerializer):
    def validate(self, attrs):
        data = super().validate(attrs)
        user = self.user
        # Kontrollera om användarens konto är terminerat
        if hasattr(user, 'account') and user.account.is_terminated:
            raise serializers.ValidationError('Account is terminated.')
        return data

# 2. Skapa en egen TokenObtainPairView:
from rest_framework_simplejwt.views import TokenObtainPairView

class MyTokenObtainPairView(TokenObtainPairView):
    serializer_class = MyTokenObtainPairSerializer

# 3. I din urls.py, byt ut login-endpointen:
# path('auth/login/', MyTokenObtainPairView.as_view(), name='token_obtain_pair'),

# Nu kommer användare med terminerat konto inte kunna logga in!