"""
Django settings for JBDH Backend project.
"""

from pathlib import Path
import os

# Build paths inside the project like this: BASE_DIR / 'subdir'.
BASE_DIR = Path(__file__).resolve().parent.parent.parent

# Helper function to get env variables (Docker eller .env)
def get_env(key, default=None, cast=None):
    """Get environment variable from Docker env or .env file"""
    value = os.environ.get(key)
    
    # If not in Docker env, try python-decouple
    if value is None:
        try:
            from decouple import config as decouple_config
            value = decouple_config(key, default=default, cast=cast) if cast else decouple_config(key, default=default)
        except:
            value = default
    
    # Apply cast if needed and value exists
    if value is not None and cast is not None:
        if cast == bool:
            return value in ('True', 'true', '1', True)
        elif cast == int:
            return int(value)
        else:
            return cast(value)
    
    return value

# SECURITY WARNING: keep the secret key used in production secret!
SECRET_KEY = get_env('SECRET_KEY', default='django-insecure-4#+ij*9+b)-1lzq=tp8enn%qj%ywz5ji8u&+9@-fyg4n)#35=c')

# SECURITY WARNING: don't run with debug turned on in production!
DEBUG = get_env('DEBUG', default='True', cast=bool)

ALLOWED_HOSTS = ['*']

# Application definition
INSTALLED_APPS = [
    # Local apps - apps.users MUST be first because of custom User model
    'apps.users',  # FÖRST pga custom User (AUTH_USER_MODEL)!
    
    # Django built-in apps
    'django.contrib.admin',
    'django.contrib.auth',
    'django.contrib.contenttypes',
    'django.contrib.sessions',
    'django.contrib.messages',
    'django.contrib.staticfiles',
    
    # Third party
    'rest_framework',
    'rest_framework_simplejwt',
    'rest_framework_simplejwt.token_blacklist',
    'corsheaders',
    'django_filters',
    
    # Other local apps
    'apps.authentication',
    'apps.jobs',
    'apps.kanban',
    'apps.friendship',
    'apps.messaging',
    'apps.organisations',
    'apps.ai_services',
    'drf_spectacular',
    'drf_yasg',
]

# Custom user model
AUTH_USER_MODEL = 'users.User'

MIDDLEWARE = [
    'corsheaders.middleware.CorsMiddleware',
    'django.middleware.security.SecurityMiddleware',
    'django.contrib.sessions.middleware.SessionMiddleware',
    'django.middleware.common.CommonMiddleware',
    # 'django.middleware.csrf.CsrfViewMiddleware',  # <-- Kommentera bort
    'django.contrib.auth.middleware.AuthenticationMiddleware',
    'django.contrib.messages.middleware.MessageMiddleware',
    'django.middleware.clickjacking.XFrameOptionsMiddleware',
]

ROOT_URLCONF = 'config.urls'

TEMPLATES = [
    {
        'BACKEND': 'django.template.backends.django.DjangoTemplates',
        'DIRS': [BASE_DIR / 'templates'],
        'APP_DIRS': True,
        'OPTIONS': {
            'context_processors': [
                'django.template.context_processors.debug',
                'django.template.context_processors.request',
                'django.contrib.auth.context_processors.auth',
                'django.contrib.messages.context_processors.messages',
            ],
        },
    },
]

WSGI_APPLICATION = 'config.wsgi.application'

# Database
DATABASES = {
    'default': {
        'ENGINE': 'django.db.backends.postgresql',
        'NAME': get_env('DATABASE_NAME', default='jbdh_db'),
        'USER': get_env('DATABASE_USER', default='postgres'),
        'PASSWORD': get_env('DATABASE_PASSWORD', default='ArianAAA123'),
        'HOST': get_env('DATABASE_HOST', default='localhost'), 
        'PORT': get_env('DATABASE_PORT', default=5432, cast=int),
    }
}

# Password validation
AUTH_PASSWORD_VALIDATORS = [
    {
        'NAME': 'django.contrib.auth.password_validation.UserAttributeSimilarityValidator',
    },
    {
        'NAME': 'django.contrib.auth.password_validation.MinimumLengthValidator',
    },
    {
        'NAME': 'django.contrib.auth.password_validation.CommonPasswordValidator',
    },
    {
        'NAME': 'django.contrib.auth.password_validation.NumericPasswordValidator',
    },
]

# Internationalization
LANGUAGE_CODE = 'sv-se'
TIME_ZONE = 'Europe/Stockholm'
USE_I18N = True
USE_TZ = True

# Static files
STATIC_URL = 'static/'
STATIC_ROOT = BASE_DIR / 'staticfiles'

# Media files
MEDIA_URL = 'media/'
MEDIA_ROOT = BASE_DIR / 'media'

# Default primary key field type
DEFAULT_AUTO_FIELD = 'django.db.models.BigAutoField'

# REST Framework
REST_FRAMEWORK = {
    'DEFAULT_AUTHENTICATION_CLASSES': [
        'rest_framework_simplejwt.authentication.JWTAuthentication',
    ],
    'DEFAULT_PERMISSION_CLASSES': [
        'rest_framework.permissions.IsAuthenticated',
    ],
    'DEFAULT_PAGINATION_CLASS': 'rest_framework.pagination.PageNumberPagination',
    'PAGE_SIZE': 20,
    'DEFAULT_FILTER_BACKENDS': [
        'django_filters.rest_framework.DjangoFilterBackend',
        'rest_framework.filters.SearchFilter',
        'rest_framework.filters.OrderingFilter',
    ],
    'DEFAULT_SCHEMA_CLASS': 'drf_spectacular.openapi.AutoSchema',
}

# Simple JWT Configuration
from datetime import timedelta

SIMPLE_JWT = {
    'ACCESS_TOKEN_LIFETIME': timedelta(hours=1),
    'REFRESH_TOKEN_LIFETIME': timedelta(days=7),
    'ROTATE_REFRESH_TOKENS': True,
    'BLACKLIST_AFTER_ROTATION': True,
    'UPDATE_LAST_LOGIN': True,
    'ALGORITHM': 'HS256',
    'SIGNING_KEY': SECRET_KEY,
    'AUTH_HEADER_TYPES': ('Bearer',),
    'AUTH_HEADER_NAME': 'HTTP_AUTHORIZATION',
    'USER_ID_FIELD': 'id',
    'USER_ID_CLAIM': 'user_id',
    'AUTH_TOKEN_CLASSES': ('rest_framework_simplejwt.tokens.AccessToken',),
    'TOKEN_TYPE_CLAIM': 'token_type',
}

# CORS
CORS_ALLOW_CREDENTIALS = True



CORS_ALLOW_HEADERS = [
    'accept',
    'accept-encoding',
    'authorization',
    'content-type',
    'dnt',
    'origin',
    'user-agent',
    'x-csrftoken',
    'x-requested-with',
]

# Expose Authorization header to frontend if needed
CORS_EXPOSE_HEADERS = [
    'authorization',
]

# AI Configuration
DEEPSEEK_API_KEY = get_env('DEEPSEEK_API_KEY', default='')
DEEPSEEK_MODEL = get_env('DEEPSEEK_MODEL', default='deepseek-chat')
DEEPSEEK_URL = get_env('DEEPSEEK_URL', default='https://api.deepseek.com/v1/chat/completions')
GEMINI_API_KEY = get_env('GEMINI_API_KEY', default='')
GEMINI_EMBEDDING_MODEL = get_env('GEMINI_EMBEDDING_MODEL', default='gemini-embedding-001')
GEMINI_EMBEDDING_URL = get_env('GEMINI_EMBEDDING_URL', default='https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent')

# Email Configuration
EMAIL_BACKEND = 'django.core.mail.backends.smtp.EmailBackend'
EMAIL_HOST = get_env('SMTP_HOST', default='smtp.gmail.com')
EMAIL_PORT = get_env('SMTP_PORT', default='587', cast=int)
EMAIL_USE_TLS = get_env('SMTP_USE_TLS', default='True', cast=bool)
EMAIL_HOST_USER = get_env('SMTP_USERNAME', default='')
EMAIL_HOST_PASSWORD = get_env('SMTP_PASSWORD', default='')
DEFAULT_FROM_EMAIL = get_env('DEFAULT_FROM_EMAIL', default='noreply@example.com')

# Logging
LOGGING = {
    'version': 1,
    'disable_existing_loggers': False,
    'formatters': {
        'verbose': {
            'format': '{levelname} {asctime} {module} {message}',
            'style': '{',
        },
    },
    'handlers': {
        'console': {
            'class': 'logging.StreamHandler',
            'formatter': 'verbose',
        },
    },
    'root': {
        'handlers': ['console'],
        'level': 'INFO',
    },
    'loggers': {
        'django': {
            'handlers': ['console'],
            'level': 'INFO',
            'propagate': False,
        },
        'apps': {  # ← Lägg till detta
            'handlers': ['console'],
            'level': 'DEBUG',
            'propagate': False,
        },
    },
}

# CSRF settings (kept for compatibility but CSRF middleware is disabled)
CSRF_TRUSTED_ORIGINS = [
    'http://localhost:3000',
    'http://127.0.0.1:3000',
]

CSRF_COOKIE_SECURE = False
CSRF_COOKIE_HTTPONLY = False
SESSION_COOKIE_SECURE = False
SESSION_COOKIE_SAMESITE = None
