using System.ComponentModel.DataAnnotations;

namespace UserIdentityApi.Models
{
    public class EmailVerificationPinDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6)]
        [RegularExpression("^[0-9]*$", ErrorMessage = "PIN must be numeric")]
        public string Pin { get; set; }
    }
} 