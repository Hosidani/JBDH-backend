from rest_framework.views import exception_handler
from rest_framework.response import Response
from rest_framework import status
import logging

logger = logging.getLogger(__name__)

def custom_exception_handler(exc, context):
    # Anropa DRF:s standardhanterare först (hanterar t.ex. 401 Unauthorized)
    response = exception_handler(exc, context)

    # Om DRF inte kände igen felet, kolla dina egna Job-exceptions
    if response is None:
        from apps.jobs.services.job_interaction_service import (
            ResourceNotFoundError,
            SavedJobAlreadyExistsError,
            NotInterestedAlreadyExistsError,
            InvalidInputError
        )

        if isinstance(exc, ResourceNotFoundError):
            return Response({"detail": str(exc)}, status=status.HTTP_404_NOT_FOUND)
        
        if isinstance(exc, (SavedJobAlreadyExistsError, NotInterestedAlreadyExistsError)):
            return Response({"detail": str(exc)}, status=status.HTTP_409_CONFLICT)
        
        if isinstance(exc, InvalidInputError):
            return Response({"detail": str(exc)}, status=status.HTTP_400_BAD_REQUEST)

        # ===== NYA: CV/Job exceptions =====
        try:
            try:
                from apps.ai_services.domain.exceptions import CVJobAnalysisError
            except ImportError:
                from apps.ai_services.domain.exceptions import CVJobAnalysisError

            if isinstance(exc, CVJobAnalysisError):
                status_code = getattr(exc, 'status_code', status.HTTP_400_BAD_REQUEST)
                return Response({"detail": str(exc)}, status=status_code)
        except ImportError:
            # Om ai_services.exceptions inte finns än, ignorera tyst
            pass

        # Logga oväntade fel (500)
        logger.error(f"Unexpected error: {exc}", exc_info=True)
        return Response({"detail": "Internal server error"}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

    return response