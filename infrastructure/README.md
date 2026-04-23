# Infrastructure — Terraform + Ansible + CI/CD

This folder contains all Infrastructure as Code (IaC) for the JBDH project on Hetzner Cloud.

```
infrastructure/
├── terraform/          # Provisions the cloud servers (runs only on main push)
│   ├── providers.tf
│   ├── main.tf
│   ├── variables.tf
│   ├── outputs.tf
│   ├── terraform.tfvars.example   # copy → terraform.tfvars (never commit)
│   └── templates/
│       └── cloud-init.yaml.tpl    # minimal OS bootstrap
│
└── ansible/            # Configures servers and deploys the app
    ├── inventory/
    │   ├── dev.yml
    │   ├── staging.yml
    │   └── prod.yml
    ├── group_vars/
    │   ├── all.yml       # shared vars — set git_repo and certbot_email here
    │   ├── dev.yml
    │   ├── staging.yml
    │   └── prod.yml
    ├── playbooks/
    │   ├── provision.yml   # run once — OS setup, Docker, nginx, SSL
    │   └── deploy.yml      # run on every push — pull code, rebuild, migrate
    └── roles/
        ├── common/         # UFW, fail2ban, unattended-upgrades
        ├── docker/         # Docker Engine + Compose plugin
        ├── nginx/          # nginx reverse proxy + certbot SSL
        └── app/            # git pull + docker compose up + migrate
```

CI/CD workflows live in `.github/workflows/` at the repo root.

---

## Architecture — single server, three environments

All three environments run on **one Hetzner server** (`ubuntu-4gb-hel1-1`, cx22, hel1).
Terraform manages the server. Ansible + Docker Compose manage the environments.

| Environment | URL | Backend port | Frontend port | Docker project |
|---|---|---|---|---|
| prod | `webapp.loghouse.se` | 8000 | 3000 | `jbdh-prod` |
| staging | `webapp-staging.loghouse.se` | 8001 | 3001 | `jbdh-staging` |
| dev | `webapp-dev.loghouse.se` | 8002 | 3002 | `jbdh-dev` |

Each environment has its own:
- Deploy directory (`/opt/app/JBDH-backend`, `/opt/app/JBDH-backend-dev`, etc.)
- `.env` file (written by Ansible from the corresponding GitHub Secret)
- Docker Compose override file (`docker-compose.staging.yml`, `docker-compose.dev.yml`)
- Postgres database (`jbdh_prod`, `jbdh_staging`, `jbdh_dev`)
- nginx `server {}` block + certbot SSL certificate
- pgadmin (prod=8080, staging=8081, dev=8082)

**Terraform vs Ansible — what does what:**

| Concern | Tool |
|---|---|
| Create/destroy cloud servers | Terraform |
| Install Docker, UFW, nginx, certbot | Ansible (`provision.yml`) |
| Deploy code on every push | Ansible (`deploy.yml`) |
| Write `.env` file on server | Ansible (from GitHub Secret) |
| Run `docker compose up` + migrate | Ansible |

> Dev runs on the same server as staging/prod — Terraform is **not** involved in dev setup.
> Only Ansible + Docker handle the dev environment.

---

## Prerequisites

| Tool | Version | Install |
|---|---|---|
| Terraform | >= 1.6 | [terraform.io](https://developer.hashicorp.com/terraform/downloads) |
| Ansible | >= 2.15 | `pip install ansible` |
| Ansible community.general | latest | `ansible-galaxy collection install community.general` |
| Hetzner account | — | [console.hetzner.cloud](https://console.hetzner.cloud) |

---

## Step 1 — Hetzner Cloud setup

1. Create a **project** in Hetzner Cloud Console.
2. Go to **Security → API Tokens** → create a token with **Read & Write**.
3. Copy the token — you will never see it again.

---

## Step 2 — SSH key

If you don't already have an ed25519 key:

```bash
ssh-keygen -t ed25519 -C "jbdh-deploy" -f ~/.ssh/jbdh_deploy
```

You'll use:
- `~/.ssh/jbdh_deploy.pub` → `ssh_public_key` in Terraform + `DEPLOY_SSH_PUBLIC_KEY` GitHub Secret
- `~/.ssh/jbdh_deploy`     → `DEPLOY_SSH_KEY` GitHub Secret (shared by all environments)

---

## Step 3 — Update group_vars/all.yml

Before running anything, edit `ansible/group_vars/all.yml`:

```yaml
git_repo: "git@github.com:Hosidani/JBDH-backend.git"
certbot_email: "your-email@example.com"
```

---

## Step 4 — Terraform: create/import servers

```bash
cd infrastructure/terraform

# 1. Copy the example vars file
cp terraform.tfvars.example terraform.tfvars

# 2. Edit terraform.tfvars — set hcloud_token and ssh_public_key

# 3. Init (downloads the Hetzner provider)
terraform init

# 4. Preview what will be created
terraform plan

# 5. Create/apply servers
terraform apply
```

After apply, the server IP is printed:
```
server_ip = "89.167.117.88"
```

> If a server already exists, import it instead of recreating:
> `terraform import hcloud_server.app <server_id>`

---

## Step 5 — Ansible: provision server (run once)

Run this **once** after the server is created, or after a fresh OS install:

```bash
cd infrastructure/ansible

# Provision the server (all environments share one server)
ansible-playbook -i inventory/prod.yml playbooks/provision.yml
```

This installs: UFW, fail2ban, Docker, nginx, and obtains SSL certificates via certbot
for all configured domains (`webapp.loghouse.se`, `webapp-staging.loghouse.se`, `webapp-dev.loghouse.se`).

### After provisioning — create databases manually (once per environment)

Ansible does not create the Postgres databases automatically. Run these once on the server:

```bash
# Find the postgres container name
docker ps | grep postgres

# Create databases (replace container name if different)
docker exec jbdh-prod_postgres psql -U postgres -c "CREATE DATABASE jbdh_staging;"
docker exec jbdh-prod_postgres psql -U postgres -c "CREATE DATABASE jbdh_dev;"
docker exec jbdh-prod_postgres psql -U postgres -d jbdh_staging -c "CREATE EXTENSION IF NOT EXISTS vector;"
docker exec jbdh-prod_postgres psql -U postgres -d jbdh_dev -c "CREATE EXTENSION IF NOT EXISTS vector;"
```

---

## Step 6 — Manual first deployment

Before CI/CD is wired up, deploy manually:

```bash
cd infrastructure/ansible

# Dev
ansible-playbook -i inventory/dev.yml playbooks/deploy.yml \
  -e "env_file_content=$(cat /path/to/.env.dev)"

# Staging
ansible-playbook -i inventory/staging.yml playbooks/deploy.yml \
  -e "env_file_content=$(cat /path/to/.env.staging)"

# Prod
ansible-playbook -i inventory/prod.yml playbooks/deploy.yml \
  -e "env_file_content=$(cat /path/to/.env.prod)"
```

---

## Step 7 — GitHub Secrets

Add these secrets in **GitHub → Settings → Secrets and variables → Actions**:

| Secret | Value |
|---|---|
| `HCLOUD_TOKEN` | Hetzner API token (for Terraform workflow) |
| `DEPLOY_SSH_PUBLIC_KEY` | Content of `~/.ssh/jbdh_deploy.pub` |
| `DEPLOY_SSH_KEY` | Content of `~/.ssh/jbdh_deploy` (private key) |
| `SERVER_IP` | Server IP — used by staging and prod workflows |
| `DEV_SERVER_IP` | Same server IP — used by dev workflow |
| `DEV_ENV_FILE` | Full content of your `.env.dev` file |
| `STAGING_ENV_FILE` | Full content of your `.env.staging` file |
| `PROD_ENV_FILE` | Full content of your `.env.prod` file |

### GitHub Environments (manual approval for prod)

1. Go to **Settings → Environments → New environment** → name it `production`.
2. Add yourself as a **Required reviewer**.
3. Now every push to `main` will pause and wait for your approval before deploying.

---

## Step 8 — CI/CD: how it works

```
Developer pushes to branch
         │
         ├── dev branch      →  deploy-dev.yml      →  jbdh-dev stack (port 3002/8002)
         │                       (automatic, no approval)
         │
         ├── staging branch  →  deploy-staging.yml  →  jbdh-staging stack (port 3001/8001)
         │                       (automatic, no approval)
         │
         └── main branch     →  deploy-prod.yml     →  [APPROVAL GATE]  →  jbdh-prod stack
                                  (requires reviewer approval)
```

Terraform runs automatically too:
- Push to `dev` or any PR → `terraform plan` only (no changes applied)
- Push to `main` → `terraform plan` + `terraform apply` (creates/updates servers)

### Branch strategy

| Branch | Deploys to | Trigger |
|---|---|---|
| `dev` | dev stack on shared server | automatic on push |
| `staging` | staging stack on shared server | automatic on push |
| `main` | prod stack on shared server | requires manual approval |

**Workflow:**
1. Create a feature branch from `dev`.
2. Open a PR into `dev` → merged → auto-deploys to dev stack.
3. Test on `webapp-dev.loghouse.se`.
4. Open a PR from `dev` → `staging` → merged → auto-deploys to staging.
5. Test on `webapp-staging.loghouse.se`.
6. Open a PR from `staging` → `main` → GitHub asks for reviewer approval → deploys to prod.

---

## DNS setup

All three records point to the **same server IP** (done in Cloudflare or your DNS provider):

| Record | Value |
|---|---|
| `webapp.loghouse.se` | A → `89.167.117.88` |
| `webapp-staging.loghouse.se` | A → `89.167.117.88` |
| `webapp-dev.loghouse.se` | A → `89.167.117.88` |

nginx uses `server_name` to route each domain to the correct Docker port.

---

## .gitignore additions

```gitignore
# Terraform
infrastructure/terraform/terraform.tfvars
infrastructure/terraform/.terraform/
infrastructure/terraform/*.tfstate
infrastructure/terraform/*.tfstate.backup
infrastructure/terraform/tfplan

# Ansible vault (if you use ansible-vault later)
infrastructure/ansible/**/*vault*
```

---

## Backup strategy

Two layers of protection are active for the production server:

### 1 — Hetzner Automated Backups (always on)

Configured via `backups = true` in `infrastructure/terraform/main.tf`.

- Hetzner takes a **daily snapshot** of the entire server automatically
- Keeps the **last 7 backups** (rolling window)
- Costs ~20% extra per month on top of the server price (~1–2 €/month)
- Restore via: **Hetzner Cloud Console → Servers → Backups → Restore**

### 2 — Pre-deploy Snapshots (before every prod deploy)

Triggered automatically in `.github/workflows/deploy-prod.yml` before Ansible runs.

- A snapshot named `pre-deploy-YYYYMMDD-HHMMSS` is created before each production deployment
- If the deployment breaks something, you can restore the snapshot immediately
- Old snapshots are cleaned up automatically — only the **5 most recent** are kept
- Requires the `HCLOUD_TOKEN` GitHub Secret (already needed for Terraform)

### Restore procedure

1. Go to **Hetzner Cloud Console → Project → Images → Snapshots**
2. Find the snapshot you want (either automated backup or pre-deploy)
3. Click **...** → **Rebuild server from snapshot** (or create a new server from it)

> Note: snapshots capture the full disk. To restore just the database, use `pg_dump` / `pg_restore` instead.

---

## Scalability path (future)

Once the basics work, the natural next steps are:

1. **Terraform modules** — extract reusable `server` module so dev/staging/prod share the same logic.
2. **Ansible Vault** — encrypt `env_file_content` at rest instead of storing raw in GitHub Secrets.
3. **Ansible provision role for DB** — automate `CREATE DATABASE` + `CREATE EXTENSION` in `provision.yml`.
4. **Hetzner Load Balancer** — add a `hcloud_load_balancer` resource and run 2+ prod backend servers.
5. **Managed DB** — replace Docker postgres with Hetzner Managed Databases (no backup worry).
6. **Monitoring** — Grafana + Prometheus in Docker, or Hetzner native metrics.
7. **Flower + Celery** — if you add async task queue later, Terraform just adds one more server type.
