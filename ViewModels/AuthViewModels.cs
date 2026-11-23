using System.ComponentModel.DataAnnotations;

namespace AgriSyncIndia.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your official email address.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember this device")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}
