using Microsoft.Extensions.Options;
using PhotosStorageMap.Application.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace PhotosStorageMap.Infrastructure.Email
{
    public sealed class SendGridEmailService : IEmailService
    {
        private readonly SendGridOptions _options;
        private readonly ILogger<SendGridEmailService> _logger;

        public SendGridEmailService(
            IOptions<SendGridOptions> options,
            ILogger<SendGridEmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }


        public async Task SendAsync(
            string toEmail, 
            string subject, 
            string htmlBody)
        {
            var client = new SendGridClient(_options.ApiKey);

            var from = new EmailAddress(
                _options.FromEmail,
                _options.FromName);

            var to = new EmailAddress(toEmail);

            var message = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                plainTextContent: null,
                htmlContent: htmlBody);

            var response = await client.SendEmailAsync(message);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Body.ReadAsStringAsync();

                _logger.LogError(
                    "SendGrid email failed. Status: {StatusCode}. To: {ToEmail}. Response: {ResponseBody}",
                    response.StatusCode,
                    toEmail,
                    responseBody);

                throw new InvalidOperationException($"Email could not be sent. SendGrid returned {response.StatusCode}");
            }

            _logger.LogInformation(
                "Email sent successfully to {ToEmail}. Subject: {Subject}",
                toEmail,
                subject);
        }
    }
}
