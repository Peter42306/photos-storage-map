using PhotosStorageMap.Application.Interfaces;

namespace PhotosStorageMap.Infrastructure.Email
{
    public class DevEmailService : IEmailService
    {
        private readonly ILogger<DevEmailService> _logger;

        public DevEmailService(ILogger<DevEmailService> logger)
        {
            _logger = logger;
        }


        public Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            _logger.LogInformation("DEV EMAIL to {}. Subject: {Subject}. Body: {Body}", toEmail, subject, htmlBody);
            return Task.CompletedTask;
        }
    }
}
