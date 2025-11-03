namespace ARS.Models.DTO;

public class AuthResDTO
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}