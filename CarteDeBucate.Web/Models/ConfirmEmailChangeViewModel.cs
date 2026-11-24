using System.ComponentModel.DataAnnotations;

namespace CarteDeBucate.Web.Models;

public sealed class ConfirmEmailChangeViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
