# Azure Interview Preparation Guide

Focus: Azure Functions, App Services, Cosmos DB (NoSQL)

---

## Azure Functions

### Key Concepts to Know

**1. What are Azure Functions?**
- Serverless compute service (FaaS - Function as a Service)
- Event-driven execution
- Pay only for execution time (consumption plan)
- Auto-scales based on demand

**2. Hosting Plans**

| Plan | Use Case | Cost | Scale |
|------|----------|------|-------|
| **Consumption** | Sporadic workloads | Pay per execution | Auto (0 to many) |
| **Premium** | No cold start, VNET | Higher fixed cost | Auto with pre-warmed |
| **Dedicated** | Existing App Service | App Service pricing | Manual/Auto |

**3. Triggers (What starts the function)**
- HTTP Trigger - REST API endpoints
- Queue Trigger - Azure Storage Queue messages
- Blob Trigger - File uploaded to storage
- Timer Trigger - Cron schedule (e.g., `0 */5 * * * *`)
- Cosmos DB Trigger - Document changes
- Event Hub/Service Bus Trigger - Event streaming

**4. Bindings (Input/Output)**
```csharp
// Input binding - reads from Cosmos
[CosmosDBInput("db", "collection", Id = "{id}")]

// Output binding - writes to Queue
[QueueOutput("myqueue")]
```

### Common Interview Questions

**Q: What's the difference between Consumption and Premium plans?**

A:
- **Consumption**: Cold starts (function sleeps after idle), scales to zero, pay per execution, 5-min max timeout (can extend to 10)
- **Premium**: Pre-warmed instances (no cold start), VNET integration, unlimited timeout, higher cost

**Q: How do you handle long-running processes in Functions?**

A:
- Use **Durable Functions** for orchestration
- Break into smaller functions chained together
- Use Queue triggers for async processing
- Consider Premium/Dedicated plan for longer timeouts

**Q: What are Durable Functions?**

A: Extension for writing stateful workflows:
- **Orchestrator**: Coordinates the workflow
- **Activity**: Does the actual work
- **Entity**: Manages state
- Patterns: Chaining, Fan-out/Fan-in, Async HTTP APIs, Monitoring

**Q: How do you secure Azure Functions?**

A:
- **Function-level**: Function keys, Admin keys
- **App-level**: Azure AD authentication
- **Network**: VNET integration (Premium), IP restrictions
- **API Management**: Put APIM in front

**Q: What happens when a Queue trigger fails?**

A:
- Automatic retry (default 5 times)
- Exponential backoff between retries
- After max retries → message goes to **poison queue** (`<queuename>-poison`)
- You can configure `maxDequeueCount` in host.json

### Real-World Example (From This Project)

```csharp
[Function(nameof(ProcessDocument))]
public async Task ProcessDocument(
    [QueueTrigger("document-processing", Connection = "AzureWebJobsStorage")]
    string messageJson)
{
    var message = JsonSerializer.Deserialize<ProcessDocumentMessage>(messageJson);

    // Read from Cosmos DB
    var document = await _container.ReadItemAsync<Document>(
        message.DocumentId,
        new PartitionKey(message.DocumentId));

    // Process and update
    document.Resource.Status = DocumentStatus.Completed;
    await _container.UpsertItemAsync(document.Resource);
}
```

---

## Azure App Service

### Key Concepts to Know

**1. What is App Service?**
- PaaS for hosting web apps, APIs, mobile backends
- Managed infrastructure (no VM management)
- Built-in CI/CD, scaling, SSL, custom domains

**2. Pricing Tiers**

| Tier | Features | Use Case |
|------|----------|----------|
| **Free (F1)** | 60 CPU min/day, shared | Dev/Test |
| **Basic (B1-B3)** | Custom domain, manual scale | Small production |
| **Standard (S1-S3)** | Auto-scale, slots, backups | Production |
| **Premium (P1-P3)** | More instances, better perf | High traffic |

**3. Deployment Options**
- Git push (Local Git, GitHub, Azure DevOps)
- ZIP deploy
- Docker containers
- FTP (not recommended)

**4. Key Features**
- **Deployment Slots**: Staging environments, swap with production
- **Auto-scaling**: Scale out based on metrics (CPU, memory, requests)
- **SSL/TLS**: Free managed certificates
- **VNET Integration**: Connect to private resources

### Common Interview Questions

**Q: What's the difference between scaling up vs scaling out?**

A:
- **Scale Up (Vertical)**: Bigger machine (more CPU/RAM) - change pricing tier
- **Scale Out (Horizontal)**: More instances - add more machines running your app
- Scale out is preferred for high availability and true scalability

**Q: Explain deployment slots and their use cases**

A:
- Separate environments within same App Service
- Use cases:
  - **Staging slot**: Test before production
  - **Blue/Green deployment**: Swap with zero downtime
  - **A/B testing**: Route % of traffic to different slots
- Swap is instant (just swaps virtual IPs)

**Q: How do you handle configuration differences between environments?**

A:
- **App Settings**: Environment variables in Azure Portal
- **Slot Settings**: Can mark settings as "slot-specific" (don't swap)
- **Connection Strings**: Separate section, auto-injected
- Use `IConfiguration` in code - automatically reads from App Settings

**Q: What's "Always On" and why does it matter?**

A:
- Keeps app loaded even when no traffic
- Without it: App unloads after ~20 min idle → cold start on next request
- Only available in Basic tier and above
- Critical for: background jobs, SignalR connections, consistent response times

**Q: How do you troubleshoot a slow App Service?**

A:
1. **Metrics**: Check CPU, memory, response time in Portal
2. **Application Insights**: Trace requests, find bottlenecks
3. **Diagnose and Solve**: Built-in diagnostic tools
4. **Log Stream**: Real-time logs
5. **Kudu console**: Advanced debugging (yourapp.scm.azurewebsites.net)

### Real-World Example (From This Project)

```csharp
// Program.cs - ASP.NET Core Minimal API on App Service
var builder = WebApplication.CreateBuilder(args);

// Configuration automatically reads from Azure App Settings
var cosmosConnection = builder.Configuration.GetConnectionString("CosmosDb");

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",
            "https://myapp.azurestaticapps.net")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
```

---

## Cosmos DB (NoSQL)

### Key Concepts to Know

**1. What is Cosmos DB?**
- Globally distributed, multi-model database
- Single-digit millisecond latency at 99th percentile
- Automatic and instant scalability
- Multiple APIs: NoSQL (native), MongoDB, Cassandra, Gremlin, Table

**2. Core Concepts**

```
Account
  └── Database
        └── Container (≈ Table/Collection)
              └── Items (≈ Documents/Rows)
```

**3. Request Units (RUs)**
- Currency for throughput
- 1 RU = 1 read of 1KB document by ID
- Writes cost more (~5-7 RUs for 1KB)
- Complex queries cost more
- Provisioned vs Serverless

**4. Partition Key (CRITICAL)**
- Determines data distribution
- Choose wisely - cannot change later!
- Good: High cardinality, even distribution, frequently queried
- Bad: Low cardinality (e.g., true/false), hot partitions

### Common Interview Questions

**Q: How do you choose a partition key?**

A: Consider:
1. **Cardinality**: Many unique values (userId, deviceId)
2. **Distribution**: Even spread of data and requests
3. **Query patterns**: Include in WHERE clause to avoid cross-partition queries
4. **Document size**: Keep logical partitions under 20GB

Example: For user documents, use `/userId` not `/country` (few countries = hot partitions)

**Q: What's the difference between Provisioned and Serverless?**

| Aspect | Provisioned | Serverless |
|--------|-------------|------------|
| Cost | Pay for reserved RU/s | Pay per request |
| Scale | Manual or auto-scale | Automatic |
| Best for | Steady, predictable traffic | Sporadic, dev/test |
| Max RUs | 1M+ RU/s | 5000 RU/s |

**Q: How do you optimize Cosmos DB costs?**

A:
1. **Right-size RUs**: Start low, use autoscale
2. **Use partition key in queries**: Avoid cross-partition queries
3. **Project only needed fields**: `SELECT c.id, c.name` not `SELECT *`
4. **Use point reads**: `ReadItemAsync(id, partitionKey)` = 1 RU
5. **Enable TTL**: Auto-delete old documents
6. **Consider Serverless**: For dev/test or sporadic workloads

**Q: Explain consistency levels**

| Level | Guarantees | Latency | Use Case |
|-------|------------|---------|----------|
| **Strong** | Linearizable reads | Highest | Financial data |
| **Bounded Staleness** | Consistent within K versions/T time | High | Inventory |
| **Session** | Read your own writes | Medium | Most apps (default) |
| **Consistent Prefix** | Ordered, but may lag | Low | Social media |
| **Eventual** | No ordering guarantee | Lowest | View counters |

**Q: How do you handle transactions in Cosmos DB?**

A:
- **Transactional Batch**: Multiple operations on same partition key
- All succeed or all fail (ACID within partition)
- Cannot span partitions - design accordingly

```csharp
var batch = container.CreateTransactionalBatch(new PartitionKey(userId))
    .CreateItem(order)
    .CreateItem(orderItem)
    .ReplaceItem(id, updatedUser);

var response = await batch.ExecuteAsync();
```

**Q: How do you query Cosmos DB efficiently?**

A:
```sql
-- Good: Uses partition key, indexed fields
SELECT * FROM c
WHERE c.userId = 'user123'
  AND c.status = 'active'

-- Bad: Cross-partition query
SELECT * FROM c WHERE c.email = 'user@example.com'

-- Bad: Functions on fields (can't use index)
SELECT * FROM c WHERE LOWER(c.name) = 'john'
```

### Real-World Example (From This Project)

```csharp
// Service for Cosmos DB operations
public class CosmosDbService : ICosmosDbService
{
    private readonly Container _container;

    public async Task<Document?> GetDocumentAsync(string id)
    {
        try
        {
            // Point read - most efficient (1 RU)
            var response = await _container.ReadItemAsync<Document>(
                id,
                new PartitionKey(id));  // Partition key = id
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<Document>> GetDocumentsAsync(int page, int pageSize)
    {
        // Query with pagination
        var query = _container.GetItemQueryIterator<Document>(
            new QueryDefinition("SELECT * FROM c ORDER BY c.createdAt DESC")
                .WithParameter("@skip", (page - 1) * pageSize)
                .WithParameter("@take", pageSize));

        var results = new List<Document>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }
}
```

---

## Architecture Questions

**Q: Design a document processing system on Azure**

A: (This is exactly what we built!)

```
                                    ┌─────────────────┐
                                    │  Static Web App │
                                    │   (Frontend)    │
                                    └────────┬────────┘
                                             │
                                             ▼
┌─────────────────────────────────────────────────────────────┐
│                        App Service                          │
│                      (REST API)                             │
│  - File validation                                          │
│  - Upload to Blob Storage                                   │
│  - Create Cosmos DB record (status: Pending)                │
│  - Send queue message                                       │
└──────────────┬─────────────────┬─────────────────┬─────────┘
               │                 │                 │
               ▼                 ▼                 ▼
        ┌───────────┐     ┌───────────┐     ┌───────────┐
        │   Blob    │     │   Queue   │     │ Cosmos DB │
        │  Storage  │     │  Storage  │     │           │
        └───────────┘     └─────┬─────┘     └───────────┘
                                │                 ▲
                                ▼                 │
                         ┌───────────┐            │
                         │  Azure    │────────────┘
                         │ Functions │  Update status
                         └───────────┘  to Completed
```

**Q: How do you handle failures in this architecture?**

A:
1. **Queue retry**: Auto-retry with exponential backoff
2. **Poison queue**: Failed messages go to separate queue for investigation
3. **Idempotency**: Functions can process same message multiple times safely
4. **Status tracking**: Cosmos DB shows Pending/Processing/Completed/Failed
5. **Monitoring**: Application Insights for end-to-end tracing

**Q: How would you scale this system?**

A:
- **API**: App Service auto-scale based on CPU/requests
- **Functions**: Consumption plan auto-scales automatically
- **Cosmos DB**: Auto-scale RUs or manual provisioning
- **Storage**: Virtually unlimited, auto-scales
- **Frontend**: Static Web App with global CDN

---

## Behavioral/Scenario Questions

**Q: Tell me about a time you debugged a cloud issue**

Example from this project:
> "We deployed an update and documents stopped being processed - they stayed in 'Pending' status. I systematically checked: API was working, files were in Blob storage, but the Azure Function wasn't processing queue messages. I checked the poison queue and found all messages there. The issue was a message encoding mismatch - we changed how the API encoded messages but the Function expected a different format. I reverted the encoding change and added documentation to prevent this in the future."

**Q: How do you ensure you don't exceed cloud costs?**

A:
1. Set budget alerts at $1, $5, $10
2. Use free tiers for dev/test
3. Review Cost Analysis weekly
4. Delete test resources after use
5. Use Serverless/Consumption where possible
6. Tag resources by project for cost tracking

---

## Quick Facts to Memorize

### Azure Functions
- Consumption timeout: 5 min (default), 10 min (max)
- Premium timeout: 30 min (default), unlimited
- Cold start: ~1-3 seconds (Consumption), eliminated in Premium
- Max instances: 200 (Consumption), configurable (Premium)

### App Service
- F1 Free: 60 CPU minutes/day
- Deployment slots: Standard tier and above
- Auto-scale: Standard tier and above
- Always On: Basic tier and above

### Cosmos DB
- Free tier: 1000 RU/s, 25GB, 1 per subscription
- Point read (by ID + partition key): 1 RU
- Write 1KB document: ~5-7 RUs
- Max item size: 2MB
- Max partition size: 20GB logical, unlimited physical
- Consistency levels: 5 (Strong to Eventual)

---

## Resources

- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Cosmos DB Documentation](https://docs.microsoft.com/azure/cosmos-db/)
- [Azure Architecture Center](https://docs.microsoft.com/azure/architecture/)
- [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/)
