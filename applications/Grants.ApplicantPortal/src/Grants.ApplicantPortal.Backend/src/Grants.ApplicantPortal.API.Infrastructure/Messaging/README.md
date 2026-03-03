# Messaging System - Distributed Locking Configuration

The messaging system automatically adapts to your deployment scenario by detecting Redis configuration in your `appsettings.json`.

## ?? **Automatic Mode Selection**

### **Redis Mode** (Multi-Pod/Distributed Deployments)
**Triggered when:** Redis connection string is present in configuration
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

**Uses:** `RedisDistributedLock`
**Benefits:**
- ? **True distributed locking** across multiple pods
- ? **Prevents work overlap** when scaled horizontally  
- ? **Atomic operations** using Lua scripts
- ? **Lock expiry and renewal** for long-running jobs

### **In-Memory Mode** (Single-Pod/Development Deployments)  
**Triggered when:** No Redis connection string found
```json
{
  "ConnectionStrings": {
    // No Redis configuration
  }
}
```

**Uses:** `InMemoryDistributedLock` with `IDistributedMemoryCache`
**Benefits:**
- ? **No external dependencies** required
- ? **Perfect for development** and single-pod deployments
- ? **Same interface** as Redis version - no code changes needed
- ? **Built-in .NET abstractions** - reliable and tested

## ?? **Configuration Examples**

### Production (Multi-Pod with Redis)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=grants;Username=grants;Password=password",
    "Redis": "redis:6379"
  },
  "Messaging": {
    "Outbox": {
      "PollingIntervalSeconds": 30,
      "BatchSize": 100
    },
    "DistributedLocks": {
      "DefaultTimeoutMinutes": 5,
      "WaitTimeoutSeconds": 5
    }
  }
}
```

**Result:** Uses Redis-based distributed locking ?

### Development (Single-Pod, No Redis)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=grants_dev;Username=dev;Password=password"
  },
  "Messaging": {
    "Outbox": {
      "PollingIntervalSeconds": 10,
      "BatchSize": 50  
    }
  }
}
```

**Result:** Uses in-memory distributed locking ?

## ?? **Behavior Comparison**

| Feature | Redis Mode | In-Memory Mode |
|---------|------------|----------------|
| **Multi-Pod Safe** | ? Yes | ?? Single-pod only |
| **External Dependencies** | Redis required | None |
| **Lock Persistence** | Survives app restarts | Lost on restart |
| **Performance** | Network latency | In-process |
| **Development Friendly** | Requires Redis setup | Works immediately |
| **Production Ready** | ? Yes | Single-pod only |

## ?? **Deployment Scenarios**

### **Kubernetes/OpenShift (Multi-Pod)**
```yaml
# Use Redis mode for horizontal scaling
apiVersion: v1
kind: ConfigMap
metadata:
  name: grants-config
data:
  appsettings.json: |
    {
      "ConnectionStrings": {
        "Redis": "redis-service:6379"
      }
    }
```

### **Docker Compose (Development)**
```yaml
version: '3.8'
services:
  backend:
    # No Redis service = automatic in-memory mode
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=grants;
```

### **Docker Compose (Production-like)**
```yaml
version: '3.8'
services:
  backend:
    environment:
      - ConnectionStrings__Redis=redis:6379
  redis:
    image: redis:alpine
```

## ?? **Code Usage (Same Regardless of Mode)**

```csharp
public class SomeBackgroundJob : IJob
{
    private readonly IDistributedLock _distributedLock;
    
    public async Task Execute(IJobExecutionContext context)
    {
        // Works the same whether using Redis or in-memory!
        var lockResult = await _distributedLock.AcquireLockAsync(
            "my-job-lock", 
            TimeSpan.FromMinutes(5));
            
        if (lockResult.IsSuccess)
        {
            try
            {
                // Do work...
            }
            finally
            {
                await _distributedLock.ReleaseLockAsync("my-job-lock", lockResult.Value);
            }
        }
    }
}
```

## ?? **How Detection Works**

The system checks for Redis configuration at startup:

```csharp
var redisConnectionString = configuration.GetConnectionString("Redis");
var hasRedisConfig = !string.IsNullOrEmpty(redisConnectionString);

if (hasRedisConfig)
{
    // Register RedisDistributedLock
}
else
{
    // Register InMemoryDistributedLock + IDistributedMemoryCache
}
```

## ?? **Recommendations**

- **Development:** Use in-memory mode (no Redis configuration)
- **Single-Pod Production:** Use in-memory mode if only one instance
- **Multi-Pod Production:** Always use Redis mode
- **Testing:** In-memory mode is perfect for unit/integration tests

The system will log which mode it's using at startup, so you can always verify the configuration is working as expected.