namespace FigurasQE_AuthenticationService.Models;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public AuthAdminDto? Admin { get; set; }
}

public class AuthAdminDto
{
    public int IdAdmin { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime? RegistrationDate { get; set; }
}