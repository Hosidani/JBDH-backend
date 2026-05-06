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
    # Commonly listed in Swedish IT job ads but missing before
    "SQL", "HTML", "CSS", "F#", "VBA", "Solidity", "Zig",
]

# ---------------------------------------------------------------------------
# Tech – web / backend frameworks & libraries
# ---------------------------------------------------------------------------
FRAMEWORKS: List[str] = [
    "Django", "FastAPI", "Flask", "Express", "NestJS", "Spring Boot",
    "ASP.NET", "ASP.NET Core", ".NET", ".NET Core", ".NET 8", ".NET 6",
    "Laravel", "Rails", "Ruby on Rails", "Next.js",
    "Nuxt.js", "React", "Vue.js", "Angular", "Svelte", "Remix",
    "Gatsby", "Celery", "GraphQL", "REST API", "gRPC",
    "Pandas", "NumPy", "Scikit-learn", "TensorFlow", "PyTorch", "Keras",
    "Hugging Face", "LangChain", "Transformers",
    # Missing – very common in Swedish IT ads
    "Blazor", "SignalR", "Entity Framework", "EF Core",
    "Spring", "Spring MVC", "Spring Security", "Hibernate",
    "Tailwind CSS", "Bootstrap", "Material UI", "MUI",
    "Redux", "Zustand", "React Native", "Flutter", "Expo",
    "WPF", "WinForms", "MAUI", "Xamarin",
    "SQLAlchemy", "TypeORM", "Prisma", "Drizzle",
    "Pydantic", "FastHTML", "HTMX", "Astro", "SvelteKit",
    "Unity", "Unreal Engine",
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
    # Missing – common in Swedish IT ads
    "MariaDB", "Supabase", "CosmosDB", "Azure SQL",
    "Databricks", "DuckDB", "TimescaleDB",
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
    # Missing – very common in Swedish IT ads
    "Azure DevOps", "AKS", "EKS", "ECS",
    "ArgoCD", "Flux", "Pulumi", "CloudFormation",
    "AWS Lambda", "Azure Functions", "Google Cloud Functions", "Serverless",
    "HashiCorp Vault", "Vault", "Consul", "Istio",
    "Vercel", "Netlify", "Cloudflare",
    "OpenTelemetry", "Jaeger", "New Relic", "Dynatrace",
    "SonarQube", "SonarCloud", "Snyk", "Trivy",
    "Podman", "Buildah", "Skaffold",
    "Platform Engineering", "SRE",
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
    "Postman", "Swagger", "OpenAPI", "Figma",
    # Missing – common in Swedish IT ads
    "Visual Studio", "VS Code", "IntelliJ IDEA", "Rider", "WebStorm", "PyCharm",
    "CQRS", "Event Sourcing", "Saga pattern", "Hexagonal Architecture",
    "Domain-Driven Design", "Test-Driven Development", "Behavior-Driven Development",
    "SAFe", "Lean", "OKR",
    "k6", "JMeter", "Gatling", "Locust",
    # Swedish Agile inflections – appear verbatim in Swedish job ads
    "agilt", "agila", "agilt team", "sprintplanering", "sprintplanering",
    "retrospektiv",
    "Storybook", "Webpack", "Vite", "ESBuild", "Rollup",
    "ESLint", "Prettier", "Husky",
    "Insomnia", "AsyncAPI", "Protocol Buffers", "Protobuf",
    "Trunk-based development", "Feature flags", "A/B testing",
    "Linear", "Notion", "Miro",
]

# ---------------------------------------------------------------------------
# Soft skills (EN + SV)
# ---------------------------------------------------------------------------
SOFT_SKILLS: List[str] = [
    "Communication", "Teamwork", "Leadership", "Problem solving",
    "Critical thinking", "Creativity", "Time management", "Adaptability",
    "Attention to detail", "Project management", "Mentoring", "Coaching",
    "Presentation", "Negotiation", "Stakeholder management",
    "Self-motivated", "Proactive", "Collaborative", "Analytical",
    "Structured", "Responsible", "Initiative",
    # Swedish
    "Kommunikation", "Teamarbete", "Ledarskap", "Problemlösning",
    "Kreativitet", "Tidshantering", "Anpassningsförmåga", "Noggrannhet",
    "Projektledning", "Mentorskap", "Presentationsteknik",
    # Swedish everyday words missing before
    "samarbete", "samarbetsförmåga", "samarbetsvillig", "lagarbete",
    "självgående", "självständig",
    "noggrann", "noggranna", "noggrannt",
    "strukturerad", "analytisk", "driven", "kommunikativ",
    "lösningsorienterad", "initiativrik", "flexibel", "ansvarsfull",
    "pedagogisk", "serviceinriktad", "affärsmässig",
    # Swedish soft skill inflections – appear verbatim in job ads
    "ansvarstagande", "resultatorienterad", "nyfiken", "strukturerat",
    "samarbetsvilligt", "självständigt",
    # Swedish job-ad phrases (multi-word formulations)
    "god samarbetsförmåga", "har lätt för att samarbeta",
    "arbetar självständigt", "tar ansvar", "trivs i team",
    "är kommunikativ", "god analytisk förmåga",
    "är strukturerad", "är noggrann", "är flexibel",
    "är lösningsorienterad", "är serviceinriktad",
    "har affärsmässig förståelse",
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
    "Systemvetenskap", "Informatik",
    # Swedish higher education
    "KTH", "Chalmers", "LiTH", "LTH", "Uppsala", "Lund", "Stockholm",
    # Certifications – already in list
    "AWS Certified", "Azure Certified", "Google Certified",
    "PMP", "PRINCE2", "Scrum Master", "CISSP", "CompTIA",
]

# ---------------------------------------------------------------------------
# IT domain words – Swedish process/lifecycle terms common in job ads
# ---------------------------------------------------------------------------
DOMAIN_SKILLS: List[str] = [
    # Swedish
    "driftsättning", "förvaltning", "systemförvaltning",
    "verksamhetsutveckling", "processutveckling", "processledning",
    "kvalitetssäkring", "kravhantering", "testledning", "testning",
    "modernisering", "systemutveckling", "vidareutveckling",
    "upphandling", "budgetansvar", "it-säkerhet", "informationssäkerhet",
    "arkitektur", "lösningsarkitektur", "systemarkitektur",
    "releasehantering", "incidenthantering", "förändringshantering",
    # Additional Swedish process/governance terms
    "verksamhetsanalys", "verksamhetsansvar",
    "förvaltningsmodell", "förvaltningsstyrning",
    "livscykelhantering", "förbättringsarbete", "förändringsarbete",
    "driftsansvar", "förändringsledning", "uppföljning",
    "problemhantering", "kravanalys", "kravarbete",
    # English equivalents
    "deployment", "maintenance", "system maintenance",
    "business development", "process improvement",
    "quality assurance", "requirements management",
    "release management", "incident management", "change management",
    "solution architecture", "enterprise architecture",
]

# ---------------------------------------------------------------------------
# Certifications – common in Swedish IT job ads
# ---------------------------------------------------------------------------
CERTIFICATIONS: List[str] = [
    "ITIL", "ITIL 4", "ITIL Foundation", "ITIL Master", "ITIL Practitioner",
    "ISO 27001", "ISO 9001", "ISO 14001",
    "SAFe", "SAFe Agilist", "SAFe Architect",
    "TOGAF", "COBIT",
    "Azure Solutions Architect", "AWS Solutions Architect",
    "Certified Kubernetes Administrator", "CKA",
    "Certified Scrum Master", "CSM",
    "ISTQB",
    "ITIL 4 Foundation",
    "PMP", "PRINCE2",
    "CompTIA Security+", "CompTIA Cloud+",
]

# ---------------------------------------------------------------------------
# Framework aliases – alternative spellings/abbreviations
# ---------------------------------------------------------------------------
FRAMEWORK_ALIASES: List[str] = [
    "dotnet", "dot net",
    "nodejs", "node js", "node.js",
    "vuejs", "vue js",
    "reactjs", "react js",
    "angularjs",
    "nextjs", "next js",
    "asp net", "aspnet",
    "c sharp", "cplusplus", "c plus plus",
    "csharp",
    "ci cd", "cicd",
    "continuous integration", "continuous delivery", "continuous deployment",
    "java script",
    "frontend", "back end",
]

# ---------------------------------------------------------------------------
# Frontend stack – HTML/CSS/JS ecosystem (maps to "framework" category)
# ---------------------------------------------------------------------------
FRONTEND_STACK: List[str] = [
    "HTML5", "CSS3",
    "SCSS", "Sass", "CSS-in-JS", "Styled Components",
    "Context API", "MobX",
    "Vue 3", "React 18",
]

# ---------------------------------------------------------------------------
# Backend stack – server-side and API patterns (maps to "tools" category)
# ---------------------------------------------------------------------------
BACKEND_STACK: List[str] = [
    "Backend", "Server-side", "API development",
    "RESTful API", "SOAP",
    "Monolith", "Event-driven architecture",
    "Authentication", "Authorization", "OAuth2",
    "Caching", "Queue", "Messaging",
    "Integration", "Integration testing", "Unit testing",
]

# ---------------------------------------------------------------------------
# Role titles – matches job ad role descriptions (maps to "domain" category)
# ---------------------------------------------------------------------------
ROLES: List[str] = [
    "Full stack", "Fullstack", "Frontend developer", "Backend developer",
    "Systemutvecklare", "Utvecklare",
    "Software Engineer", "Software developer", "Developer", "Architect",
    "Technical lead", "Tech lead", "Team lead",
    "Solution architect",
    "IT consultant", "IT-konsult", "Consultant",
    "Fullstack developer", "Full stack developer",
]

# ---------------------------------------------------------------------------
# Networking & infrastructure (maps to "cloud_devops" category)
# ---------------------------------------------------------------------------
NETWORKING: List[str] = [
    "Networking", "TCP/IP", "DNS", "HTTP", "HTTPS", "TLS", "SSL",
    "Load balancing", "Reverse proxy", "Firewall", "VPN",
    "Linux Administration", "Shell scripting",
    "Containerization", "Orchestration",
    "Production support", "On-call",
]

# ---------------------------------------------------------------------------
# Business & ERP systems (maps to "domain" category)
# ---------------------------------------------------------------------------
BUSINESS_SYSTEMS: List[str] = [
    "CRM", "ERP", "Salesforce", "Dynamics 365", "SAP",
    "Business systems", "Internal systems",
    "Case management", "Workflow", "Process management",
    "Business intelligence", "BI", "Reporting",
]

# ---------------------------------------------------------------------------
# Master lookup: all skills flat (for PhraseMatcher)
# ---------------------------------------------------------------------------
def get_all_skills() -> List[str]:
    return (
        PROGRAMMING_LANGUAGES
        + FRAMEWORKS
        + FRONTEND_STACK
        + FRAMEWORK_ALIASES
        + DATABASES
        + CLOUD_DEVOPS
        + NETWORKING
        + TOOLS_METHODOLOGY
        + BACKEND_STACK
        + SOFT_SKILLS
        + HUMAN_LANGUAGES
        + EDUCATION_KEYWORDS
        + DOMAIN_SKILLS
        + ROLES
        + BUSINESS_SYSTEMS
        + CERTIFICATIONS
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
    for skill in FRAMEWORK_ALIASES:
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
    for skill in DOMAIN_SKILLS:
        mapping[skill.lower()] = "domain"
    for skill in CERTIFICATIONS:
        mapping[skill.lower()] = "certification"
    for skill in FRONTEND_STACK:
        mapping[skill.lower()] = "framework"
    for skill in BACKEND_STACK:
        mapping[skill.lower()] = "tools"
    for skill in NETWORKING:
        mapping[skill.lower()] = "cloud_devops"
    for skill in ROLES:
        mapping[skill.lower()] = "domain"
    for skill in BUSINESS_SYSTEMS:
        mapping[skill.lower()] = "domain"
    return mapping


CATEGORY_MAP: Dict[str, str] = _build_category_map()

# ---------------------------------------------------------------------------
# Canonical display map – normalised key → human-readable display form.
# When a term was normalised before parsing (e.g. "C#" → "csharp"),
# this map converts it back so output always shows the canonical name.
# ---------------------------------------------------------------------------
CANONICAL_DISPLAY: Dict[str, str] = {
    "dotnet": ".NET",
    "dot net": ".NET",
    "csharp": "C#",
    "c sharp": "C#",
    "cplusplus": "C++",
    "c plus plus": "C++",
    "cicd": "CI/CD",
    "ci cd": "CI/CD",
    "aspnet": "ASP.NET",
    "asp net": "ASP.NET",
    "nodejs": "Node.js",
    "node js": "Node.js",
    "vuejs": "Vue.js",
    "vue js": "Vue.js",
    "reactjs": "React",
    "react js": "React",
    "nextjs": "Next.js",
    "next js": "Next.js",
    "angularjs": "Angular",
    "java script": "JavaScript",
    # K8s is normalised to "Kubernetes" before parsing, so this entry ensures
    # the display name is always the full canonical form.
    "k8s": "Kubernetes",
    "kubernetes": "Kubernetes",
}
