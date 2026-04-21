#cloud-config
# Minimal cloud-init — only creates the deploy user.
# All further configuration is handled by Ansible.

users:
  - name: ${deploy_user}
    groups: [sudo, docker]
    shell: /bin/bash
    sudo: ALL=(ALL) NOPASSWD:ALL
    ssh_authorized_keys:
      - ${ssh_public_key}

# Disable root SSH login
disable_root: true

# Keep cloud-init fast — only reboot if the kernel needs it
package_update: false
package_upgrade: false
