using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserIdentityApi.Services.SendGrid;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace UserIdentityApi.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger _logger;
        private readonly SendGridOptions _options;

        public EmailSender(
            ILogger<EmailSender> logger,
            IOptions<SendGridOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async Task SendEmailAsync(string emailAddress, string subject, string content)
        {
            try
            {
                _logger.LogInformation($"Attempting to send email to {emailAddress} with subject: {subject}");
                
                if (string.IsNullOrEmpty(_options.ApiKey))
                {
                    _logger.LogError("SendGrid API key is not configured");
                    throw new InvalidOperationException("SendGrid API key is not configured");
                }

                var client = new SendGridClient(_options.ApiKey);
                var message = new SendGridMessage();
                message.SetFrom("ozmen_celik@hotmail.com", "Ozzy Ozmen Celik");
                message.SetSubject(subject);
                message.AddTo(new EmailAddress(emailAddress));
                message.HtmlContent = content;
                message.PlainTextContent = content.Replace("<strong>", "").Replace("</strong>", "")
                                               .Replace("<h2>", "").Replace("</h2>", "\n")
                                               .Replace("<p>", "").Replace("</p>", "\n");
                message.SetClickTracking(false, false);

                _logger.LogInformation("Sending email via SendGrid...");
                var response = await client.SendEmailAsync(message);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email to {emailAddress} queued successfully!");
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"Failed to send email to {emailAddress}. Status Code: {response.StatusCode}, Body: {responseBody}");
                    throw new Exception($"Failed to send email. Status Code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {emailAddress}: {ex.Message}");
                throw;
            }
        }
    }
}