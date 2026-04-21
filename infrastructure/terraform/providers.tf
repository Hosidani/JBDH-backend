terraform {
  required_version = ">= 1.6.0"

  required_providers {
    hcloud = {
      source  = "hetznercloud/hcloud"
      version = "~> 1.47"
    }
  }

  # Remote state: Hetzner Object Storage (S3-compatible)
  # Uncomment and configure after creating the bucket:
  #
  # backend "s3" {
  #   bucket                      = "jbdh-terraform-state"
  #   key                         = "terraform.tfstate"
  #   region                      = "eu-central-1"     # dummy value, required by S3 provider
  #   endpoint                    = "https://fsn1.your-objectstorage.com"
  #   access_key                  = var.hetzner_s3_access_key
  #   secret_key                  = var.hetzner_s3_secret_key
  #   skip_credentials_validation = true
  #   skip_metadata_api_check     = true
  #   skip_region_validation      = true
  #   force_path_style            = true
  # }
}

provider "hcloud" {
  token = var.hcloud_token
}
