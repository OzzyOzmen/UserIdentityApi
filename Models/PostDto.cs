using System.ComponentModel.DataAnnotations;

namespace UserIdentityApi.Models
{
    public class PostDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Surname { get; set; }

        public string? Photo { get; set; }
    }
}