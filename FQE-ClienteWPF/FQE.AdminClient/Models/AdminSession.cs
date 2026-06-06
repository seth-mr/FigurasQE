namespace FQE.AdminClient.Models;

public class AdminSession
{
    public int IdAdmin { get; set; }
    public string Token { get; set; } = string.Empty;
    public AdminProfile Profile { get; set; } = new();
}