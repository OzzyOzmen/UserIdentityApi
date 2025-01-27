using System.ComponentModel.DataAnnotations;

namespace UserIdentityApi.Models
{
    public class ResendVerificationPinDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
} 