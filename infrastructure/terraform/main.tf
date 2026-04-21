# ─────────────────────────────────────────────
# SSH Key
# ─────────────────────────────────────────────
resource "hcloud_ssh_key" "deployer" {
  name       = "${var.project_name}-deployer"
  public_key = var.ssh_public_key
}

# ─────────────────────────────────────────────
# Firewall — shared rules for all app servers
# ─────────────────────────────────────────────
resource "hcloud_firewall" "app" {
  name = "${var.project_name}-app-firewall"

  # SSH
  rule {
    direction  = "in"
    protocol   = "tcp"
    port       = "22"
    source_ips = ["0.0.0.0/0", "::/0"]
  }

  # HTTP
  rule {
    direction  = "in"
    protocol   = "tcp"
    port       = "80"
    source_ips = ["0.0.0.0/0", "::/0"]
  }

  # HTTPS
  rule {
    direction  = "in"
    protocol   = "tcp"
    port       = "443"
    source_ips = ["0.0.0.0/0", "::/0"]
  }

  # Allow all outbound
  rule {
    direction       = "out"
    protocol        = "tcp"
    port            = "any"
    destination_ips = ["0.0.0.0/0", "::/0"]
  }

  rule {
    direction       = "out"
    protocol        = "udp"
    port            = "any"
    destination_ips = ["0.0.0.0/0", "::/0"]
  }

  rule {
    direction       = "out"
    protocol        = "icmp"
    destination_ips = ["0.0.0.0/0", "::/0"]
  }
}

# ─────────────────────────────────────────────────────────────────────
# Single application server
# Hosts ALL environments (staging + prod) as separate Docker stacks.
# To add a dedicated dev server in the future, set create_dev = true.
# ─────────────────────────────────────────────────────────────────────
resource "hcloud_server" "app" {
  name         = "${var.project_name}-app"
  image        = var.server_image
  server_type  = var.server_type
  location     = var.location
  ssh_keys     = [hcloud_ssh_key.deployer.id]
  firewall_ids = [hcloud_firewall.app.id]

  labels = {
    project = var.project_name
  }

  # cloud-init only runs at first boot — ignore_changes prevents destroy/recreate
  # when importing an existing server.
  user_data = templatefile("${path.module}/templates/cloud-init.yaml.tpl", {
    deploy_user    = var.deploy_user
    ssh_public_key = var.ssh_public_key
  })

  lifecycle {
    ignore_changes = [user_data, image]
    # Prevent accidental deletion of the server
    prevent_destroy = true
  }
}

# ─────────────────────────────────────────────────────────────────────
# Dev server — created only when create_dev = true
# Will run on a separate server when the dev branch workflow is ready.
# ─────────────────────────────────────────────────────────────────────
resource "hcloud_server" "dev" {
  count        = var.create_dev ? 1 : 0
  name         = "${var.project_name}-dev"
  image        = var.server_image
  server_type  = var.dev_server_type
  location     = var.location
  ssh_keys     = [hcloud_ssh_key.deployer.id]
  firewall_ids = [hcloud_firewall.app.id]

  labels = {
    environment = "dev"
    project     = var.project_name
  }

  user_data = templatefile("${path.module}/templates/cloud-init.yaml.tpl", {
    deploy_user    = var.deploy_user
    ssh_public_key = var.ssh_public_key
  })

  lifecycle {
    ignore_changes = [user_data, image]
  }
}
