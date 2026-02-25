# VARA: Infrastructure Plan
## Hetzner + Terraform + CI/CD

---

## Overview

**MVP Infrastructure:**
- Single Hetzner VPS (cx22: 2vCPU, 4GB RAM, $10/month)
- PostgreSQL on same VPS
- Docker Compose for orchestration
- Terraform for IaC
- GitHub Actions for CI/CD
- Total monthly cost: ~$15

**Year 2+ (if scaling):**
- Multiple VPS or Kubernetes
- AWS migration (if needed)
- Managed database (RDS/CloudSQL)
- CDN for frontend

---

## Phase 1: MVP Hetzner Setup

### Hetzner Server Specs

**Recommended: cx22**
- 2 vCPU (Intel Xeon)
- 4 GB RAM
- 40 GB NVMe SSD
- 20 TB traffic/month included
- Ubuntu 22.04 LTS
- Cost: ~$10/month

**If handling 50+ concurrent users, upgrade to cx32:**
- 4 vCPU
- 8 GB RAM
- 80 GB SSD
- Cost: ~$20/month

### Terraform Configuration

**main.tf**
```hcl
terraform {
  required_providers {
    hcloud = {
      source  = "hetznercloud/hcloud"
      version = "~> 1.36"
    }
  }
}

provider "hcloud" {
  token = var.hcloud_token
}

# VPS for API + Web
resource "hcloud_server" "vara_api" {
  name        = "vara-api-prod"
  image       = "ubuntu-22.04"
  server_type = var.server_type  # cx22 for MVP
  location    = var.location      # fsn1 (Frankfurt) or nbg1 (Nuremberg)
  
  labels = {
    app     = "vara"
    env     = "production"
    version = "1.0.0"
  }
  
  ssh_keys = [hcloud_ssh_key.deployment.id]
  
  user_data = base64encode(templatefile("${path.module}/user_data.sh", {
    github_repo = var.github_repo
    environment = var.environment
  }))
}

# Volume for persistent data
resource "hcloud_volume" "vara_postgres" {
  name      = "vara-postgres-data"
  size      = 50  # GB
  server_id = hcloud_server.vara_api.id
  location  = var.location
  
  labels = {
    app = "vara"
    purpose = "database"
  }
}

# Firewall
resource "hcloud_firewall" "vara" {
  name = "vara-firewall"
  
  labels = {
    app = "vara"
  }
  
  # HTTP
  rules {
    direction = "in"
    protocol  = "tcp"
    port      = "80"
    source_ips = ["0.0.0.0/0", "::/0"]
  }
  
  # HTTPS
  rules {
    direction = "in"
    protocol  = "tcp"
    port      = "443"
    source_ips = ["0.0.0.0/0", "::/0"]
  }
  
  # SSH (from specific IPs if possible)
  rules {
    direction = "in"
    protocol  = "tcp"
    port      = "22"
    source_ips = var.ssh_whitelist  # Default: ["0.0.0.0/0"]
  }
  
  # Block everything else inbound
  
  applied_to_servers = [hcloud_server.vara_api.id]
}

# SSH key for deployment
resource "hcloud_ssh_key" "deployment" {
  name       = "vara-deployment"
  public_key = var.ssh_public_key
}

# Reserved IP (static, for DNS pointing)
resource "hcloud_primary_ip" "vara" {
  name              = "vara-api-ip"
  type              = "ipv4"
  server_id         = hcloud_server.vara_api.id
  assignee_resource_type = "server"
  auto_delete        = false
  
  labels = {
    app = "vara"
  }
}

output "server_ip" {
  value       = hcloud_primary_ip.vara.ip_address
  description = "Public IP address of API server"
}

output "server_id" {
  value       = hcloud_server.vara_api.id
  description = "Hetzner server ID"
}
```

**variables.tf**
```hcl
variable "hcloud_token" {
  description = "Hetzner Cloud API token"
  type        = string
  sensitive   = true
}

variable "server_type" {
  description = "Hetzner server type"
  type        = string
  default     = "cx22"
}

variable "location" {
  description = "Hetzner location (fsn1, nbg1, hel1)"
  type        = string
  default     = "fsn1"
}

variable "github_repo" {
  description = "GitHub repository URL"
  type        = string
}

variable "environment" {
  description = "Environment (production, staging)"
  type        = string
  default     = "production"
}

variable "ssh_public_key" {
  description = "Public SSH key for deployment"
  type        = string
}

variable "ssh_whitelist" {
  description = "IP addresses allowed to SSH"
  type        = list(string)
  default     = ["0.0.0.0/0"]  # Open to all for MVP, restrict later
}
```

**terraform.tfvars**
```hcl
hcloud_token    = "YOUR_HETZNER_TOKEN"
server_type     = "cx22"
location        = "fsn1"
github_repo     = "https://github.com/yourusername/vara"
environment     = "production"
ssh_public_key  = "ssh-rsa AAAA... your-key@machine"
ssh_whitelist   = ["0.0.0.0/0"]  # Restrict with your IP later
```

**outputs.tf**
```hcl
output "api_server_ip" {
  value = hcloud_primary_ip.vara.ip_address
}

output "api_server_hostname" {
  value = hcloud_server.vara_api.name
}

output "postgres_volume_id" {
  value = hcloud_volume.vara_postgres.id
}
```

### User Data Script

**user_data.sh**
```bash
#!/bin/bash
set -e

echo "=== VARA Server Initialization ==="

# Update system
apt-get update
apt-get upgrade -y
apt-get install -y \
    curl wget git \
    docker.io docker-compose \
    certbot python3-certbot-nginx \
    build-essential

# Start Docker
systemctl start docker
systemctl enable docker

# Format and mount volume
mkdir -p /var/lib/postgresql
mkfs.ext4 /dev/sdb
mount /dev/sdb /var/lib/postgresql
echo "/dev/sdb /var/lib/postgresql ext4 defaults,nofail 0 2" >> /etc/fstab

# Create deployment directory
mkdir -p /opt/vara
cd /opt/vara

# Clone repository
git clone ${github_repo} .

# Create .env file with placeholders
cat > .env << 'EOF'
# Database
POSTGRES_PASSWORD=changeme
DB_HOST=localhost
DB_PORT=5432
DB_NAME=vara

# JWT
JWT_SECRET=generate-strong-secret

# YouTube API
YOUTUBE_API_KEY=

# LLM Providers
OPENAI_API_KEY=
ANTHROPIC_API_KEY=
GROQ_API_KEY=

# Environment
ASPNETCORE_ENVIRONMENT=Production
EOF

echo "=== Setup Complete ==="
echo "Next steps:"
echo "1. SSH into server: ssh root@${SERVER_IP}"
echo "2. Update .env file: nano /opt/vara/.env"
echo "3. Start services: docker-compose up -d"
echo "4. Check status: docker-compose ps"
```

### Deployment Process

**Initial Setup:**
```bash
# Initialize Terraform
terraform init

# Plan deployment
terraform plan

# Deploy infrastructure
terraform apply

# Get server IP
terraform output api_server_ip
```

**SSH into Server:**
```bash
ssh root@$(terraform output -raw api_server_ip)

# Check docker is running
docker ps

# View logs
docker-compose logs -f
```

---

## Phase 2: Docker Compose

**docker-compose.yml**
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: vara_postgres
    restart: unless-stopped
    environment:
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${DB_NAME}
      POSTGRES_INITDB_ARGS: "--encoding=UTF8"
    ports:
      - "127.0.0.1:5432:5432"  # Only localhost access
    volumes:
      - /var/lib/postgresql/data:/var/lib/postgresql/data
      - ./scripts/init.sql:/docker-entrypoint-initdb.d/init.sql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build:
      context: ./vara-backend
      dockerfile: Dockerfile
    container_name: vara_api
    restart: unless-stopped
    ports:
      - "0.0.0.0:5000:5000"  # Listen on all interfaces for reverse proxy
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: Host=postgres;Database=${DB_NAME};Username=postgres;Password=${POSTGRES_PASSWORD}
      Jwt__Secret: ${JWT_SECRET}
      Jwt__Issuer: https://vara.yourdomain.com
      Jwt__Audience: https://vara.yourdomain.com
      YouTube__ApiKey: ${YOUTUBE_API_KEY}
      Llm__Providers__OpenAi__ApiKey: ${OPENAI_API_KEY}
      Llm__Providers__Anthropic__ApiKey: ${ANTHROPIC_API_KEY}
      Llm__Providers__Groq__ApiKey: ${GROQ_API_KEY}
    depends_on:
      postgres:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
    volumes:
      - ./logs:/app/logs

  web:
    build:
      context: ./vara-frontend
      dockerfile: Dockerfile
    container_name: vara_web
    restart: unless-stopped
    ports:
      - "0.0.0.0:3000:3000"
    environment:
      API_URL: http://api:5000
      NODE_ENV: production
    depends_on:
      - api
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:3000"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Reverse proxy with SSL
  nginx:
    image: nginx:alpine
    container_name: vara_nginx
    restart: unless-stopped
    ports:
      - "0.0.0.0:80:80"
      - "0.0.0.0:443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./certs:/etc/nginx/certs:ro
      - ./logs/nginx:/var/log/nginx
    depends_on:
      - api
      - web

volumes:
  postgres_data:
```

**Dockerfile (Backend)**
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10 as build
WORKDIR /src

COPY ["vara-backend/vara-backend.csproj", "vara-backend/"]
RUN dotnet restore "vara-backend/vara-backend.csproj"

COPY . .
RUN dotnet build "vara-backend/vara-backend.csproj" -c Release -o /app/build

FROM build as publish
RUN dotnet publish "vara-backend/vara-backend.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "vara-backend.dll"]
```

**Dockerfile (Frontend)**
```dockerfile
FROM node:18-alpine as build
WORKDIR /app

COPY vara-frontend/package*.json ./
RUN npm ci

COPY vara-frontend .
RUN npm run build

# Serve built app
FROM node:18-alpine
WORKDIR /app

RUN npm install -g serve
COPY --from=build /app/build .

EXPOSE 3000
CMD ["serve", "-s", ".", "-l", "3000"]
```

**nginx.conf**
```nginx
upstream api {
    server api:5000;
}

upstream web {
    server web:3000;
}

server {
    listen 80;
    listen [::]:80;
    server_name _;
    
    # Redirect to HTTPS
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name vara.yourdomain.com;
    
    ssl_certificate /etc/nginx/certs/fullchain.pem;
    ssl_certificate_key /etc/nginx/certs/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    
    # API routes to backend
    location /api/ {
        proxy_pass http://api/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # WebSocket for SignalR
    location /api/hubs/ {
        proxy_pass http://api;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # Frontend
    location / {
        proxy_pass http://web;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # Health check for monitoring
    location /health {
        access_log off;
        return 200 "OK\n";
        add_header Content-Type text/plain;
    }
}
```

---

## Phase 3: CI/CD with GitHub Actions

**.github/workflows/deploy.yml**
```yaml
name: Deploy to Hetzner

on:
  push:
    branches: [main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Build backend Docker image
        run: |
          docker build -t vara-api:latest ./vara-backend
          docker tag vara-api:latest ${{ secrets.DOCKER_REGISTRY }}/vara-api:latest
      
      - name: Build frontend Docker image
        run: |
          docker build -t vara-web:latest ./vara-frontend
          docker tag vara-web:latest ${{ secrets.DOCKER_REGISTRY }}/vara-web:latest
      
      - name: Login to Docker registry
        run: |
          echo ${{ secrets.DOCKER_PASSWORD }} | docker login \
            -u ${{ secrets.DOCKER_USERNAME }} --password-stdin
      
      - name: Push images
        run: |
          docker push ${{ secrets.DOCKER_REGISTRY }}/vara-api:latest
          docker push ${{ secrets.DOCKER_REGISTRY }}/vara-web:latest
      
      - name: Deploy to Hetzner
        uses: appleboy/ssh-action@master
        with:
          host: ${{ secrets.HETZNER_IP }}
          username: root
          key: ${{ secrets.SSH_PRIVATE_KEY }}
          script: |
            cd /opt/vara
            
            # Pull latest code
            git pull origin main
            
            # Pull latest images
            docker pull ${{ secrets.DOCKER_REGISTRY }}/vara-api:latest
            docker pull ${{ secrets.DOCKER_REGISTRY }}/vara-web:latest
            
            # Restart services
            docker-compose down
            docker-compose up -d
            
            # Check health
            sleep 10
            curl http://localhost:5000/health || exit 1
            
            echo "Deploy successful!"
```

**GitHub Secrets to Configure:**
```
HETZNER_IP               = your-server-ip
SSH_PRIVATE_KEY          = (private SSH key)
DOCKER_REGISTRY          = docker.io or your registry
DOCKER_USERNAME          = your-docker-user
DOCKER_PASSWORD          = your-docker-password
YOUTUBE_API_KEY          = (from Google Cloud)
OPENAI_API_KEY           = (from OpenAI)
ANTHROPIC_API_KEY        = (from Anthropic)
GROQ_API_KEY             = (from Groq)
JWT_SECRET               = (generate: openssl rand -base64 32)
POSTGRES_PASSWORD        = (generate: openssl rand -base64 32)
```

---

## Monitoring & Logging

### Basic Health Checks

```csharp
// Health check endpoint
app.MapGet("/health", () =>
{
    var health = new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        version = "1.0.0",
        database = "connected",
        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    };
    return Results.Ok(health);
});

app.MapGet("/health/ready", async (AppDbContext db) =>
{
    try
    {
        await db.Database.ExecuteSqlAsync($"SELECT 1");
        return Results.Ok(new { ready = true });
    }
    catch
    {
        return Results.ServiceUnavailable(new { ready = false });
    }
});
```

### Structured Logging

```csharp
// In Program.cs
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddFile("logs/vara.log", 
        fileSizeLimit: 10_000_000,
        retainedFileCountLimit: 5);
});

// Use structured logging
logger.LogInformation(
    "User {UserId} performed analysis {AnalysisType}",
    userId,
    analysisType);
```

### Monitoring with Prometheus (Future)

```csharp
// Add Prometheus metrics
builder.Services.AddOpenTelemetry()
    .AddConsoleExporter()
    .AddOtlpExporter();
```

---

## Backup Strategy

### PostgreSQL Backups

```bash
#!/bin/bash
# backup.sh - Run daily via cron

DB_NAME="vara"
BACKUP_DIR="/backups"
DATE=$(date +%Y%m%d_%H%M%S)

# Full backup
docker exec vara_postgres pg_dump -U postgres -d $DB_NAME | \
  gzip > $BACKUP_DIR/vara_${DATE}.sql.gz

# Keep only last 30 days
find $BACKUP_DIR -name "vara_*.sql.gz" -mtime +30 -delete

echo "Backup completed: $BACKUP_DIR/vara_${DATE}.sql.gz"
```

**Cron job:**
```bash
# Run daily at 2 AM
0 2 * * * /opt/vara/backup.sh >> /var/log/vara-backup.log 2>&1
```

### Restore from Backup

```bash
# List backups
ls -la /backups/

# Restore
gunzip < /backups/vara_20260225_020000.sql.gz | \
  docker exec -i vara_postgres psql -U postgres -d vara
```

---

## Scaling Beyond MVP

### When to Scale (Year 2+)

**Switch to AWS when:**
- More than 10K users
- Monthly costs approaching $200+
- Need auto-scaling
- Global distribution needed

**AWS migration looks like:**
```
Hetzner VPS
    ↓
AWS with:
  - ECS for containers (auto-scaling)
  - RDS for PostgreSQL
  - S3 for static assets
  - CloudFront for CDN
  - Route53 for DNS
```

---

## Security Checklist

- [ ] SSH key authentication (no passwords)
- [ ] Firewall rules (only ports 80, 443, 22)
- [ ] SSL/TLS certificates (Let's Encrypt via certbot)
- [ ] Docker run as non-root user
- [ ] PostgreSQL password hashing
- [ ] JWT secret rotation
- [ ] Rate limiting on API
- [ ] CORS properly configured
- [ ] Environment variables never in code
- [ ] Database backups tested

---

## Cost Breakdown (Monthly)

**MVP:**
- Hetzner VPS (cx22): $10
- LLM API calls (estimated): $5-20
- Backup storage: $0 (on-server)
- Domain + DNS: $1-5
- **Total: $16-35/month**

**Year 2+ (if scaling):**
- AWS EC2: $50-100
- AWS RDS: $50-100
- AWS S3/CloudFront: $10-20
- LLM APIs: $100-500 (depends on usage)
- **Total: $210-720/month**

Year 2 pricing is where SaaS revenue kicks in to cover costs.

---

## Deployment Checklist

Before going live:

- [ ] Terraform applies successfully
- [ ] Docker containers run and pass healthchecks
- [ ] PostgreSQL initialized with schema
- [ ] API responds on /health
- [ ] Frontend loads in browser
- [ ] SSL certificate installed
- [ ] Backups running daily
- [ ] Monitoring alerts configured
- [ ] DNS pointing to server IP
- [ ] GitHub Actions CI/CD working
- [ ] Error logs being captured
- [ ] Database connections pooling

---

**Infrastructure is simple for MVP. Complexity comes later when you need it.**
