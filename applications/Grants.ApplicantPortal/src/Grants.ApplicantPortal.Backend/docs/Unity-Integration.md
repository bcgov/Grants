# Unity Grant Manager Integration

## Overview

The Applicant Portal integrates with Unity Grant Manager through two mechanisms:

1. **REST API** - For synchronous queries (profile info, tenant associations)
2. **RabbitMQ Messaging** - For asynchronous communication (commands, events, notifications)

This document provides a high-level overview of both integration points.

> **?? Detailed Technical Documentation**: For complete API specifications, authentication details, and implementation guidance, see the [Unity Grant Manager Integration Guide](https://github.com/bcgov/Unity/blob/main/applications/Unity.GrantManager/docs/ApplicantPortalIntegration.md)

## Quick Start

### 1. Obtain API Key
Contact the Unity Grant Manager team to obtain an API key for your environment (dev/test/prod).

### 2. Configure API Key
Store the API key securely in your application configuration:

```json
{
  "UnityIntegration": {
    "BaseUrl": "https://unity.example.com",
    "ApiKey": "your-api-key-here"
  }
}
```

### 3. Make API Requests
Include the API key in the `X-API-Key` header:

```http
GET /api/app/applicant-profiles/tenants?ProfileId=abc&Subject=user@idp
Host: unity.example.com
X-API-Key: your-api-key-here
```

## Integration Points

### 1. Get Applicant Tenants
Retrieves the list of grant programs (tenants) an applicant has access to based on their OIDC identity.

**Use Case**: Display list of available grant programs on portal homepage after user authentication.

**Endpoint**: `GET /api/app/applicant-profiles/tenants`

**Parameters**:
- `ProfileId`: Your internal user identifier
- `Subject`: OIDC subject from user's authentication token

**Response**:
```json
[
  {
    "tenantId": "guid",
    "tenantName": "Housing Grant Program"
  },
  {
    "tenantId": "guid",
    "tenantName": "Business Development Fund"
  }
]
```

### 2. Get Applicant Profile
Retrieves basic profile information for an applicant within a specific tenant.

**Use Case**: Display applicant information in tenant-specific context.

**Endpoint**: `GET /api/app/applicant-profiles/profile`

**Parameters**:
- `ProfileId`: Your internal user identifier
- `Subject`: OIDC subject from user's authentication token
- `TenantId`: The specific tenant to query

**Response**:
```json
{
  "profileId": "abc123",
  "subject": "user@idp",
  "email": "",
  "displayName": ""
}
```

## How It Works

### Architecture Flow

```
????????????????
? User Logs In ?
?  (OIDC/IDP)  ?
????????????????
       ? Authentication Token
       ?
??????????????????????
? Applicant Portal   ?
? Extracts OIDC Sub  ?
??????????????????????
          ? API Request (Subject + API Key)
          ?
??????????????????????????
? Unity Grant Manager    ?
? Queries Tenant Mapping ?
??????????????????????????
          ? Returns Available Tenants
          ?
??????????????????????
? Applicant Portal   ?
? Display Programs   ?
??????????????????????
```

### Data Synchronization

The Unity Grant Manager maintains a centralized lookup table (`AppApplicantTenantMaps`) that maps OIDC subjects to tenants:

**Real-time Updates**:
- When an applicant submits an application, Unity automatically creates/updates the mapping
- Mappings are available within milliseconds

**Nightly Reconciliation**:
- A background job runs daily at 2 AM PST to ensure data consistency
- Catches any missed events or new tenants

**Result**: The API always returns up-to-date tenant associations without requiring expensive database scans.

## Authentication

All API requests require an API key in the `X-API-Key` header.

**Security Requirements**:
- Always use HTTPS in production
- Store API key in secure configuration (Azure Key Vault, environment variables)
- Never commit API keys to source control
- Implement key rotation strategy

## Error Handling

| Status Code | Meaning | Action |
|-------------|---------|--------|
| 200 OK | Success | Process response |
| 400 Bad Request | Invalid parameters | Check query parameters |
| 401 Unauthorized | Invalid API key | Verify API key configuration |
| 500 Internal Server Error | Server error | Retry with exponential backoff |

**Empty Result**:
When a user has no tenant associations, the API returns an empty array `[]` with status 200 OK.

## Subject Identifier Format

The Unity system normalizes OIDC subject identifiers by:
1. Extracting the identifier before the `@` symbol
2. Converting to uppercase

**Examples**:
- `smzfrrla7j5hw6z7wzvyzdrtq6dj6fbr@chefs-frontend-5299` ? `SMZFRRLA7J5HW6Z7WZVYZDRTQ6DJ6FBR`
- `anonymous@bcservicescard` ? `ANONYMOUS`

**Implementation Note**: Pass the full OIDC subject from your authentication token. Unity handles the normalization internally.

## RabbitMQ Messaging

### Overview

In addition to REST APIs, the Applicant Portal and Unity communicate asynchronously via RabbitMQ for:
- **Commands**: Portal requests Unity to perform actions
- **Events**: Unity notifies Portal of state changes
- **Workflows**: Multi-step processes spanning both systems

### Inbox/Outbox Pattern

The Applicant Portal implements **Transactional Inbox/Outbox** for reliable messaging:

**Outbox** (Sending Messages):
```
Business Logic ? Save to Outbox Table ? Background Worker ? Publish to RabbitMQ
```

**Inbox** (Receiving Messages):
```
RabbitMQ ? Save to Inbox Table ? Acknowledge ? Background Worker ? Process Message
```

**Benefits**:
- ? No message loss (persisted to database)
- ? Transactional consistency
- ? At-least-once delivery guaranteed
- ? Full audit trail

### Message Types (Future)

#### Commands (Portal ? Unity)
- `UpdateApplicantProfileCommand` - User updates profile
- `SubmitApplicationDraftCommand` - User saves application draft
- `UploadDocumentCommand` - User uploads supporting document

#### Events (Unity ? Portal)
- `ApplicationStatusChangedEvent` - Application status updated
- `PaymentApprovedEvent` - Payment processed
- `DocumentRequestedEvent` - Additional documents needed
- `ReviewerAssignedEvent` - Application assigned for review

### Message Format

All messages follow a standard envelope:

```json
{
  "messageId": "uuid",
  "correlationId": "uuid",
  "messageType": "ApplicationStatusChangedEvent",
  "timestamp": "2026-01-15T22:42:24Z",
  "source": "unity",
  "payload": {
    "applicationId": "guid",
    "currentStatus": "Approved",
    "tenantName": "Housing Grant Program"
  }
}
```

### Configuration

**RabbitMQ Settings**:
```json
{
  "RabbitMQ": {
    "HostName": "rabbitmq.example.com",
    "UserName": "applicant-portal",
    "Password": "***",
    "Queues": {
      "Commands": "applicant-portal.commands",
      "Events": "unity.events.application"
    }
  }
}
```

**Background Workers**:
- **OutboxPublisher**: Polls outbox every 5 seconds, publishes pending messages
- **InboxProcessor**: Polls inbox every 5 seconds, processes received messages

### Key Concepts

**Idempotency**: All handlers must handle duplicate messages safely (messages may be delivered more than once)

**Retry Logic**: Failed messages retry with exponential backoff (5s, 25s, 125s) before moving to Dead Letter Queue

**Dead Letter Queue (DLQ)**: Messages that fail after retries require manual inspection and reprocessing

### Testing

**Local Development**:
```bash
# Start RabbitMQ with Docker
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Access Management UI
http://localhost:15672 (admin/admin)
```

**Integration Tests**:
- Verify commands reach Unity
- Verify events reach Portal
- Test duplicate message handling
- Test retry and DLQ behavior

> **?? Detailed Messaging Documentation**: See [RabbitMQ Messaging Integration](https://github.com/bcgov/Unity/blob/main/applications/Unity.GrantManager/docs/ApplicantPortalIntegration.md#rabbitmq-messaging-integration) section for:
> - Complete message schemas
> - Queue structure and naming
> - Error handling strategies
> - Monitoring and observability
> - Security considerations

## Performance



- **Response Time**: < 100ms typical (< 50ms p50)
- **Rate Limits**: Contact Unity team for rate limit details
- **Availability**: 99.9% uptime SLA

## Example Implementation

### C# / .NET
```csharp
public class UnityClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    
    public UnityClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["UnityIntegration:ApiKey"];
        _httpClient.BaseAddress = new Uri(config["UnityIntegration:BaseUrl"]);
    }
    
    public async Task<List<TenantDto>> GetApplicantTenantsAsync(string subject)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get, 
            $"/api/app/applicant-profiles/tenants?ProfileId={userId}&Subject={subject}");
        
        request.Headers.Add("X-API-Key", _apiKey);
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<List<TenantDto>>();
    }
}
```

### JavaScript / TypeScript
```typescript
class UnityClient {
  constructor(
    private baseUrl: string,
    private apiKey: string
  ) {}
  
  async getApplicantTenants(profileId: string, subject: string): Promise<Tenant[]> {
    const url = `${this.baseUrl}/api/app/applicant-profiles/tenants?ProfileId=${profileId}&Subject=${subject}`;
    
    const response = await fetch(url, {
      headers: {
        'X-API-Key': this.apiKey
      }
    });
    
    if (!response.ok) {
      throw new Error(`Unity API error: ${response.status}`);
    }
    
    return await response.json();
  }
}
```

## Testing

### Development Environment
- **Base URL**: `https://unity-dev.apps.silver.devops.gov.bc.ca`
- **API Key**: Contact Unity team for dev API key

### Test Scenarios
1. User with submissions in multiple tenants
2. User with no submissions (empty array)
3. Invalid API key (401 error)
4. Missing subject parameter (400 error)

## Support & Contacts

**Unity Grant Manager Team**:
- Repository: https://github.com/bcgov/Unity
- Technical Documentation: [Unity Integration Guide](https://github.com/bcgov/Unity/blob/main/applications/Unity.GrantManager/docs/ApplicantPortalIntegration.md)

**For Questions**:
- Slack: #unity-grant-manager (internal)
- Email: unity-support@gov.bc.ca

## Deployment Checklist

- [ ] API key obtained for each environment (dev/test/prod)
- [ ] API key stored securely (Key Vault, environment variables)
- [ ] Base URLs configured per environment
- [ ] Error handling implemented
- [ ] Logging configured (exclude API keys from logs)
- [ ] Rate limiting handled
- [ ] Integration tested in dev environment

## Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2026-01-XX | 1.0.0 | Initial integration documentation |

## Related Documentation

- [Unity Integration Guide (Detailed)](https://github.com/bcgov/Unity/blob/main/applications/Unity.GrantManager/docs/ApplicantPortalIntegration.md) - Complete API specifications
- [OIDC Authentication Setup](./authentication.md) - Your portal's auth configuration
- [Environment Configuration](./configuration.md) - Environment-specific settings
