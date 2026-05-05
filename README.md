# JBDH Django Backend

Django REST API backend for JBDH (Job Board & Development Hub) — a job search and application management platform with AI integration.

## 🏗️ Architecture

### Technologies

- **Django 5.2** - Web framework
- **Django REST Framework** - REST API
- **PostgreSQL 16 + pgvector** - Database with vector search
- **Redis** - Caching & JWT blacklisting
- **MiniLM** - Local embedding service
- **ATS Service** - CV/job parsing & ATS scoring (spaCy, no GPU)
- **Docker & Docker Compose** - Containerization
- **DeepSeek AI** - CV and job analysis
- **Gemini AI** - Embeddings

---

## 📁 Project Structure

```
JBDH-Backend/
├── manage.py
├── requirements.txt
├── Dockerfile
├── Dockerfile.minilm
├── Dockerfile.ats
├── docker-compose.yml          # Shared base — never run alone
├── docker-compose.local.yml    # Local override
├── docker-compose.staging.yml  # Staging override
├── docker-compose.prod.yml     # Production override
├── .env.local                  # Local secrets (not in git)
├── .env.staging                # Staging secrets (not in git)
├── .env.prod                   # Production secrets (not in git)
│
├── config/
│   └── settings/
│       ├── base.py
│       ├── development.py
│       ├── staging.py
│       └── production.py
│
└── apps/
    ├── authentication/         # Login, Register, Token auth
    ├── users/                  # User profiles, CV upload
    ├── ai_services/            # AI analysis (CV, Jobs)
    ├── jobs/                   # Job listings, applications
    ├── kanban/                 # Boards, Lists, Cards
    ├── friendship/             # Friend requests, connections
    ├── messaging/              # User messaging
    └── organisations/          # Company management
```

---


### 🧠 Resurshantering & Swap (RAM-säkerhet)
Servern har **4GB fysiskt RAM**. För att förhindra krascher vid tunga byggen (t.ex. `npm run build`) eller när vi kör flera miljöer samtidigt (Prod + Staging + Dev), har vi aktiverat en **4GB Swap-fil**.

- **Funktion:** Om RAM-minnet går över 3.7GB börjar servern använda swap-filen på hårddisken. Servern kan då bli något långsammare, men den kraschar inte.
- **Optimering:** `swappiness` är satt till **10**, vilket betyder att servern prioriterar det snabba RAM-minnet i det längsta och bara använder swap vid kris.

#### Kontrollera resursanvändning:
Se hur mycket RAM och Swap som används just nu:
```bash
free -h
```

Se resursförbrukning per container:
```bash
docker stats --no-stream
```


## 🐳 Docker Environments

This repo uses a **shared base file + environment-specific overrides**. Never run `docker-compose.yml` alone.

| File | Purpose | Env file |
|---|---|---|
| `docker-compose.yml` | Shared base (images, networks, volumes, healthchecks) | — |
| `docker-compose.local.yml` | Local development | `.env.local` |
| `docker-compose.staging.yml` | Staging server | `.env.staging` |
| `docker-compose.prod.yml` | Production server | `.env.prod` |

### Services per environment

| Service | Local | Staging | Prod |
|---|---|---|---|
| postgres | `localhost:5432` | `localhost:5433` | internal only |
| redis | `localhost:6379` | internal only | internal only |
| backend | `localhost:8000` | `localhost:8001` | `localhost:8000` |
| frontend | `localhost:3000` | `localhost:3001` | `localhost:3000` |
| minilm | `localhost:8500` | `localhost:8500` | internal only |
| ats | `localhost:8501` | internal only | internal only |
| pgadmin | `localhost:8080` | `localhost:8081` | via `/admin` proxy |
| etl | background | background | background |

---

## 🚀 Docker Commands

### Local

```bash
# Start everything (build first)
docker compose --env-file .env.local -f docker-compose.yml -f docker-compose.local.yml up -d --build

# Start without rebuilding
docker compose --env-file .env.local -f docker-compose.yml -f docker-compose.local.yml up -d

# Stop
docker compose --env-file .env.local -f docker-compose.yml -f docker-compose.local.yml down

# Validate merged config
docker compose --env-file .env.local -f docker-compose.yml -f docker-compose.local.yml config
```

### Staging

```bash
docker compose --project-name jbdh-staging --env-file .env.staging -f docker-compose.yml -f docker-compose.staging.yml up -d --build --remove-orphans
docker compose --project-name jbdh-staging --env-file .env.staging -f docker-compose.yml -f docker-compose.staging.yml down
docker compose --project-name jbdh-staging --env-file .env.staging -f docker-compose.yml -f docker-compose.staging.yml config
docker compose --env-file .env.staging -f docker-compose.yml -f docker-compose.staging.yml up -d
docker compose --env-file .env.staging -f docker-compose.yml -f docker-compose.staging.yml logs -f backend
```

### Production


```bash
docker compose --project-name jbdh-prod --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml up -d --build --remove-orphans
docker compose --project-name jbdh-prod --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml down
docker compose --project-name jbdh-prod --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml config
```

### Build specific services

```bash
# Build one service (local)
docker compose --env-file .env.local -f docker-compose.yml -f docker-compose.local.yml build backend

# Build multiple services (local)
docker compose --env-file .env.local -f docker-compose.yml -f docker-compose.local.yml build backend frontend

# Build ATS service (local)
docker compose --env-file .env.local -f docker-compose.yml -f docker-compose.local.yml build ats

# Rebuild and restart one service without stopping others (local)
docker compose --env-file .env.local -f docker-compose.yml -f docker-compose.local.yml up -d --build backend

# Build one service (staging)
docker compose --project-name jbdh-staging --env-file .env.staging -f docker-compose.yml -f docker-compose.staging.yml build backend

# Build ATS service (staging)
docker compose --project-name jbdh-staging --env-file .env.staging -f docker-compose.yml -f docker-compose.staging.yml build ats

# Build one service (prod)
docker compose --project-name jbdh-prod --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml build backend
```

### Useful commands

```bash
# View logs for a service (local)
docker compose --env-file .env.local -f docker-compose.yml -f docker-compose.local.yml logs -f backend

# Run a one-off management command (local)
docker compose --env-file .env.local -f docker-compose.yml -f docker-compose.local.yml run --rm backend python manage.py migrate

# Open a shell inside a running container
docker exec -it jbdh_backend bash

# Run ETL job manually (local)
docker compose --env-file .env.local -f docker-compose.yml -f docker-compose.local.yml run --rm backend \
  python manage.py shell -c "from apps.jobs.services.etl_pipeline_service import ETLPipelineService; ETLPipelineService.run_pipeline_batched(1000)"

# Run Job AI analysis manually (local)
docker compose --env-file .env.local -f docker-compose.yml -f docker-compose.local.yml run --rm backend \
  python manage.py run_job_ai --batch-size 10 --delay 1
```

---

## 🗄️ PostgreSQL Commands

Connect to the database inside Docker:

```bash
docker exec -it jbdh_backend_postgres psql -U postgres -d jbdh_db
```

### User management

```sql
-- List all users
SELECT id, email, is_active, is_staff FROM users_customuser;

-- Create a superuser via Django (preferred)
-- docker compose ... run --rm backend python manage.py createsuperuser

-- Deactivate a user
UPDATE users_customuser SET is_active = false WHERE email = 'user@example.com';

-- Activate a user
UPDATE users_customuser SET is_active = true WHERE email = 'user@example.com';

-- Delete a user (hard delete)
DELETE FROM users_customuser WHERE email = 'user@example.com';

-- Promote user to staff/admin
UPDATE users_customuser SET is_staff = true, is_superuser = true WHERE email = 'user@example.com';

-- Reset password hash (use Django shell instead — see below)
```

Reset a password via Django shell:

```bash
docker compose -f docker-compose.yml -f docker-compose.local.yml run --rm backend \
  python manage.py shell -c "
from apps.users.models import CustomUser
u = CustomUser.objects.get(email='user@example.com')
u.set_password('newpassword123')
u.save()
"
```

markdown
## 🗄 Database & Backups

Dessa kommandon körs i terminalen på Ubuntu-servern.

### Skapa en manuell backup (Prod)
Använd detta innan du gör ändringar i Docker-volymer eller större uppdateringar:
```bash
docker exec jbdh-prod_postgres pg_dumpall -U postgres | gzip > manual_prod_backup_$(date +%F).sql.gz
```

### Återställ databas från backup
**Varning:** Detta skriver över nuvarande data i databasen.
```bash
# 1. Packa upp och läs in filen (byt ut filnamnet mot rätt datum)
zcat manual_prod_backup_YYYY-MM-DD.sql.gz | docker exec -i jbdh-prod_postgres psql -U postgres
```

### Kontrollera storlek på existerande volymer
```bash
docker system df -v
```

### Ladda ner backup till lokal dator (Mac/Windows)
Körs från din **lokala terminal**, inte från servern:
```bash
scp root@<SERVER_IP>:/opt/app/JBDH-backend/manual_prod_backup_*.sql.gz ./
```

### Database inspection

```sql
-- List all tables
\dt

-- Count jobs
SELECT COUNT(*) FROM jobs_job;

-- Check table size
SELECT pg_size_pretty(pg_total_relation_size('jobs_job'));

-- List active connections
SELECT pid, usename, application_name, state FROM pg_stat_activity WHERE datname = 'jbdh_db';

-- Kill a connection
SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE pid = <pid>;
```

---

## ⚠️ Common Errors

**Port 5432 already in use:**
```bash
netstat -ano | findstr :5432
# Then: Win+R → services.msc → stop the PostgreSQL Windows service → retry
```

**Container name conflict (running staging + prod on same host):**  
Always use `--project-name` flag when running multiple environments on the same machine/server.

---

## 🧪 Create a test user (PowerShell)

```powershell
$body = @{
    Email = "test@test.com"
    Password = "test12"
    ConfirmPassword = "test12"
    Location = "Stockholm"
    PreferredLanguage = "sv"
    Open = $false
} | ConvertTo-Json

iwr -Uri "http://localhost:8000/api/Register/RegisterUser" -Method POST -ContentType "application/json" -Body $body
```

---

## 📡 API Endpoints

### Auth

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