using System.Linq;
using Microsoft.AspNetCore.Identity;

namespace UserIdentityApi.Infrastructure.Extensions
{
    public static class IdentityResultExtensions
    {
        public static string GetError(this IdentityResult identityResult)
        {
            return identityResult.Errors.Select(e => e.Description).First();
        }
    }
}