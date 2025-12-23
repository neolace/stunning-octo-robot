## Repository: PersonalAIAgent (stunning-octo-robot)

This repo is a small ASP.NET Core Web API that exposes a tiny "Personal AI Agent" surface backed by Microsoft Graph.

Key files
- `PersonalAIAgent/Program.cs` — standard .NET Program bootstrapping using `Startup`.
- `PersonalAIAgent/Startup.cs` — registers services and controllers. Important: `GraphService` is registered as a singleton here.
- `PersonalAIAgent/Controllers/AgentController.cs` — the public HTTP surface (routes: `POST /api/agent/send-email`, `POST /api/agent/upload-file`).
- `PersonalAIAgent/GraphService.cs` — encapsulates Microsoft Graph access using client-credentials (MSAL ConfidentialClient). This is the primary integration point to external services.
- `PersonalAIAgent/appsettings.json` — contains the `AzureAd` configuration keys (ClientId, TenantId, ClientSecret) used by `GraphService`.

Big-picture architecture
- Single ASP.NET Core Web API project (targeting .NET 9). The app exposes a minimal controller that delegates to a single `GraphService` for all Microsoft Graph operations.
- `GraphService` is injected as a singleton and constructed with `IConfiguration`. It uses a client-credentials flow (AcquireTokenForClient) and a `GraphServiceClient` with a delegate auth provider.
- Endpoints are thin wrappers: controller accepts HTTP requests and calls `GraphService.SendEmailAsync` and `GraphService.UploadFileAsync` (uploads to OneDrive/Drive via `Me.Drive`).

Developer workflows (how to build/run/debug)
- Local build (command line):

  dotnet restore
  dotnet build PersonalAIAgent/PersonalAIAgent.csproj

- Run the API (project folder):

  dotnet run --project PersonalAIAgent/PersonalAIAgent.csproj

- In Visual Studio: open `stunning-octo-robot.sln` and run the `PersonalAIAgent` project. `Properties/launchSettings.json` provides debug launch configuration.

Configuration & credentials
- `PersonalAIAgent/appsettings.json` contains placeholders for `AzureAd:ClientId`, `AzureAd:TenantId`, `AzureAd:ClientSecret`. Replace these with real values or set equivalent environment variables (ASP.NET Core will bind env vars using `AzureAd__ClientId` etc.).
- Because `GraphService` uses a client credential flow, ensure the Azure AD app has appropriate application permissions and tenant admin consent for the Graph APIs you call (Mail.Send, Files.ReadWrite or whichever your tenant requires).

API examples
- Send email (note: controller binds simple params from query/form):

  curl -X POST "http://localhost:5000/api/agent/send-email?to=someone@example.com&subject=hi&body=hello"

- Upload file (multipart/form-data):

  curl -F "file=@/path/to/file.txt" http://localhost:5000/api/agent/upload-file

Project-specific patterns and caveats
- Single shared `GraphService` handles auth and Graph client creation — keep logic here for token caching and request customization.
- The code uses `GraphServiceClient` calls against `Me` (e.g., `_client.Me.SendMail` and `_client.Me.Drive`). Because `GraphService` obtains tokens using client-credentials (app-only), confirm the chosen Graph endpoints are allowed with app-only permissions in your tenant. If you need delegated (user) calls, change the authentication model accordingly.
- Controller action signatures use simple types (string) without `[FromBody]`, so calls often bind from query string or form data. Use query parameters or form posts from clients unless you update the binding attributes.

Dependencies
- See `PersonalAIAgent/PersonalAIAgent.csproj` for critical packages: `Microsoft.Graph`, `Microsoft.Identity.Client`, .NET 9 SDK.

If you change auth or Graph calls
- Update `GraphService` only — it centralizes the Graph flow. Add tests around token acquisition and client usage if you expand behavior.

What an AI coding agent should do first
1. Read `PersonalAIAgent/GraphService.cs` and `PersonalAIAgent/Controllers/AgentController.cs` to understand the minimal surface and Graph usage.
2. Confirm runtime configuration (set `AzureAd` keys in `appsettings.json` or via environment variables) before attempting any live Graph calls.
3. For changes that affect permissions, include a note in the PR describing required Azure AD permissions and whether tenant admin consent is needed.

Questions / feedback
- If any of the above assumptions are wrong (for example you prefer delegated authentication rather than app-only), tell me which auth mode you want and I'll adjust instructions and code examples.

-- End of instructions
