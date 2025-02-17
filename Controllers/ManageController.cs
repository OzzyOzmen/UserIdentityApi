using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserIdentityApi.Data.Entities;
using UserIdentityApi.Models;
using UserIdentityApi.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace UserIdentityApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ManageController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public ManageController(
            UserManager<User> userManager,
            RoleManager<Role> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new {
                id = user.Id,
                userName = user.UserName,
                email = user.Email,
                name = user.Name,
                surname = user.Surname,
                photo = user.Photo,
                phoneNumber = user.PhoneNumber,
                emailConfirmed = user.EmailConfirmed,
                roles = roles
            });
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody]UpdateProfileDto request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            // Check fields to update
            if (!string.IsNullOrEmpty(request.UserName) && user.UserName != request.UserName)
            {
                var userNameExists = await _userManager.FindByNameAsync(request.UserName);
                if (userNameExists != null && userNameExists.Id != user.Id)
                {
                    return BadRequest("Bu kullanıcı adı zaten kullanılıyor.");
                }
                user.UserName = request.UserName;
            }

            if (!string.IsNullOrEmpty(request.Email) && user.Email != request.Email)
            {
                var emailExists = await _userManager.FindByEmailAsync(request.Email);
                if (emailExists != null && emailExists.Id != user.Id)
                {
                    return BadRequest("Bu e-posta adresi zaten kullanılıyor.");
                }
                user.Email = request.Email;
                user.EmailConfirmed = false; // Email verification required when email changes
            }

            if (!string.IsNullOrEmpty(request.Name))
                user.Name = request.Name;
            
            if (!string.IsNullOrEmpty(request.Surname))
                user.Surname = request.Surname;
            
            if (!string.IsNullOrEmpty(request.PhoneNumber))
                user.PhoneNumber = request.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                return Ok(new { 
                    message = "Profiliniz başarıyla güncellendi.",
                    user = new {
                        id = user.Id,
                        userName = user.UserName,
                        email = user.Email,
                        name = user.Name,
                        surname = user.Surname,
                        photo = user.Photo,
                        phoneNumber = user.PhoneNumber,
                        emailConfirmed = user.EmailConfirmed,
                        roles = roles
                    }
                });
            }

            return BadRequest(result.GetError());
        }

        [HttpPost("upload-photo")]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                return BadRequest("No photo was uploaded.");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png" };
            if (!allowedTypes.Contains(photo.ContentType.ToLower()))
            {
                return BadRequest("Only JPEG and PNG images are allowed.");
            }

            // Convert the photo to base64
            using var memoryStream = new MemoryStream();
            await photo.CopyToAsync(memoryStream);
            var photoBytes = memoryStream.ToArray();
            var base64Photo = Convert.ToBase64String(photoBytes);

            // Update user's photo
            user.Photo = $"data:{photo.ContentType};base64,{base64Photo}";
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { 
                    message = "Photo has been updated successfully.",
                    photo = user.Photo
                });
            }

            return BadRequest(result.GetError());
        }

        // Admin only endpoints
        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Select(u => new
                {
                    id = u.Id,
                    userName = u.UserName,
                    email = u.Email,
                    name = u.Name,
                    surname = u.Surname,
                    photo = u.Photo,
                    phoneNumber = u.PhoneNumber,
                    roleId = u.UserRoles.FirstOrDefault().Role.Id,
                    role = u.UserRoles.FirstOrDefault().Role.Name,
                    emailConfirmed = u.EmailConfirmed
                })
                .ToListAsync();

            return Ok(users);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                Name = model.Name,
                Surname = model.Surname,
                EmailConfirmed = false // Email verification required
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.GetError());
            }

            // Role assignment
            var role = await _roleManager.FindByIdAsync(model.RoleId.ToString());
            if (role == null)
            {
                return BadRequest("Invalid role");
            }

            await _userManager.AddToRoleAsync(user, role.Name);

            return Created($"/api/Manage/users/{user.Id}", new
            {
                id = user.Id,
                userName = user.UserName,
                email = user.Email,
                name = user.Name,
                surname = user.Surname,
                roleId = model.RoleId,
                role = role.Name,
                emailConfirmed = user.EmailConfirmed
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("update-user/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound("User not found");
            }

            user.UserName = model.UserName;
            user.Email = model.Email;
            user.Name = model.Name;
            user.Surname = model.Surname;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.GetError());
            }

            // Role update
            var currentRole = user.UserRoles.FirstOrDefault()?.Role;
            var newRole = await _roleManager.FindByIdAsync(model.RoleId.ToString());
            
            if (newRole == null)
            {
                return BadRequest("Invalid role");
            }

            if (currentRole != null && currentRole.Id != model.RoleId)
            {
                await _userManager.RemoveFromRoleAsync(user, currentRole.Name);
                await _userManager.AddToRoleAsync(user, newRole.Name);
            }

            return Ok(new
            {
                id = user.Id,
                userName = user.UserName,
                email = user.Email,
                name = user.Name,
                surname = user.Surname,
                roleId = model.RoleId,
                role = newRole.Name,
                emailConfirmed = user.EmailConfirmed
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound("User not found");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.GetError());
            }

            return Ok(new { message = "User deleted successfully" });
        }
    }
} 