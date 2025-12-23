using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;

namespace PersonalAIAgent
{
    public class GraphService
    {
        private readonly GraphServiceClient _client;

        public GraphService(IConfiguration config)
        {
            var clientId = config["AzureAd:ClientId"];
            var tenantId = config["AzureAd:TenantId"];
            var clientSecret = config["AzureAd:ClientSecret"];

            var confidentialClient = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithTenantId(tenantId)
                .WithClientSecret(clientSecret)
                .Build();

            var authProvider = new DelegateAuthenticationProvider(async request =>
            {
                var result = await confidentialClient
                    .AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
                    .ExecuteAsync();
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", result.AccessToken);
            });

            _client = new GraphServiceClient(authProvider);
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody { ContentType = BodyType.Html, Content = body },
                ToRecipients = new[] { new Recipient { EmailAddress = new EmailAddress { Address = to } } }
            };
            await _client.Me.SendMail(message, true).Request().PostAsync();
        }

        public async Task UploadFileAsync(string path, Stream content)
        {
            await _client.Me.Drive.Root.ItemWithPath(path).Content.Request().PutAsync<DriveItem>(content);
        }
    }
}
