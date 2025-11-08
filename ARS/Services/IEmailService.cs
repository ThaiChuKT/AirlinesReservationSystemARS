namespace ARS.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
        IReadOnlyList<MockEmailMessage> GetAll();
    }

    public class MockEmailMessage
    {
        public DateTime SentAt { get; set; }
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
