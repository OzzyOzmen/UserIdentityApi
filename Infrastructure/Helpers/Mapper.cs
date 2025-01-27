using UserIdentityApi.Data.Entities;
using UserIdentityApi.Models;

namespace UserIdentityApi.Infrastructure.Helpers
{
   public class Mapper
    {
        public UserDto MapToUserDto(User user)
        {
            var userDto = new UserDto()
            {
                Id = user.Id,
                Name = user.Name,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber
            };

            return userDto;
        }
    }
}