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
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Official email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(100)]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select your primary role.")]
    [StringLength(50)]
    [Display(Name = "Role / Designation")]
    public string Role { get; set; } = "Agronomist";

    [StringLength(150)]
    [Display(Name = "Primary Farm Cluster")]
    public string? AffiliatedFarmCluster { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "I accept the operational protocol")]
    public bool AcceptTerms { get; set; } = true;

    public string? ReturnUrl { get; set; }
}
