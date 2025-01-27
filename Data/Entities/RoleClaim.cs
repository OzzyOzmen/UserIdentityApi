using Microsoft.AspNetCore.Identity;

namespace UserIdentityApi.Data.Entities
{
    public class RoleClaim : IdentityRoleClaim<int>
    {
        public virtual Role Role { get; set; }
    }
}