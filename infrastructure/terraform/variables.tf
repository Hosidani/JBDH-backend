# ─────────────────────────────────────────────
# Required — set these in terraform.tfvars
# or via TF_VAR_* environment variables
# ─────────────────────────────────────────────

variable "hcloud_token" {
  description = "Hetzner Cloud API token (read+write). Store in GitHub Secrets as HCLOUD_TOKEN."
  type        = string
  sensitive   = true
}

variable "ssh_public_key" {
  description = "SSH public key content to install on servers."
  type        = string
}

# ─────────────────────────────────────────────
# Project defaults
# ─────────────────────────────────────────────

variable "project_name" {
  description = "Short name used as prefix for all resources."
  type        = string
  default     = "jbdh"
}

variable "deploy_user" {
  description = "Linux user created on each server for deployments."
  type        = string
  default     = "deploy"
}

# ─────────────────────────────────────────────
# Server config
# ─────────────────────────────────────────────

variable "server_image" {
  description = "Hetzner base image. ubuntu-24.04 is LTS."
  type        = string
  default     = "ubuntu-24.04"
}

variable "location" {
  description = "Hetzner datacenter location. Options: nbg1, fsn1, hel1, ash, hil."
  type        = string
  default     = "hel1"   # Helsinki — matches your existing ubuntu-4gb-hel1-1 server
}

variable "network_zone" {
  description = "Must match the location. eu-central for nbg1/fsn1/hel1."
  type        = string
  default     = "eu-central"
}

# Your existing server is 4GB RAM = cx22 (2vCPU/4GB)
# cx22 hosts both staging + prod Docker stacks.
variable "server_type" {
  description = "Hetzner server type for the main app server (hosts staging + prod)."
  type        = string
  default     = "cx22"
}

variable "dev_server_type" {
  description = "Hetzner server type for the future dev server."
  type        = string
  default     = "cx22"
}

# ─────────────────────────────────────────────
# Environment toggles
# ─────────────────────────────────────────────

variable "create_dev" {
  description = "Set to false until the dev branch workflow is ready."
  type        = bool
  default     = false
}
