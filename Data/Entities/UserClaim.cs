using Microsoft.AspNetCore.Identity;

namespace UserIdentityApi.Data.Entities
{
    public class UserClaim : IdentityUserClaim<int>
    {
        public virtual User User { get; set; }
    }
}