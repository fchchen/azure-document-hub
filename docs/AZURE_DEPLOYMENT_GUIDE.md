# Azure Deployment Guide & Lessons Learned

This document captures lessons learned from deploying this application to Azure, common pitfalls, and tips for staying within free tier limits.

## Table of Contents
1. [Lessons Learned](#lessons-learned)
2. [Deployment Checklist](#deployment-checklist)
3. [Free Tier Tips](#free-tier-tips)
4. [Common Issues & Solutions](#common-issues--solutions)
5. [Configuration Management](#configuration-management)

---

## Lessons Learned

### 1. Environment Configuration Mismatch

**Problem**: Frontend deployed to Azure was still calling `localhost:5255` instead of the Azure API URL.

**Root Cause**: Angular's `angular.json` was missing `fileReplacements` configuration for production builds.

**Solution**: Add file replacements to `angular.json`:
```json
"configurations": {
  "production": {
    "fileReplacements": [
      {
        "replace": "src/environments/environment.ts",
        "with": "src/environments/environment.prod.ts"
      }
    ]
  }
}
```

**Prevention**: Always verify the production build contains correct URLs:
```bash
grep -o 'apiUrl:"[^"]*"' ./dist/*/browser/main-*.js
```

### 2. Queue Message Encoding

**Problem**: Azure Functions wasn't processing queue messages - documents stayed in "Pending" status.

**Root Cause**: Mismatch between how API encodes messages and how Functions expects them.

**What Works**:
```csharp
// QueueService.cs - Manual base64 + SDK base64 (double encoding)
var json = JsonSerializer.Serialize(message);
var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
await queueClient.SendMessageAsync(base64);
```

**What Doesn't Work**:
- Removing manual base64 encoding
- Setting `MessageEncoding = QueueMessageEncoding.None`

**Lesson**: Don't "fix" working code based on assumptions. Test thoroughly before changing message encoding.

### 3. Cosmos DB Free Tier Must Be Enabled at Creation

**Problem**: Existing Cosmos DB account was "Opted Out" of free tier.

**Key Points**:
- Free tier can ONLY be enabled when creating the account
- Cannot convert existing account to free tier
- Only ONE free tier Cosmos DB per subscription

**Solution**: Delete and recreate with free tier enabled, or use Serverless mode (pay-per-request).

### 4. App Service Plan Pricing

**Problem**: App Service plan was on B1 (Basic) tier, costing ~$0.62/day.

**Solution**: Change to F1 (Free) tier:
1. Go to App Service Plan → Scale up
2. Select Dev/Test → F1

**Limitations of F1 Free**:
- 60 CPU minutes/day
- No custom domains
- No "Always On" (app sleeps after idle)
- Shared infrastructure

### 5. Azurite API Version Compatibility

**Problem**: Local Azurite rejected requests with "API version not supported" error.

**Solution**: Start Azurite with `--skipApiVersionCheck` flag:
```bash
azurite --skipApiVersionCheck
```

---

## Deployment Checklist

### Before Deploying API

- [ ] Update `appsettings.json` or use environment variables for:
  - Connection strings (Storage, Cosmos DB)
  - CORS origins (add Static Web App URL)
- [ ] Build in Release mode: `dotnet publish -c Release`
- [ ] Verify no secrets in committed files

### Before Deploying Functions

- [ ] Update `local.settings.json` is NOT deployed (it's gitignored)
- [ ] Configure App Settings in Azure Portal:
  - `AzureWebJobsStorage`
  - `CosmosDbConnection`
- [ ] Deploy: `func azure functionapp publish <name>`

### Before Deploying Frontend

- [ ] Verify `environment.prod.ts` has correct API URL
- [ ] Verify `angular.json` has `fileReplacements` for production
- [ ] Build: `npm run build -- --configuration=production`
- [ ] Verify build output: `grep 'apiUrl' ./dist/*/browser/main-*.js`
- [ ] Deploy: `npx @azure/static-web-apps-cli deploy ...`

### After Deployment

- [ ] Test API endpoint directly: `curl https://<api>.azurewebsites.net/health`
- [ ] Test frontend loads and calls correct API
- [ ] Upload a file and verify status changes to "Completed"
- [ ] Check Azure Portal for any errors

---

## Free Tier Tips

### What's Actually Free

| Service | Free Tier Limit | Notes |
|---------|-----------------|-------|
| **App Service** | 10 apps (F1) | 60 CPU min/day, shared |
| **Azure Functions** | 1M executions/month | Consumption plan |
| **Cosmos DB** | 1000 RU/s + 25GB | Must enable at creation |
| **Storage** | 5GB LRS (12 months) | Then pay-as-you-go |
| **Static Web Apps** | 100GB bandwidth | Free forever |

### Avoiding Unexpected Costs

1. **Set Budget Alerts**
   - Azure Portal → Cost Management → Budgets
   - Set alert at $1, $5, $10

2. **Check Pricing Tier Before Creating Resources**
   - Always select "Free" or "Consumption" where available
   - Avoid "Basic", "Standard", "Premium" tiers

3. **Use Resource Groups**
   - Group all project resources together
   - Easy to see total cost
   - Easy to delete everything when done

4. **Review Cost Analysis Weekly**
   - Azure Portal → Cost Management → Cost analysis
   - Filter by resource group

5. **Stop/Delete When Not Using**
   - App Service: Stop (still incurs some cost) or scale to F1
   - VMs: Deallocate when not in use
   - Delete test resources after experiments

### Cost-Saving Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    FREE TIER ARCHITECTURE                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   Static Web Apps (Free)     App Service F1 (Free)          │
│   ┌─────────────┐            ┌─────────────┐                │
│   │  Frontend   │───────────▶│    API      │                │
│   │  (Angular)  │            │  (ASP.NET)  │                │
│   └─────────────┘            └──────┬──────┘                │
│                                     │                        │
│                    ┌────────────────┼────────────────┐      │
│                    ▼                ▼                ▼      │
│            ┌───────────┐    ┌───────────┐    ┌───────────┐  │
│            │  Storage  │    │   Queue   │    │ Cosmos DB │  │
│            │   (5GB)   │    │  (Free)   │    │(Free Tier)│  │
│            └───────────┘    └─────┬─────┘    └───────────┘  │
│                                   │                          │
│                                   ▼                          │
│                          ┌───────────────┐                  │
│                          │   Functions   │                  │
│                          │ (Consumption) │                  │
│                          └───────────────┘                  │
│                                                              │
│   Monthly Cost: ~$0 (within limits)                         │
└─────────────────────────────────────────────────────────────┘
```

---

## Common Issues & Solutions

### Issue: API returns 500 error after deployment

**Check**:
1. Azure Portal → App Service → Log stream
2. Verify connection strings are configured
3. Check if Cosmos DB/Storage is accessible

**Solution**: Add connection strings in Configuration → Application settings

### Issue: Functions not processing queue messages

**Check**:
1. Azure Portal → Function App → Functions → Monitor
2. Check poison queue for failed messages
3. Verify `AzureWebJobsStorage` connection string

**Solution**:
- Restart Function App
- Check message encoding matches expectations
- Review Function logs for deserialization errors

### Issue: CORS errors in browser

**Check**: Browser dev tools → Network tab → Look for blocked requests

**Solution**: Add frontend URL to CORS settings:
```csharp
policy.WithOrigins(
    "http://localhost:4200",
    "https://your-app.azurestaticapps.net")
```

### Issue: "Always On" not available on Free tier

**Impact**: App goes to sleep after ~20 minutes of inactivity, causing cold start delays.

**Workarounds**:
- Accept the cold start delay (first request takes 10-30 seconds)
- Use Azure Functions timer trigger to ping the API (uses function executions)
- Upgrade to Basic tier when needed

### Issue: File uploads fail silently

**Check**:
1. File size limits (Kestrel default: 30MB)
2. Request timeout settings
3. Storage container permissions

**Solution**: Configure request limits in `Program.cs`:
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024; // 500KB
});
```

---

## Configuration Management

### Secrets Management

**Never commit**:
- `appsettings.Development.json` (with real connection strings)
- `local.settings.json` (Azure Functions)
- `.env` files

**Do commit**:
- `appsettings.Development.json.template` (with placeholder values)
- `local.settings.json.template`

**In Azure**: Use Application Settings (encrypted at rest)

### Environment-Specific Configuration

```
Local Development:
├── appsettings.Development.json  (gitignored, real secrets)
├── local.settings.json           (gitignored, real secrets)
└── environment.ts                (localhost URLs)

Azure Production:
├── Application Settings          (configured in Portal)
└── environment.prod.ts           (Azure URLs, committed)
```

### Connection String Pattern

**Local (Azurite)**:
```
UseDevelopmentStorage=true
```

**Azure Storage**:
```
DefaultEndpointsProtocol=https;AccountName=xxx;AccountKey=xxx;EndpointSuffix=core.windows.net
```

**Cosmos DB (Local Emulator)**:
```
AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==
```

---

## Quick Reference Commands

### Deploy API
```bash
cd src/Api
dotnet publish -c Release -o ./publish
cd publish && zip -r ../deploy.zip .
az webapp deployment source config-zip --resource-group <rg> --name <app> --src deploy.zip
```

### Deploy Functions
```bash
cd src/Functions
func azure functionapp publish <function-app-name>
```

### Deploy Frontend
```bash
cd frontend
npm run build -- --configuration=production
npx @azure/static-web-apps-cli deploy ./dist/frontend/browser \
  --deployment-token $(az staticwebapp secrets list --name <swa> --query "properties.apiKey" -o tsv) \
  --env production
```

### Check Costs
```bash
az consumption usage list --query "[].{name:name, cost:pretaxCost}" -o table
```

### Restart Services
```bash
az webapp restart --name <api-name> --resource-group <rg>
az functionapp restart --name <func-name> --resource-group <rg>
```
