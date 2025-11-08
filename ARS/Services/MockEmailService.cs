using System.Collections.Concurrent;

namespace ARS.Services
{
    public class MockEmailService : IEmailService
    {
        private readonly ConcurrentBag<MockEmailMessage> _messages = new();

        public Task SendAsync(string to, string subject, string body)
        {
            var msg = new MockEmailMessage
            {
                SentAt = DateTime.UtcNow,
                To = to,
                Subject = subject,
                Body = body
            };

            _messages.Add(msg);

            // Log ra console cho dễ debug
            Console.WriteLine("=== MOCK EMAIL SENT ===");
            Console.WriteLine($"To: {to}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Body:\n{body}");
            Console.WriteLine("=======================");

            return Task.CompletedTask;
        }

        public IReadOnlyList<MockEmailMessage> GetAll()
        {
            return _messages.ToList();
        }
    }
}
