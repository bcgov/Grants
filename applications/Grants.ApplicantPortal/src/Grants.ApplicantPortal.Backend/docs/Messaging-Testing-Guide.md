# Testing the Messaging System - End-to-End Examples

## ?? **Quick Test Scenarios**

### **1. Plugin Sending Message (Outbound)**

```csharp
// In your DemoProfilePlugin
public async Task TestOutboundMessage()
{
    var message = new ProfileUpdatedMessage(
        profileId: Guid.NewGuid(),
        pluginId: "DEMO", 
        provider: "PROGRAM1",
        key: "ORGINFO",
        correlationId: "test-correlation-123");

    await _messagePublisher.PublishAsync(message);
    // Message will be stored in OutboxMessages table
    // Background job will process and send to RabbitMQ
}
```

### **2. External System Sending Acknowledgment (Inbound)**

Simulate external system sending acknowledgment via RabbitMQ:

```json
{
  "messageId": "ack-123e4567-e89b-12d3-a456-426614174000",
  "messageType": "MessageAcknowledgment", 
  "correlationId": "test-correlation-123",
  "pluginId": "DEMO",
  "createdAt": "2024-01-15T10:30:00Z",
  "originalMessageId": "123e4567-e89b-12d3-a456-426614174000",
  "status": "SUCCESS",
  "details": "Profile successfully updated in external system",
  "processedAt": "2024-01-15T10:30:05Z"
}
```

### **3. External System Sending Plugin Data (Inbound)**

```json
{
  "messageId": "data-456e7890-f12a-34bc-d567-890123456def",
  "messageType": "PluginDataMessage",
  "correlationId": "sync-operation-456", 
  "pluginId": "DEMO",
  "createdAt": "2024-01-15T11:00:00Z",
  "dataType": "CONTACT_SYNC",
  "data": {
    "contacts": [
      {
        "id": "ext-contact-1",
        "name": "John Doe",
        "email": "john@example.com",
        "type": "PRIMARY"
      }
    ]
  }
}
```

## ?? **Monitoring Database Activity**

### **Check Outbound Messages**
```sql
-- See all outbound messages
SELECT 
    PluginId,
    MessageType, 
    Status,
    CreatedAt,
    ProcessedAt,
    RetryCount,
    CorrelationId
FROM OutboxMessages 
ORDER BY CreatedAt DESC;

-- Check failed messages
SELECT * FROM OutboxMessages 
WHERE Status = 2 -- Failed
ORDER BY CreatedAt DESC;
```

### **Check Inbound Messages**
```sql
-- See all inbound messages
SELECT 
    MessageType,
    Status, 
    ReceivedAt,
    ProcessedAt,
    RetryCount,
    CorrelationId
FROM InboxMessages
ORDER BY ReceivedAt DESC;

-- Check acknowledgments received
SELECT * FROM InboxMessages
WHERE MessageType = 'MessageAcknowledgment'
ORDER BY ReceivedAt DESC;
```

## ?? **Manual Testing Steps**

### **Step 1: Test Outbound Flow**

1. **Trigger Plugin Action**:
   - Use your application's UI to populate profile data
   - Or call the API endpoint directly

2. **Check Database**:
   ```sql
   SELECT * FROM OutboxMessages WHERE PluginId = 'DEMO';
   ```

3. **Watch Logs**:
   - OutboxProcessorJob should pick up the message
   - RabbitMQ publisher should send it (or simulate if no RabbitMQ)

### **Step 2: Test Inbound Flow (With RabbitMQ)**

1. **Send Test Message to RabbitMQ**:
   ```bash
   # Using RabbitMQ management UI or CLI
   # Send to grants.messaging exchange with routing key grants.demo.acknowledgment
   ```

2. **Check Database**:
   ```sql
   SELECT * FROM InboxMessages WHERE MessageType = 'MessageAcknowledgment';
   ```

3. **Watch Logs**:
   - RabbitMQConsumer should receive message
   - InboxProcessorJob should process it
   - DemoPluginMessageHandler should handle it

### **Step 3: Test Inbound Flow (Without RabbitMQ)**

If no RabbitMQ available, manually insert test message:

```sql
-- Insert test acknowledgment
INSERT INTO InboxMessages (
    MessageId,
    MessageType,
    Payload,
    ReceivedAt,
    Status,
    RetryCount,
    CorrelationId
) VALUES (
    'ack-test-123e4567-e89b-12d3-a456-426614174000',
    'MessageAcknowledgment',
    '{"messageId":"ack-test-123e4567-e89b-12d3-a456-426614174000","messageType":"MessageAcknowledgment","correlationId":"test-correlation-123","pluginId":"DEMO","createdAt":"2024-01-15T10:30:00Z","originalMessageId":"123e4567-e89b-12d3-a456-426614174000","status":"SUCCESS","details":"Test acknowledgment","processedAt":"2024-01-15T10:30:05Z"}',
    NOW(),
    0, -- Pending
    0,
    'test-correlation-123'
);
```

## ?? **Expected Log Output**

### **Outbound Processing**
```
[INF] Demo plugin successfully populated profile for ProfileId: 123e4567-e89b-12d3-a456-426614174000
[DBG] Published ProfileUpdatedMessage for 123e4567-e89b-12d3-a456-426614174000
[DBG] Outbox processor job starting
[DBG] Processing outbox message 123e4567... of type ProfileUpdatedMessage
[DBG] Published message 123e4567... to RabbitMQ with routing key grants.demo.profileupdated
```

### **Inbound Processing**
```
[DBG] Received message ack-123e4567... of type MessageAcknowledgment with routing key grants.demo.acknowledgment
[DBG] Successfully stored message ack-123e4567... in inbox
[DBG] Inbox processor job starting  
[DBG] Processing inbox message ack-123e4567... of type MessageAcknowledgment
[DBG] Routing acknowledgment ack-123e4567... to plugin handler DEMO
[INF] Demo plugin received acknowledgment for message 123e4567... with status SUCCESS
```

## ?? **Configuration for Testing**

### **Development Settings (appsettings.Development.json)**
```json
{
  "Messaging": {
    "RabbitMQ": {
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest"
    },
    "Outbox": {
      "PollingIntervalSeconds": 10,  // Faster for testing
      "BatchSize": 10
    },
    "Inbox": {
      "PollingIntervalSeconds": 5,   // Faster for testing  
      "BatchSize": 10
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"  // See detailed logs
    }
  }
}
```

This gives you a complete testing framework to verify the entire messaging flow! ??