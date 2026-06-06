namespace FQE.AdminClient.Models;

public class AdminProfile
{
    public int IdAdmin { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime? RegistrationDate { get; set; }
}