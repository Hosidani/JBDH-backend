"""
Curated skills database used by the ATS parser.

Organised by category so scoring can weight categories differently.
The lists cover both English and Swedish terms for the Swedish job market.
"""

from typing import Dict, List

# ---------------------------------------------------------------------------
# Tech – programming languages
# ---------------------------------------------------------------------------
PROGRAMMING_LANGUAGES: List[str] = [
    "Python", "JavaScript", "TypeScript", "Java", "C#", "C++", "C", "Go",
    "Rust", "Ruby", "PHP", "Swift", "Kotlin", "Scala", "R", "MATLAB",
    "Perl", "Shell", "Bash", "PowerShell", "Dart", "Elixir", "Haskell",
    "Lua", "Objective-C", "Assembly", "COBOL", "Fortran", "Groovy",
]

# ---------------------------------------------------------------------------
# Tech – web / backend frameworks & libraries
# ---------------------------------------------------------------------------
FRAMEWORKS: List[str] = [
    "Django", "FastAPI", "Flask", "Express", "NestJS", "Spring Boot",
    "ASP.NET", ".NET", "Laravel", "Rails", "Ruby on Rails", "Next.js",
    "Nuxt.js", "React", "Vue.js", "Angular", "Svelte", "Remix",
    "Gatsby", "Celery", "GraphQL", "REST API", "gRPC",
    "Pandas", "NumPy", "Scikit-learn", "TensorFlow", "PyTorch", "Keras",
    "Hugging Face", "LangChain", "Transformers",
]

# ---------------------------------------------------------------------------
# Tech – databases & search
# ---------------------------------------------------------------------------
DATABASES: List[str] = [
    "PostgreSQL", "MySQL", "SQLite", "SQL Server", "Oracle", "MongoDB",
    "Redis", "Elasticsearch", "OpenSearch", "Cassandra", "DynamoDB",
    "Firebase", "Firestore", "CouchDB", "Neo4j", "InfluxDB",
    "pgvector", "Pinecone", "Weaviate", "Qdrant", "Chroma",
    "BigQuery", "Snowflake", "Redshift",
]

# ---------------------------------------------------------------------------
# Tech – cloud & DevOps
# ---------------------------------------------------------------------------
CLOUD_DEVOPS: List[str] = [
    "AWS", "Azure", "Google Cloud", "GCP", "Docker", "Kubernetes", "K8s",
    "Terraform", "Ansible", "Helm", "Jenkins", "GitHub Actions", "GitLab CI",
    "CircleCI", "Travis CI", "Nginx", "Apache", "Linux", "Ubuntu",
    "CI/CD", "DevOps", "Infrastructure as Code", "IaC",
    "Prometheus", "Grafana", "Datadog", "ELK Stack", "Kibana", "Logstash",
    "RabbitMQ", "Kafka", "Celery", "Airflow",
]

# ---------------------------------------------------------------------------
# Tech – tools & methodology
# ---------------------------------------------------------------------------
TOOLS_METHODOLOGY: List[str] = [
    "Git", "GitHub", "GitLab", "Bitbucket", "Jira", "Confluence",
    "Agile", "Scrum", "Kanban", "TDD", "BDD", "DDD",
    "Microservices", "Event-driven", "SOLID", "Clean Architecture",
    "API", "REST", "WebSocket", "OAuth", "JWT", "OpenID Connect",
    "Selenium", "Playwright", "Cypress", "Pytest", "Jest", "JUnit",
    "Postman", "Swagger", "OpenAPI", "Figma", "Jira",
]

# ---------------------------------------------------------------------------
# Soft skills (EN + SV)
# ---------------------------------------------------------------------------
SOFT_SKILLS: List[str] = [
    "Communication", "Teamwork", "Leadership", "Problem solving",
    "Critical thinking", "Creativity", "Time management", "Adaptability",
    "Attention to detail", "Project management", "Mentoring", "Coaching",
    "Presentation", "Negotiation", "Stakeholder management",
    # Swedish equivalents
    "Kommunikation", "Teamarbete", "Ledarskap", "Problemlösning",
    "Kreativitet", "Tidshantering", "Anpassningsförmåga", "Noggrannhet",
    "Projektledning", "Mentorskap", "Presentationsteknik",
]

# ---------------------------------------------------------------------------
# Human languages
# ---------------------------------------------------------------------------
HUMAN_LANGUAGES: List[str] = [
    "English", "Swedish", "Norwegian", "Danish", "Finnish", "German",
    "French", "Spanish", "Italian", "Portuguese", "Dutch", "Polish",
    "Russian", "Chinese", "Japanese", "Arabic", "Engelska", "Svenska",
    "Tyska", "Franska", "Spanska",
]

# ---------------------------------------------------------------------------
# Education keywords
# ---------------------------------------------------------------------------
EDUCATION_KEYWORDS: List[str] = [
    # Degrees EN
    "Bachelor", "Master", "PhD", "Doctorate", "Associate",
    "BSc", "MSc", "MBA", "BEng", "MEng", "B.Sc", "M.Sc",
    # Degrees SV
    "Kandidat", "Master", "Magister", "Doktor", "Civilingenjör",
    "Högskoleingenjör", "Teknologie", "Fil.dr",
    # Fields
    "Computer Science", "Software Engineering", "Data Science",
    "Electrical Engineering", "Information Technology", "Mathematics",
    "Datavetenskap", "Datateknik", "Informationsteknik",
    # Swedish higher education
    "KTH", "Chalmers", "LiTH", "LTH", "Uppsala", "Lund", "Stockholm",
    # Certifications
    "AWS Certified", "Azure Certified", "Google Certified",
    "PMP", "PRINCE2", "Scrum Master", "CISSP", "CompTIA",
]

# ---------------------------------------------------------------------------
# Master lookup: all skills flat (for PhraseMatcher)
# ---------------------------------------------------------------------------
def get_all_skills() -> List[str]:
    return (
        PROGRAMMING_LANGUAGES
        + FRAMEWORKS
        + DATABASES
        + CLOUD_DEVOPS
        + TOOLS_METHODOLOGY
        + SOFT_SKILLS
        + HUMAN_LANGUAGES
        + EDUCATION_KEYWORDS
    )


# ---------------------------------------------------------------------------
# Category map: skill → category (built once at import time)
# ---------------------------------------------------------------------------
def _build_category_map() -> Dict[str, str]:
    mapping: Dict[str, str] = {}
    for skill in PROGRAMMING_LANGUAGES:
        mapping[skill.lower()] = "programming"
    for skill in FRAMEWORKS:
        mapping[skill.lower()] = "framework"
    for skill in DATABASES:
        mapping[skill.lower()] = "database"
    for skill in CLOUD_DEVOPS:
        mapping[skill.lower()] = "cloud_devops"
    for skill in TOOLS_METHODOLOGY:
        mapping[skill.lower()] = "tools"
    for skill in SOFT_SKILLS:
        mapping[skill.lower()] = "soft"
    for skill in HUMAN_LANGUAGES:
        mapping[skill.lower()] = "language"
    for skill in EDUCATION_KEYWORDS:
        mapping[skill.lower()] = "education"
    return mapping


CATEGORY_MAP: Dict[str, str] = _build_category_map()
