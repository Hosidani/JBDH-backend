output "server_ip" {
  description = "Public IPv4 of the app server (hosts staging + prod)."
  value       = hcloud_server.app.ipv4_address
}

output "server_name" {
  description = "Name of the app server."
  value       = hcloud_server.app.name
}

output "dev_ip" {
  description = "Public IPv4 of the dev server (created only when create_dev = true)."
  value       = length(hcloud_server.dev) > 0 ? hcloud_server.dev[0].ipv4_address : null
}

output "ssh_fingerprint" {
  description = "Fingerprint of the deployed SSH key."
  value       = hcloud_ssh_key.deployer.fingerprint
}
