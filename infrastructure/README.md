# Infrastructure — Terraform + Ansible + CI/CD

This folder contains all Infrastructure as Code (IaC) for the JBDH project on Hetzner Cloud.

```
infrastructure/
├── terraform/          # Provisions the cloud servers
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
    │   ├── staging.yml
    │   └── prod.yml
    ├── group_vars/
    │   ├── all.yml
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
- `~/.ssh/jbdh_deploy`     → `STAGING_DEPLOY_SSH_KEY` + `PROD_DEPLOY_SSH_KEY` GitHub Secrets

---

## Step 3 — Terraform: create servers

```bash
cd infrastructure/terraform

# 1. Copy the example vars file
cp terraform.tfvars.example terraform.tfvars

# 2. Edit terraform.tfvars — set hcloud_token and ssh_public_key

# 3. Init (downloads the Hetzner provider)
terraform init

# 4. Preview what will be created
terraform plan

# 5. Create the servers
terraform apply
```

After apply, note the server IPs printed in the output:
```
staging_ip = "1.2.3.4"
prod_ip    = "5.6.7.8"
```

---

## Step 4 — Update Ansible inventory

Replace `REPLACE_WITH_TERRAFORM_OUTPUT_*_IP` in:
- `ansible/inventory/staging.yml` → staging_ip
- `ansible/inventory/prod.yml`    → prod_ip

Also update in `ansible/group_vars/all.yml`:
- `git_repo` → your GitHub SSH repo URL
- `certbot_email` → your email

---

## Step 5 — Ansible: provision servers

Run this **once per server** after Terraform creates them:

```bash
cd infrastructure/ansible

# Provision staging
ansible-playbook -i inventory/staging.yml playbooks/provision.yml

# Provision prod
ansible-playbook -i inventory/prod.yml playbooks/provision.yml
```

This installs: UFW, fail2ban, Docker, nginx, and obtains SSL certificates.

---

## Step 6 — Manual first deployment

Before CI/CD is wired up, deploy manually:

```bash
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
| `STAGING_DEPLOY_SSH_KEY` | Content of `~/.ssh/jbdh_deploy` (private) |
| `STAGING_SERVER_IP` | IP from `terraform output staging_ip` |
| `STAGING_ENV_FILE` | Full content of your `.env.staging` file |
| `PROD_DEPLOY_SSH_KEY` | Content of `~/.ssh/jbdh_deploy` (private) |
| `PROD_SERVER_IP` | IP from `terraform output prod_ip` |
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
         ├── staging branch  →  deploy-staging.yml  →  staging server
         │                       (automatic, no approval)
         │
         └── main branch     →  deploy-prod.yml     →  [APPROVAL GATE]  →  prod server
                                  (requires reviewer approval)
```

### Branch strategy

| Branch | Deploys to | Trigger |
|---|---|---|
| `dev` (future) | dev server | automatic on push |
| `staging` | staging server | automatic on push |
| `main` | prod server | requires manual approval |

**Workflow:**
1. Create a feature branch from `dev` (or `staging`).
2. Open a PR into `staging` → merged → auto-deploys to staging.
3. Test on `webapp-staging.loghouse.se`.
4. Open a PR from `staging` → `main` → GitHub asks for reviewer approval → deploys to prod.

---

## Step 9 — Dev environment (future)

When ready:

1. Set `create_dev = true` in `terraform.tfvars` → `terraform apply`.
2. Add `ansible/inventory/dev.yml` and `ansible/group_vars/dev.yml`.
3. Run `ansible-playbook -i inventory/dev.yml playbooks/provision.yml`.
4. Add the `.github/workflows/deploy-dev.yml` workflow.
5. Add `VITE_MODE: development` to `docker-compose.dev.yml` (fork of staging).
6. Point DNS: `webapp-dev.loghouse.se` → dev server IP.

---

## DNS setup

For each server (done in Cloudflare or your DNS provider):

| Record | Value |
|---|---|
| `webapp.loghouse.se` | A → prod_ip |
| `webapp-staging.loghouse.se` | A → staging_ip |
| `webapp-dev.loghouse.se` | A → dev_ip (future) |

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

## Scalability path (future)

Once the basics work, the natural next steps are:

1. **Terraform modules** — extract reusable `server` module so dev/staging/prod share the same logic.
2. **Ansible Vault** — encrypt `env_file_content` at rest instead of storing raw in GitHub Secrets.
3. **Hetzner Load Balancer** — add a `hcloud_load_balancer` resource and run 2+ prod backend servers.
4. **Managed DB** — replace Docker postgres with Hetzner Managed Databases (no backup worry).
5. **Monitoring** — Grafana + Prometheus in Docker, or Hetzner native metrics.
6. **Flower + Celery** — if you add async task queue later, Terraform just adds one more server type.
