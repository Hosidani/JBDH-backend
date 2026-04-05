# apps/ai_services/exceptions.py
from rest_framework import status

class CVJobAnalysisError(Exception):
    """Base exception för CV/Job analys."""
    status_code = status.HTTP_400_BAD_REQUEST
    default_detail = "CV/Job analys misslyckades"

    def __init__(self, detail=None):
        self.detail = detail or self.default_detail
        super().__init__(self.detail)

class MissingRequiredFileError(CVJobAnalysisError):
    default_detail = "CV-fil krävs"

class MissingJobAdTextError(CVJobAnalysisError):
    default_detail = "Jobbannons text krävs"

class TextExtractionError(CVJobAnalysisError):
    default_detail = "Kunde inte extrahera text från CV"

class VectorComparisonError(CVJobAnalysisError):
    default_detail = "Vektorräkning misslyckades"

class AIExtractionError(CVJobAnalysisError):
    status_code = status.HTTP_503_SERVICE_UNAVAILABLE
    default_detail = "AI-tjänst otillgänglig"