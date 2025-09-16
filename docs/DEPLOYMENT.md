# Deployment Guide

Production deployment strategies for Redis Cache Patterns applications.

## Pre-Deployment Checklist

- [ ] Redis instance configured and tested
- [ ] Connection string secured in configuration
- [ ] Application built in Release mode
- [ ] Performance tests completed
- [ ] Security review completed
- [ ] Monitoring configured
- [ ] Backup strategy in place
- [ ] Rollback plan documented

## Local Development Deployment

### Using Docker Compose

```bash
# Start Redis and application
docker-compose up --build

# View logs
docker-compose logs -f app

# Stop
docker-compose down
```

**docker-compose.yml**:
```yaml
version: '3.8'
services:
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  app:
    build: .
    ports:
      - "5000:5000"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      REDIS_CONNECTION_STRING: "redis:6379"
    depends_on:
      redis:
        condition: service_healthy
    volumes:
      - .:/app

volumes:
  redis_data:
```

## Staging Deployment

### AWS EC2 Deployment

**1. Provision EC2 Instances**

```bash
# Launch t3.medium instance with Ubuntu 22.04 LTS
# Security group: Allow inbound on ports 22, 5000, 6379
```

**2. Install Runtime**

```bash
#!/bin/bash
# Install .NET 10
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0

# Install Redis
sudo apt-get install -y redis-server

# Start Redis
sudo systemctl start redis-server
sudo systemctl enable redis-server
```

**3. Deploy Application**

```bash
# Clone repository
git clone https://github.com/Sarmkadan/redis-cache-patterns.git
cd redis-cache-patterns

# Build
dotnet build -c Release

# Run
dotnet run -c Release --project RedisCachePatterns.csproj
```

**4. Configure Systemd Service**

```ini
[Unit]
Description=Redis Cache Patterns
After=network.target

[Service]
Type=simple
User=ubuntu
WorkingDirectory=/home/ubuntu/redis-cache-patterns
ExecStart=/usr/bin/dotnet /home/ubuntu/redis-cache-patterns/bin/Release/net10.0/RedisCachePatterns.dll
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable rcp
sudo systemctl start rcp
sudo systemctl status rcp
```

### Azure Container Instances

**1. Build Docker Image**

```bash
docker build -t rcp:latest .
```

**2. Tag and Push to ACR**

```bash
# Create Azure Container Registry
az acr create --resource-group mygroup --name myrcp --sku Basic

# Login
az acr login --name myrcp

# Tag and push
docker tag rcp:latest myrcp.azurecr.io/rcp:latest
docker push myrcp.azurecr.io/rcp:latest
```

**3. Deploy to Azure Container Instances**

```bash
az container create \
  --resource-group mygroup \
  --name rcp-container \
  --image myrcp.azurecr.io/rcp:latest \
  --registry-login-server myrcp.azurecr.io \
  --registry-username <username> \
  --registry-password <password> \
  --ip-address Public \
  --ports 5000 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
    REDIS_CONNECTION_STRING="redishost:6379"
```

## Production Deployment

### Kubernetes Deployment

**1. Create Kubernetes Manifests**

**deployment.yaml**:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: redis-cache-patterns
  labels:
    app: rcp
spec:
  replicas: 3
  selector:
    matchLabels:
      app: rcp
  template:
    metadata:
      labels:
        app: rcp
    spec:
      containers:
      - name: rcp
        image: myregistry.azurecr.io/rcp:1.2.0
        ports:
        - containerPort: 5000
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: REDIS_CONNECTION_STRING
          valueFrom:
            secretKeyRef:
              name: redis-secret
              key: connection-string
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 5
        resources:
          requests:
            cpu: 200m
            memory: 256Mi
          limits:
            cpu: 500m
            memory: 512Mi
```

**service.yaml**:
```yaml
apiVersion: v1
kind: Service
metadata:
  name: rcp-service
spec:
  selector:
    app: rcp
  type: LoadBalancer
  ports:
  - protocol: TCP
    port: 80
    targetPort: 5000
```

**2. Deploy**

```bash
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml

# Verify
kubectl get deployments
kubectl get services
kubectl logs -l app=rcp
```

### Redis Enterprise Deployment

**1. Azure Database for Redis**

```bash
# Create Redis instance
az redis create \
  --resource-group mygroup \
  --name myredis \
  --location eastus \
  --sku Premium \
  --vm-size p1

# Get connection string
az redis show-connection-string --name myredis --resource-group mygroup
```

**2. Configure Application**

```json
{
  "Redis": {
    "ConnectionString": "myredis.redis.cache.windows.net:6379,password=...,ssl=true,abortConnect=false"
  }
}
```

## Configuration Management

### Environment Variables

```bash
export ASPNETCORE_ENVIRONMENT=Production
export REDIS_CONNECTION_STRING="redis.prod.com:6379,password=secure"
export CACHE_DEFAULT_EXPIRATION=3600
export CACHE_ENABLE_COMPRESSION=true
export LOG_LEVEL=Information
```

### Secrets Management

**Azure Key Vault**:
```csharp
var builder = WebApplication.CreateBuilder(args);

var keyVaultEndpoint = new Uri("https://myvault.vault.azure.net/");
builder.Configuration.AddAzureKeyVault(
    keyVaultEndpoint,
    new DefaultAzureCredential());
```

**AWS Secrets Manager**:
```csharp
var client = new SecretsManagerClient();
var secret = await client.GetSecretValueAsync(
    new GetSecretValueRequest { SecretId = "redis-connection" });
```

## Monitoring & Observability

### Application Insights Integration

```csharp
services.AddApplicationInsightsTelemetry();
services.AddLogging(builder =>
{
    builder.AddApplicationInsights();
});
```

### Prometheus Metrics

```bash
# Expose metrics endpoint
app.MapGet("/metrics", context =>
{
    var collector = context.RequestServices.GetRequiredService<CacheMetricsCollector>();
    return collector.GetMetricsAsync();
});
```

### Health Checks

```csharp
services.AddHealthChecks()
    .AddRedis(Configuration["Redis:ConnectionString"])
    .AddCheck<CustomHealthCheck>("custom");

app.MapHealthChecks("/health");
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

## Performance Tuning

### Redis Configuration

```conf
# redis.conf
maxmemory 2gb
maxmemory-policy allkeys-lru
timeout 300
tcp-keepalive 60
```

### Application Configuration

```json
{
  "Redis": {
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "MaxPoolSize": 50
  },
  "Cache": {
    "DefaultExpirationSeconds": 3600,
    "EnableCompression": true,
    "CompressionThreshold": 512,
    "LockTimeoutSeconds": 30
  }
}
```

## Scaling Strategies

### Horizontal Scaling

**Load Balancing**:
```nginx
upstream rcp {
    server app1:5000;
    server app2:5000;
    server app3:5000;
}

server {
    listen 80;
    location / {
        proxy_pass http://rcp;
    }
}
```

**Cache Warmers**:
```csharp
// Run on each instance startup
var warmer = scope.ServiceProvider.GetRequiredService<CacheWarmingService>();
await warmer.WarmCacheAsync();
```

### Vertical Scaling

- Increase Redis memory: `maxmemory 4gb`
- Increase application resources
- Optimize serialization

## Backup & Recovery

### Redis Backup

```bash
# Enable RDB snapshots
save 900 1          # Save after 900s if 1 key changed
save 300 10         # Save after 300s if 10 keys changed
save 60 10000       # Save after 60s if 10000 keys changed

# Enable AOF persistence
appendonly yes
appendfsync everysec
```

### Automated Backups

```bash
#!/bin/bash
# backup.sh
BACKUP_DIR="/backups/redis"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

redis-cli BGSAVE
cp /var/lib/redis/dump.rdb $BACKUP_DIR/dump_$TIMESTAMP.rdb

# Upload to S3
aws s3 cp $BACKUP_DIR/dump_$TIMESTAMP.rdb s3://mybackups/redis/
```

## Disaster Recovery

### Point-in-Time Recovery

```bash
# Restore from backup
redis-server --appendonly no
# Stop redis
sudo systemctl stop redis-server

# Restore dump
cp /backups/redis/dump.rdb /var/lib/redis/dump.rdb
sudo chown redis:redis /var/lib/redis/dump.rdb

# Start redis
sudo systemctl start redis-server
```

### Multi-Region Replication

```bash
# Primary Redis
redis-server

# Secondary Redis (read-only replica)
redis-server --slaveof primary-redis-ip 6379 --read-only
```

## Security Hardening

### Network Security

```bash
# Restrict Redis to localhost
bind 127.0.0.1

# Use Redis AUTH
requirepass "strong_password_here"

# Enable SSL/TLS
port 0
tls-port 6379
tls-cert-file /path/to/cert.pem
tls-key-file /path/to/key.pem
```

### Application Security

```csharp
// Require authentication
builder.Services.AddAuthentication()
    .AddBearerToken();

// Add authorization
app.UseAuthentication();
app.UseAuthorization();

// Rate limiting
services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromSeconds(60)
            }));
});

app.UseRateLimiter();
```

## Deployment Verification

### Health Check

```bash
curl http://localhost:5000/health

# Expected response:
# {"status":"Healthy"}
```

### Redis Connection Test

```bash
redis-cli ping
# Expected: PONG
```

### Load Testing

```bash
# Using Apache Bench
ab -n 1000 -c 10 http://localhost:5000/api/products/1

# Using wrk
wrk -t4 -c100 -d30s http://localhost:5000/api/products/1
```

## Rollback Procedure

```bash
# If deployment fails
kubectl rollout undo deployment/redis-cache-patterns

# Or with Docker
docker stop current_container
docker run -d previous_image:tag

# Monitor logs
kubectl logs -l app=rcp
```

## Post-Deployment

### Monitoring

- [ ] Check application metrics in Application Insights
- [ ] Monitor Redis memory usage
- [ ] Verify cache hit rates
- [ ] Monitor error rates

### Documentation

- [ ] Update deployment runbook
- [ ] Document connection strings (in vault)
- [ ] Document scaling thresholds
- [ ] Document rollback procedure

### Maintenance

- [ ] Schedule backup tests
- [ ] Plan capacity expansion
- [ ] Review security logs
- [ ] Update dependencies

## Support & Troubleshooting

**Common issues**:

1. **Redis connection timeout**: Check firewall, increase timeout
2. **Memory issues**: Enable compression, review eviction policy
3. **Slow queries**: Check network, monitor response times
4. **Data loss**: Verify persistence enabled, test recovery

See [FAQ.md](FAQ.md) for more troubleshooting.
