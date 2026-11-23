using AgriSyncIndia.Services;
using System.ComponentModel.DataAnnotations;

namespace AgriSyncIndia.Models;

/// <summary>
/// User model representing agronomists, farm managers, and greenhouse operators.
/// </summary>
public class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Role { get; set; } = "Farm Manager"; // Agronomist, Farm Manager, Field Operator, Research Scholar

    [StringLength(150)]
    public string? AffiliatedFarmCluster { get; set; }

    [StringLength(100)]
    public string? ClerkUserId { get; set; }

    public DateTime CreatedAt { get; set; } = IstClock.Now;
}
