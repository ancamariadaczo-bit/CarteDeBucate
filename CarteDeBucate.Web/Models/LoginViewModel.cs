using System.ComponentModel.DataAnnotations;

namespace CarteDeBucate.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username-ul este obligatoriu.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Parola este obligatorie.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
