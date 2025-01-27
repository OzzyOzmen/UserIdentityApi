using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UserIdentityApi.Data.Entities;
using UserIdentityApi.Models;
using UserIdentityApi.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;

namespace UserIdentityApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ManageController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ManageController> _logger;

        public ManageController(
            UserManager<User> userManager,
            ILogger<ManageController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody]PostDto request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            user.Name = request.Name;
            user.Surname = request.Surname;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Ok(new { 
                    message = "Your profile has been updated.",
                    user = new {
                        name = user.Name,
                        surname = user.Surname,
                        photo = user.Photo
                    }
                });
            }

            return BadRequest(result.GetError());
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            return Ok(new {
                name = user.Name,
                surname = user.Surname,
                photo = user.Photo,
                email = user.Email,
                userName = user.UserName,
                phoneNumber = user.PhoneNumber
            });
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
    }
} 