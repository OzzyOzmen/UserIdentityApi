using Microsoft.AspNetCore.Identity;

namespace UserIdentityApi.Data.Entities
{
    public class User : IdentityUser<int>
    {
        private const string DEFAULT_PHOTO_URL = "https://upload.wikimedia.org/wikipedia/commons/8/89/Portrait_Placeholder.png";

        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string? Photo { get; set; }
        public string? EmailVerificationPin { get; set; }
        public DateTime? EmailVerificationPinExpiration { get; set; }
        public string? PasswordResetCode { get; set; }
        public DateTime? PasswordResetCodeExpiration { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; }
        public virtual ICollection<UserClaim> Claims { get; set; }
        public virtual ICollection<UserLogin> Logins { get; set; }
        public virtual ICollection<UserToken> Tokens { get; set; }

        public User()
        {
            UserName = string.Empty;
            Email = string.Empty;
            PhoneNumber = string.Empty;
            Photo = DEFAULT_PHOTO_URL;
            UserRoles = new HashSet<UserRole>();
            Claims = new HashSet<UserClaim>();
            Logins = new HashSet<UserLogin>();
            Tokens = new HashSet<UserToken>();
        }

        public bool HasDefaultPhoto()
        {
            return Photo == DEFAULT_PHOTO_URL;
        }
    }
}