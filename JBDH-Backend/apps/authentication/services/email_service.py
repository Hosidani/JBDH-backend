from django.core.mail import send_mail
from django.conf import settings
import logging

logger = logging.getLogger(__name__)


class EmailService:
    
    def send_email(self, to_email, subject, body):
        """
        Send email using Django's email backend.
        Returns: "Success" or error message string
        """
        try:
            send_mail(
                subject=subject,
                message=body,
                from_email=settings.DEFAULT_FROM_EMAIL,
                recipient_list=[to_email],
                fail_silently=False,
            )
            logger.info(f"Email sent successfully to {to_email}")
            return "Success"
        except Exception as e:
            logger.error(f"Failed to send email to {to_email}: {str(e)}")
            return str(e)