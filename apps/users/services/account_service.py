from django.contrib.auth import get_user_model
from django.utils import timezone
from django.core.exceptions import ValidationError
from apps.users.models import Account  
from apps.organisations.models import Organisation 
from apps.users.serializers import AccountSettingsSerializer  
User = get_user_model()

class AccountService:
    """Business logic för accounts."""
    
    @staticmethod
    def delete_user_account(user):
        """Radera user + relaterade data."""
        user.delete()
        return {"message": "Your account has been deleted."}
    
    @staticmethod
    def get_account_settings(user):
        """Hämta account settings."""
        try:
            account = Account.objects.get(user=user)
        except Account.DoesNotExist:
            raise Account.DoesNotExist("Account not found.")
        return account
    
    @staticmethod
    def get_available_organisations(user):
        """Hämta organisationer user INTE är med i."""
        member_ids = set(user.organisations.values_list('id', flat=True))
        manager_ids = set(user.managed_organisations.values_list('id', flat=True))
        exclude_ids = member_ids | manager_ids
        available_orgs = Organisation.objects.exclude(id__in=exclude_ids)
        return [{'id': org.id, 'name': org.name} for org in available_orgs]
    
    @staticmethod
    def update_account_settings(user, data, partial=True):
        """Uppdatera account."""
        account, created = Account.objects.get_or_create(user=user)
        serializer = AccountSettingsSerializer(account, data=data, partial=partial)
        if serializer.is_valid():
            serializer.save()
            return {"message": "Account updated", "account": serializer.data}
        raise ValidationError(serializer.errors)
    
    @staticmethod
    def terminate_account(user):
        """Terminera account."""
        try:
            account = Account.objects.get(user=user, is_terminated=False)
        except Account.DoesNotExist:
            raise Account.DoesNotExist("No active account found.")
        
        account.is_terminated = True
        account.terminated_at = timezone.now()
        account.save()
        return {"message": "Account terminated"}