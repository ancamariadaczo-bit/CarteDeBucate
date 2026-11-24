using System.ComponentModel.DataAnnotations;

namespace CarteDeBucate.Web.Models;

public sealed class VerifyEmailViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
