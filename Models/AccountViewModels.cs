using System.ComponentModel.DataAnnotations;

namespace ConcesionariaApp.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Ingresá tu email.")]
    [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ingresá tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = "";

    [Display(Name = "Recordarme")]
    public bool RememberMe { get; set; }
}

public sealed record DashboardViewModel(string Nombre, string Rol);
