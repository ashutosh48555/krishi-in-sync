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

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select your primary agricultural role.")]
    [Display(Name = "Organizational Role")]
    public string Role { get; set; } = "Agronomist";

    [Display(Name = "Primary Farm Cluster")]
    public string? AffiliatedFarmCluster { get; set; }

    [Display(Name = "I agree to ICAR/IMD Agromet Data Monitoring Protocols")]
    public bool AgreeToAgrometCompliance { get; set; } = true;
}
