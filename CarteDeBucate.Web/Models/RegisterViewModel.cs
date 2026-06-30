using System.ComponentModel.DataAnnotations;

namespace CarteDeBucate.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Username-ul este obligatoriu.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Parola este obligatorie.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirmarea parolei este obligatorie.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Parolele nu coincid.")]
    public string ConfirmPassword { get; set; } = "";
}
