from .base import *
import os

DEBUG = True
ALLOWED_HOSTS = os.getenv("ALLOWED_HOSTS", "*").split(",")

DATABASES = {
    "default": {
        "ENGINE": "django.db.backends.postgresql",
        "NAME": os.getenv("DATABASE_NAME", "jbdh_staging"),
        "USER": os.getenv("DATABASE_USER", "postgres"),
        "PASSWORD": os.getenv("DATABASE_PASSWORD", ""),
        "HOST": os.getenv("DATABASE_HOST", "postgres"),
        "PORT": os.getenv("DATABASE_PORT", "5432"),
    }
}

CORS_ALLOWED_ORIGINS = [
    "http://loghouse-staging.se",
    "https://loghouse-staging.se", 
    "http://localhost:3001",
    "http://127.0.0.1:3001",
    "http://89.167.117.88:3001"]

MINILM_URL = os.getenv('MINILM_URL', 'http://localhost:8001')
