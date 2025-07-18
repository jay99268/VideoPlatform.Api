namespace VideoPlatform.Api.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
    public class MockEmailService: IEmailService
    {
        public Task SendEmailAsync(string toEmail, string subject, string message)
        {
            Console.WriteLine("--- MOCK EMAIL SENDER ---");
            Console.WriteLine($"To: {toEmail}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Message: {message}");
            Console.WriteLine("-------------------------");
            return Task.CompletedTask;
        }
    }
}
