from functools import wraps

from django.http import JsonResponse
from rest_framework_simplejwt.authentication import JWTAuthentication
from rest_framework_simplejwt.exceptions import InvalidToken, TokenError


def jwt_required(view_func):
    """
    Decorator for plain Django views that need JWT authentication.
    Sets request.user from the Bearer token, identical to how DRF does it
    via DEFAULT_AUTHENTICATION_CLASSES. Use this instead of @api_view when
    the response Content-Type is not JSON (e.g. text/event-stream for SSE).
    """
    @wraps(view_func)
    def wrapper(request, *args, **kwargs):
        try:
            result = JWTAuthentication().authenticate(request)
            if result is None:
                return JsonResponse({'detail': 'Autentisering krävs.'}, status=401)
            request.user = result[0]
        except (InvalidToken, TokenError):
            return JsonResponse({'detail': 'Ogiltig eller utgången token.'}, status=401)
        return view_func(request, *args, **kwargs)
    return wrapper
