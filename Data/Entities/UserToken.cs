using Microsoft.AspNetCore.Identity;

namespace UserIdentityApi.Data.Entities
{
    public class UserToken : IdentityUserToken<int>
    {
        public virtual User User { get; set; }
    }
}