using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace PersonalAIAgent
{
    public class GraphService
    {
        private readonly GraphServiceClient _client;
        private readonly string _senderEmail;

        public GraphService(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var clientId = configuration["AzureAd:ClientId"];
            var tenantId = configuration["AzureAd:TenantId"];
            var clientSecret = configuration["AzureAd:ClientSecret"];
            _senderEmail = configuration["AzureAd:SenderEmail"] ?? configuration["Graph:SenderEmail"];

            if (string.IsNullOrWhiteSpace(clientId))
                throw new InvalidOperationException("AzureAd:ClientId is not configured.");
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new InvalidOperationException("AzureAd:TenantId is not configured.");
            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new InvalidOperationException("AzureAd:ClientSecret is not configured.");
            if (string.IsNullOrWhiteSpace(_senderEmail))
                throw new InvalidOperationException("Sender email is required for app-only (client credentials) flow. Set AzureAd:SenderEmail to the mailbox to use for sending/uploading.");

            var app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithTenantId(tenantId)
                .WithClientSecret(clientSecret)
                .Build();

            var authProvider = new DelegateAuthenticationProvider(async (requestMessage) =>
            {
                var scopes = new[] { "https://graph.microsoft.com/.default" };
                var result = await app.AcquireTokenForClient(scopes).ExecuteAsync().ConfigureAwait(false);
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            });

            _client = new GraphServiceClient(authProvider);
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(to)) throw new ArgumentNullException(nameof(to));
            if (subject == null) subject = string.Empty;
            if (body == null) body = string.Empty;

            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = body
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress { Address = to }
                    }
                }
            };

            // Use the specified user mailbox when operating in app-only mode.
            await _client.Users[_senderEmail]
                .SendMail(message, saveToSentItems: true)
                .Request()
                .PostAsync()
                .ConfigureAwait(false);
        }

        public async Task<DriveItem> UploadFileAsync(Stream fileStream, string fileName)
        {
            if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));

            // Upload to the specified user's OneDrive root using app-only permissions.
            var uploaded = await _client.Users[_senderEmail]
                .Drive
                .Root
                .ItemWithPath(fileName)
                .Content
                .Request()
                .PutAsync<DriveItem>(fileStream)
                .ConfigureAwait(false);

            return uploaded;
        }
    }
}
