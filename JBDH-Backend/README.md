# JBDH Django Backend

Django REST API backend för JBDH (Job Board & Development Hub) - en jobbsöknings- och ansökningshanteringsplattform med AI-integration.

## 🏗️ Arkitektur

Detta är Django-implementationen av JBDH backend, designad för att köra parallellt med den befintliga .NET-backend under migrationsperioden.

### Teknologier

- **Django 5.2** - Web framework
- **Django REST Framework** - REST API
- **PostgreSQL 16** - Databas
- **Redis** - Caching
- **Docker & Docker Compose** - Containerization
- **DeepSeek AI** - CV och jobbanalys
- **Gemini AI** - Embeddings

---

## 📁 Projektstruktur

JBDH-Backend/
├── manage.py # Django management script
├── requirements.txt # Python dependencies
├── docker-compose.yml # Docker services configuration
├── Dockerfile # Backend container build
├── .env # Lokal development environment
├── .env.docker # Docker environment
│
├── config/ # Django projekt-konfiguration
│ ├── settings/
│ │ ├── base.py # Bas-inställningar
│ │ ├── development.py # Dev-inställningar
│ │ └── production.py # Prod-inställningar
│ ├── urls.py # Huvudrouting
│ └── wsgi.py
│
└── apps/ # Django apps
├── authentication/ # Login, Register, Token auth
├── users/ # User profiles, CV upload
├── ai_services/ # AI analysis (CV, Jobs)
├── jobs/ # Job listings, applications
├── kanban/ # Boards, Lists, Cards
├── friendship/ # Friend requests, connections
├── messaging/ # User messaging
└── organisations/ # Company management


🎯 Viktig Information: Databas Setup
Vi använder ALLTID Docker för databasen!
⚠️ OBS: Det finns ingen lokal PostgreSQL installation. Databasen körs alltid i Docker, även när du kör Django lokalt på din maskin.

┌─────────────────────────────────────┐
│   Din Windows-dator                 │
│                                     │
│  ┌──────────────┐                  │
│  │ Python/Django│ ◄──┐             │
│  │ (Lokalt)     │    │             │
│  └──────────────┘    │             │
│                       │             │
│  ┌────────────────────┼──────────┐ │
│  │ Docker Desktop     │          │ │
│  │                    │          │ │
│  │  ┌─────────────────▼───────┐ │ │
│  │  │ PostgreSQL Container    │ │ │
│  │  │ Port: 5432             │ │ │
│  │  │ Name: jbdh_postgres    │ │ │
│  │  └─────────────────────────┘ │ │
│  │                               │ │
│  │  ┌─────────────────────────┐ │ │
│  │  │ Redis Container         │ │ │
│  │  │ Port: 6379             │ │ │
│  │  └─────────────────────────┘ │ │
│  └────────────────────────────── │ │
└─────────────────────────────────────┘

# 1. Starta PostgreSQL och Redis i Docker (MÅSTE GÖRAS FÖRST!)
docker-compose up -d postgres redis

# Vänta några sekunder tills databaserna är redo
# Kolla status:
docker ps

# 2. Kör migrations
python manage.py migrate

# 3. Skapa superuser
python manage.py createsuperuser

# 4. Starta Django development server
python manage.py runserver

# Öppna: http://localhost:8000/api/


Alternativ 2: Allt i Docker
# Starta alla services (backend + databaser)
docker-compose up -d

# Eller med loggar synliga
docker-compose up

# Services som startas:
# - PostgreSQL: localhost:5432
# - Redis: localhost:6379  
# - Django: localhost:8000

---

## 🚀 Setup & Installation

## 🐳 Docker Environments (Local vs Prod)

Detta repo använder en gemensam basfil + miljöspecifika overrides:

- `docker-compose.yml` = gemensam bas
- `docker-compose.local.yml` = lokal utveckling
- `docker-compose.prod.yml` = produktion

Miljöfiler:

- `.env.local` för lokal Docker-körning
- `.env.prod` för produktion
- `.env.example` som mall utan hemligheter


#### Create a user with curl

# Testa utan roller eller med "admin" (liten bokstav)
$body = @{
    Email = "test@test.com"
    Password = "test12"
    ConfirmPassword = "test12"
    Location = "Stockholm"
    PreferredLanguage = "sv"
    Open = $false
    # SelectedRoles = @("admin")  # Prova "admin" istället
} | ConvertTo-Json

iwr -Uri "http://localhost:8000/api/Register/RegisterUser" -Method POST -ContentType "application/json" -Body $body



### Local (utveckling)

Starta:

```bash
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d --build
```

Stoppa:

```bash
docker compose -f docker-compose.yml -f docker-compose.local.yml down
```

Validera konfiguration:

```bash
docker compose -f docker-compose.yml -f docker-compose.local.yml config
```

### Prod

Starta:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

Stoppa:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml down
```

Validera konfiguration:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml config
```

### Job AI manuellt (fyll saknade analyser)

```bash
docker compose run --rm backend python manage.py run_job_ai --batch-size 10 --delay 1
```

Tips:

- Lägg alltid riktiga hemligheter i `.env.prod` på servern (inte i git).
- Rotera API-nycklar/lösenord om de tidigare exponerats.

### **Förutsättningar**

- Python 3.11+
- Docker Desktop (för PostgreSQL)
- Git

### **1.Klona projektet,  2. Installera Python-dependencies,  3. Konfigurera miljövariabler .env**


```bash
cd JBDH_V0.1/JBDH-Backend

pip install -r requirements.txt
```

Kopiera och anpassa .env:

DEBUG=True
SECRET_KEY=django-insecure-change-this-in-production
DJANGO_SETTINGS_MODULE=config.settings.development

# Database - Lokal anslutning till Docker PostgreSQL
DATABASE_NAME=jbdh_db
DATABASE_USER=postgres
DATABASE_PASSWORD=ArianAAA123
DATABASE_HOST=127.0.0.1
DATABASE_PORT=5432

# AI Services
DEEPSEEK_API_KEY=your-api-key-here
DEEPSEEK_MODEL=deepseek-chat
DEEPSEEK_URL=https://api.deepseek.com/v1/chat/completions

# Email
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USE_TLS=True
SMTP_USERNAME=your@email.com
SMTP_PASSWORD=your-app-password

# START WITH MODELS

Auth:

POST /api/Login  ☑️
POST /api/Register/RegisterUser  ☑️

Profile:

GET /api/Profile/getprofile/{userId}  ☑️
GET /api/Profile/getmyprofile  ☑️
POST /api/Profile/AnalyzeCV ☑️
GET /api/Profile/LatestAIUserResponse  ☑️


Settings:

PUT /api/Settings/updateusersettings  ☑️ 
POST /api/Settings/changepassword
DELETE /api/Settings/deletemyuser
GET /api/Settings/getaccountsettings  ☑️
GET /api/Settings/getavailableorganisations
PUT /api/Settings/updateaccountsettings
POST /api/Settings/terminateaccount  ☑️


Home (Data Flows):

GET /api/home
POST /api/home/CreateFlow
POST /api/home/CommentOnDataFlow
POST /api/home/ReportDataFlow
DELETE /api/home/Delete/{id}
POST /api/home/CompareCvToJobAd
POST /api/Home/CompareCvToJobAdV2  ☑️


Jobs:

GET /api/Jobs/{jobId} ☑️
GET /api/Jobs/{jobId}/issaved ☑️
POST /api/Jobs/{jobId}/save ☑️
PUT /api/Jobs/{jobId}/save ☑️
DELETE /api/Jobs/{jobId}/unsave ☑️
GET /api/jobs/saved ☑️
POST /api/Jobs/{jobId}/not-interested ☑️


Search:

GET /api/Search/SearchV2  



TODO: 
1. Cloudflare
2. Upgraderringar
    Arbetsfördelningen (Vem gör vad?)
        SSD (Postgres/SQL Server): Här lagrar du din "Source of Truth". Allt i din 500 MB jobs-tabell (långa beskrivningar, metadata, gamla jobb) ligger här. Tack vare SSD:n går stora läsningar och sökningar i text mycket smidigare.
    Redis (RAM): Här har du din "Hot Data". Endast aktiva JobId, JobTitle och deras Vektor-embeddings (för den semantiska sökningen).
        Best Practice: 
            "Cache-Aside" med SSD-stöd
            När en användare söker semantiskt:
        Sök i Redis: Gör en vektorsökning i Redis (det går på mikrosekunder). Redis returnerar en lista med de 10 mest relevanta JobId och JobTitle.
        Hämta detaljer från SSD: Din backend tar dessa 10 ID:n och gör en snabb SELECT * FROM Jobs WHERE id IN (...) mot din databas på SSD:n.
            Varför: SSD:n är nu så snabb att det inte spelar någon roll att du gör ett extra anrop för att hämta de tunga detaljerna (beskrivningar etc.). Du sparar massor av värdefullt RAM på din Pi genom att inte ha hela jobb-objekt i Redis.
    Hantera JWT och Utloggning
        Fortsätt köra detta helt i Redis:
        Vid utloggning: Skriv JTI till Redis med en TTL.
        SSD:n behöver aldrig veta om utloggade tokens – det håller databasen ren och snabb.
    Docker-konfiguration för din Pi
        För att dra nytta av SSD:n måste du se till att dina Docker-volumes ligger på SSD-mounten och inte på SD-kortet.
    Semantisk sökning + SSD
        Eftersom du har en SSD kan du nu också använda vektor-index i din huvuddatabas (t.ex. pgvector för Postgres) som en backup eller för mer komplexa frågor, medan Redis hanterar de "blixtsnabba" sökningarna för användarna.
3. Geolocation
    Fixa geolocation för att läsa av användarens närmaste jobb innan användare ens hunnit söka.
4. Admin tool
    from django.contrib import admin
        urlpatterns = [
        path('admin/', admin.site.urls),
    ]    
    Ovanför tror vi kommer gynna våran felsökning och ha bättre kontroll över våran databas.
5. Django channels
    Good-to-have för "snyggare" uppdateringar för sökningar
6. nginx
    Säkerhet
7. Flower+celery
    Monitoring
8. pgvector
    Vektor indexering av databasen för snabba sökningar.

#TODO
1. Statelessness & Skalning (Nginx + Docker)
Kör flera instanser: Genom att använda gunicorn eller uvicorn inuti Docker kan du köra flera instanser av din Django-app. Nginx fungerar som lastbalanserare och fördelar trafiken mellan dem.
Undvik Local Memory Caching: Använd aldrig Djangos inbyggda minnescache om du kör flera instanser, eftersom instanserna då inte delar data. Kör Redis som din cache-backend i settings.py.
2. JWT & Utloggning (Svartlistning i Redis)
Svartlista vid utloggning: Eftersom JWT är stateless kan du inte "stänga av" dem i Django utan vidare. Vi pratade om att skicka tokenens ID (JTI) till Redis med en TTL (Time To Live) när användaren loggar ut.
Middleware: Du kan skriva en enkel middleware i Django som kollar varje inkommande anrop mot Redis-svartlistan.
3. Effektiv Caching av "Job"-tabellen
Lightweight Models: Istället för att cacha hela din 500 MB stora tabell (som dödar RAM-minnet på din Pi), sparar du bara ID och Title i Redis för snabba listvyer.
SSD vs RAM: Din tunga data ligger på SSD:n (Postgres/MySQL), medan din "heta" sökdata och JWT-svartlista ligger i Redis (RAM).
Cache-Aside: När ett jobb uppdateras i Django, använder du Signals (post_save) för att automatiskt radera den gamla cachen i Redis så att nästa användare får fräsch data.
4. Semantisk sökning (Vektorer)
Vektordatabas: Vi pratade om att lagra dina "embeddings" (vektorer) i Redis om du använder Redis Stack, vilket gör sökningen efter "liknande jobb" extremt snabb utan att belasta huvuddatabasen på SSD:n.
5. Kodstruktur (Base Views & Mixins)
APIView: Vi jämförde .NET:s ControllerBase med Djangos APIView.
BaseView / Mixins: För att hålla koden snygg (DRY) bör du skapa en bas-view för att dela logik som rör t.ex. hur du hämtar den inloggade användaren från en JWT eller hur du pratar med din Redis-cache.
6. SignalR motsvarighet (Django Channels)
Även om vi fokuserade på SignalR i .NET, nämnde vi att Django Channels är motsvarigheten om du vill ha realtidsuppdateringar (WebSockets) på din Pi, och även där är Redis motorn som kopplar ihop allt.