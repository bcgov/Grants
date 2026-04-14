# Messaging System

The Portal communicates with external systems (e.g. UNITY) via an **Outbox / Inbox** pattern backed by RabbitMQ. Messages are persisted in the database first, then published asynchronously by background jobs. This provides at-least-once delivery guarantees and full auditability.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Outbox Message Lifecycle](#outbox-message-lifecycle)
- [Inbox Message Lifecycle](#inbox-message-lifecycle)
- [Background Jobs](#background-jobs)
- [Ack Loop & Failure Handling](#ack-loop--failure-handling)
- [Plugin Events & Cache Invalidation](#plugin-events--cache-invalidation)
- [Configuration Reference](#configuration-reference)
- [Distributed Locking](#distributed-locking)
- [Circuit Breaker](#circuit-breaker)

---

## Architecture Overview

```
Portal                          RabbitMQ                     External System (UNITY)
──────                          ────────                     ───────────────────────
  Use Case
    │
    ├──▶ OutboxMessage (Pending)
    │       │
    │   OutboxProcessorJob ──publish──▶ Exchange ──▶ Queue ──▶ Processes command
    │       │                                                       │
    │   OutboxMessage (Published)                                   │
    │       │                                                       │
    │   OutboxTimeoutJob                                    Sends ack back
    │     (Published older than                                     │
    │      threshold → TimedOut)                                    │
    │                                                               │
    │                           Exchange ◀──ack──┘
    │                               │
    │   InboxMessage (Pending) ◀────┘
    │       │
    │   InboxProcessorJob
    │     ├── Routes to plugin handler
    │     ├── Marks outbox message Acknowledged
    │     └── On FAILED ack: records PluginEvent + invalidates cache
    │
    └──▶ User sees PluginEvent in UI
```

---

## Outbox Message Lifecycle

```
Pending → Processing → Published → Acknowledged   (happy path — ack received)
                                 → TimedOut        (no ack within threshold)
                                 → TimedOut → Acknowledged  (late ack after timeout)
                     → Failed                      (publish failed after max retries)
```

| Status | Meaning |
|--------|---------|
| `Pending` | Queued for publishing |
| `Processing` | Locked by the outbox processor job |
| `Published` | Successfully sent to RabbitMQ, awaiting external ack |
| `Acknowledged` | External system acknowledged receipt (SUCCESS or FAILED) |
| `TimedOut` | No ack received within `AckTimeoutMinutes` |
| `Failed` | Publishing failed after `MaxRetries` attempts |

### Key behaviours

- **Lock expiry**: If the outbox processor crashes mid-publish, the lock expires and the message returns to `Pending` for retry.
- **Late ack safety**: If an ack arrives after a message is already `TimedOut`, it is still processed. The cache was already invalidated by the timeout job, so the next read fetches fresh data — no stale data risk.

---

## Inbox Message Lifecycle

```
Pending → Processing → Processed   (handler succeeded)
                     → Failed      (handler failed after max retries)
          Duplicate                (duplicate MessageId detected)
```

| Status | Meaning |
|--------|---------|
| `Pending` | Received, queued for processing |
| `Processing` | Locked by the inbox processor job |
| `Processed` | Handler completed successfully |
| `Failed` | Handler failed after `MaxRetries` |
| `Duplicate` | Same `MessageId` already exists — idempotency guard |

---

## Background Jobs

All jobs use **Quartz.NET** with `[DisallowConcurrentExecution]`, distributed locking, and the `JobCircuitBreaker`.

| Job | Purpose | Default Schedule |
|-----|---------|------------------|
| **OutboxProcessorJob** | Publishes `Pending` outbox messages to RabbitMQ | Every 30s |
| **InboxProcessorJob** | Processes `Pending` inbox messages via plugin handlers | Every 15s |
| **OutboxTimeoutJob** | Detects `Published` messages with no ack past threshold; records `PluginEvent` (Error/AckTimeout) + marks `TimedOut` | Every 60s |
| **MessageCleanupJob** | Deletes terminal-status messages older than retention period | Hourly (1-min startup delay) |

### Startup delay

The `MessageCleanupJob` starts after a configurable delay (`BackgroundJobs:StartupDelaySeconds`, default `60`) to avoid contention with application boot (EF migrations, cache warm-up, broker connections).

### Cleanup scope

The cleanup job only deletes messages in terminal statuses — it **never** touches `Pending` or `Processing`:

- **Outbox**: `Published`, `Failed`, `TimedOut`, `Acknowledged`
- **Inbox**: `Processed`, `Failed`, `Duplicate`

Both use `ExecuteDeleteAsync()` for efficient single-SQL bulk deletion with no entity tracking overhead.

---

## Ack Loop & Failure Handling

When the `InboxProcessorJob` processes a `MessageAcknowledgment`, it closes the outbox loop:

1. Deserializes the ack to get `OriginalMessageId` and `Status`
2. Looks up the original outbox message by `OriginalMessageId`
3. If `ack.Status == "FAILED"`:
   - Parses the outbox payload via `IPluginCommandMetadataRegistry` to extract profile/data context
   - Records a `PluginEvent` with `Severity = Error` and `Source = InboxRejection`
   - This automatically triggers cache invalidation for the relevant segment
4. Marks the outbox message as `Acknowledged`

### All failure paths

| Scenario | Event Source | Triggers PluginEvent? | Invalidates Cache? |
|----------|-------------|:---:|:---:|
| Outbox publish fails (retries exhausted) | `OutboxFailure` | ✅ | ✅ |
| Published but no ack within threshold | `AckTimeout` | ✅ | ✅ |
| FAILED ack received from external system | `InboxRejection` | ✅ | ✅ |
| SUCCESS ack received | _(none)_ | ❌ | ❌ |

---

## Plugin Events & Cache Invalidation

`PluginEvent` is the mechanism for notifying users about operation outcomes and triggering compensating actions.

### How it works

1. A failure is detected (outbox failure, timeout, or FAILED ack)
2. The job creates a `PluginEventContext` with:
   - `ProfileId`, `PluginId`, `Provider`, `DataType` — parsed from the outbox payload
   - `Severity = Error` — triggers compensation
   - `Source` — one of `OutboxFailure`, `AckTimeout`, `InboxRejection`
   - `UserMessage` — human-readable message shown to the user
   - `TechnicalDetails` — diagnostic info for debugging
3. `PluginEventService.RecordAsync()`:
   - Persists the event to the database
   - For `Error` severity: resolves the cache segment via `IPluginCommandMetadataRegistry.GetCacheSegment()` and calls `IProfileCacheInvalidationService.InvalidateProfileDataCacheAsync()`
4. The next time the user reads data, the cache miss forces a fresh fetch from the external system

### Event sources

| Source | Value | When |
|--------|-------|------|
| `OutboxFailure` | 0 | Outbox message publish failed after max retries |
| `InboxRejection` | 1 | External system returned a FAILED acknowledgment |
| `ExternalNotification` | 2 | External system sent a notification or status update |
| `System` | 3 | System-generated event (cache refresh, background job) |
| `Plugin` | 4 | Plugin-specific business logic |
| `AckTimeout` | 5 | Published message received no ack within timeout |

---

## Configuration Reference

All settings live under `Messaging` in `appsettings.json`:

```json
{
  "Messaging": {
    "Outbox": {
      "PollingIntervalSeconds": 30,
      "BatchSize": 100,
      "MaxRetries": 5,
      "RetentionDays": 7,
      "CleanupIntervalHours": 24,
      "AckTimeoutMinutes": 5,
      "AckTimeoutPollingIntervalSeconds": 60
    },
    "Inbox": {
      "PollingIntervalSeconds": 15,
      "BatchSize": 50,
      "MaxRetries": 3,
      "RetentionDays": 7,
      "CleanupIntervalHours": 24
    },
    "BackgroundJobs": {
      "Enabled": true,
      "MaxConcurrency": 4,
      "MisfireThresholdSeconds": 60,
      "BaseBackoffSeconds": 15,
      "MaxBackoffSeconds": 300,
      "BackoffMultiplier": 2.0,
      "LogEveryNthFailure": 20,
      "StartupDelaySeconds": 60
    },
    "DistributedLocks": {
      "DefaultTimeoutMinutes": 5,
      "RenewalIntervalMinutes": 2,
      "WaitTimeoutSeconds": 5
    },
    "RabbitMQ": {
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest",
      "VirtualHost": "/",
      "RetryCount": 3,
      "RetryDelay": "00:00:30",
      "ConnectionTimeout": "00:00:30",
      "UseSsl": false
    }
  }
}
```

### Key settings

| Setting | Default | Description |
|---------|---------|-------------|
| `Outbox:AckTimeoutMinutes` | `5` | Minutes after publishing before a message with no ack is timed out. Set to `0` to disable. |
| `Outbox:AckTimeoutPollingIntervalSeconds` | `60` | How often the timeout job checks for stale published messages. |
| `BackgroundJobs:StartupDelaySeconds` | `60` | Delay before the cleanup job starts after application boot. |
| `BackgroundJobs:Enabled` | `true` | Master switch for all background jobs. |

---

## Distributed Locking

The system automatically selects the locking provider based on Redis availability:

| | Redis Mode | In-Memory Mode |
|--|------------|----------------|
| **Triggered when** | `ConnectionStrings:Redis` is present | No Redis configuration |
| **Implementation** | `RedisDistributedLock` | `InMemoryDistributedLock` |
| **Multi-pod safe** | ✅ Yes | ⚠️ Single-pod only |
| **External dependencies** | Redis required | None |
| **Lock persistence** | Survives app restarts | Lost on restart |
| **Best for** | Production (horizontal scaling) | Development, tests, single-pod |

### Production (multi-pod with Redis)
```json
{
  "ConnectionStrings": {
    "Redis": "redis-service:6379"
  }
}
```

### Development (no Redis needed)
```json
{
  "ConnectionStrings": {
    // No Redis configuration — uses in-memory locking automatically
  }
}
```

---

## Circuit Breaker

All background jobs use `JobCircuitBreaker` to prevent log flooding and wasted work during infrastructure outages:

```csharp
public class SomeBackgroundJob : IJob
{
    private readonly IDistributedLock _distributedLock;
    private readonly JobCircuitBreaker _circuitBreaker;
    private const string JobKey = "my-job";

    public async Task Execute(IJobExecutionContext context)
    {
        if (!_circuitBreaker.ShouldExecute(JobKey))
            return;

        try
        {
            var lockResult = await _distributedLock.AcquireLockAsync(
                JobKey, TimeSpan.FromMinutes(5));

            if (lockResult.IsSuccess)
            {
                try
                {
                    // Do work...
                }
                finally
                {
                    await _distributedLock.ReleaseLockAsync(JobKey, lockResult.Value);
                }
            }

            _circuitBreaker.RecordSuccess(JobKey);
        }
        catch (Exception ex)
        {
            // Exponential backoff with periodic log reminders:
            // 1st failure  → Error (full stack trace)
            // 2nd–19th     → Debug (suppressed)
            // Every 20th   → Warning (periodic reminder)
            // Recovery     → Information ("circuit recovered after N failures")
            _circuitBreaker.RecordFailure(JobKey, ex);
        }
    }
}
```