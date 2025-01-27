using System.ComponentModel.DataAnnotations;

namespace UserIdentityApi.Models
{
   public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}