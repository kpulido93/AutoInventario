# Infraestructura-Terraform

Este módulo despliega la infraestructura en AWS:

- Lambda function
- Secrets Manager para almacenar claves
- Role IAM con permisos

## Requisitos

- Terraform >= 1.5.0
- AWS CLI configurado
- Azure DevOps para pipeline CI/CD

## Uso

```bash
terraform init
terraform plan
terraform apply
