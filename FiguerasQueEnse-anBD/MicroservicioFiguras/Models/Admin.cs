using System;

namespace MicroservicioFiguras.Models;

public partial class Admin
{
    public int IdAdmin { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime? RegistrationDate { get; set; }
}