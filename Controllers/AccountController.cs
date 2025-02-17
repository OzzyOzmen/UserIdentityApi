using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UserIdentityApi.Data.Entities;
using UserIdentityApi.Infrastructure.Extensions;
using UserIdentityApi.Infrastructure.Helpers;
using UserIdentityApi.Models;
using UserIdentityApi.Services;
using Microsoft.Extensions.Logging;

namespace UserIdentityApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailSender emailSender,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody]LoginDto request)
        {
            // Tek sorguda user ve rollerini al
            var user = await _userManager.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user != null)
            {
                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
                if (result.Succeeded)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                        new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                    };

                    // Get roles directly from UserRoles
                    foreach (var userRole in user.UserRoles)
                    {
                        var role = userRole.Role;
                        claims.Add(new Claim(ClaimTypes.Role, role.Name));
                        claims.Add(new Claim("roleId", role.Id.ToString()));
                    }

                    var securityKey = _configuration["JwtConfiguration:SecurityKey"] ?? 
                        throw new InvalidOperationException("Security key is not configured");
                    
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    var expires = DateTime.UtcNow.AddDays(1);

                    var token = new JwtSecurityToken(
                        issuer: _configuration["JwtConfiguration:Issuer"],
                        audience: _configuration["JwtConfiguration:Audience"],
                        claims: claims,
                        notBefore: DateTime.UtcNow,
                        expires: expires,
                        signingCredentials: creds
                    );

                    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                    return Ok(new
                    {
                        token = tokenString,
                        expiration = expires
                    });
                }
                return Unauthorized(new { message = "Invalid credentials" });
            }
            return NotFound(new { message = "User not found" });
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Account/Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Normalize strings to prevent null values
            var normalizedRequest = new RegisterDto
            {
                UserName = request.UserName?.Trim() ?? string.Empty,
                Email = request.Email?.Trim() ?? string.Empty,
                Name = request.Name?.Trim() ?? string.Empty,
                Surname = request.Surname?.Trim() ?? string.Empty,
                Password = request.Password ?? string.Empty,
                ConfirmPassword = request.ConfirmPassword ?? string.Empty,
                PhoneNumber = request.PhoneNumber?.Trim()
            };

            if (string.IsNullOrWhiteSpace(normalizedRequest.Email) || string.IsNullOrWhiteSpace(normalizedRequest.UserName))
            {
                return BadRequest("Email and UserName are required.");
            }

            // Check if username already exists
            var existingUser = await _userManager.FindByNameAsync(normalizedRequest.UserName);
            if (existingUser != null)
            {
                return BadRequest("This username is already taken. Please choose another one.");
            }

            // Validate username format (only letters, numbers, and underscores allowed)
            if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedRequest.UserName, "^[a-zA-Z0-9_]+$"))
            {
                return BadRequest("Username can only contain letters, numbers, and underscores.");
            }

            var user = new User
            {
                UserName = normalizedRequest.UserName,
                Email = normalizedRequest.Email,
                Name = normalizedRequest.Name,
                Surname = normalizedRequest.Surname,
                PhoneNumber = normalizedRequest.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, normalizedRequest.Password);
            if (result.Succeeded)
            {
                try
                {
                    // Ensure "User" role exists
                    var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<Role>>();
                    if (!await roleManager.RoleExistsAsync("User"))
                    {
                        await roleManager.CreateAsync(new Role { Name = "User" });
                    }

                    // Assign "User" role to the new user
                    await _userManager.AddToRoleAsync(user, "User");

                    await SendEmailVerificationPin(user);
                    return Created($"User/{user.Id}", new { 
                        id = user.Id,
                        userName = user.UserName,
                        email = user.Email,
                        name = user.Name,
                        surname = user.Surname,
                        photo = user.Photo,
                        roles = new[] { "User" },
                        message = "Registration successful. Please check your email for verification PIN."
                    });
                }
                catch (Exception ex)
                {
                    // Log the error but still return success since user was created
                    _logger.LogError(ex, $"Failed to send verification email to {user.Email}: {ex.Message}");
                    return Created($"User/{user.Id}", new { 
                        id = user.Id,
                        userName = user.UserName,
                        email = user.Email,
                        name = user.Name,
                        surname = user.Surname,
                        photo = user.Photo,
                        roles = new[] { "User" },
                        message = "Registration successful but failed to send verification email. Please use the resend verification option."
                    });
                }
            }

            return BadRequest(result.GetError());
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Account/ResendVerificationPin")]
        public async Task<IActionResult> ResendVerificationPin([FromBody] ResendVerificationPinDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            if (user.EmailConfirmed)
            {
                return BadRequest("Email is already confirmed.");
            }

            try
            {
                await SendEmailVerificationPin(user);
                return Ok("Verification PIN has been sent to your email.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send verification email to {user.Email}: {ex.Message}");
                return BadRequest("Failed to send verification email. Please try again later.");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Account/VerifyEmail")]
        public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationPinDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest("Invalid email address.");
            }

            if (user.EmailConfirmed)
            {
                return BadRequest("Email is already confirmed.");
            }

            if (string.IsNullOrEmpty(user.EmailVerificationPin) || 
                user.EmailVerificationPinExpiration == null || 
                user.EmailVerificationPinExpiration < DateTime.UtcNow)
            {
                return BadRequest("Verification PIN has expired. Please request a new one.");
            }

            if (user.EmailVerificationPin != request.Pin)
            {
                return BadRequest("Invalid verification PIN.");
            }

            user.EmailConfirmed = true;
            user.EmailVerificationPin = null;
            user.EmailVerificationPinExpiration = null;
            
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Ok("Email has been confirmed successfully.");
            }

            return BadRequest(result.GetError());
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Account/ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody]ForgotPasswordDto request)
        {
            try
            {
                _logger.LogInformation($"ForgotPassword request received for email: {request.Email}");
                
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
                {
                    _logger.LogWarning($"User not found or email not confirmed for: {request.Email}");
                    return BadRequest("Please try another email address.");
                }

                // Generate 6-digit pin
                var resetPin = GenerateRandomPin();
                _logger.LogInformation($"Generated reset PIN for user: {request.Email}");
                
                // Save pin and expiration time to user
                user.PasswordResetCode = resetPin;
                user.PasswordResetCodeExpiration = DateTime.UtcNow.AddMinutes(15); // Valid for 15 minutes
                
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError($"Failed to update user with reset PIN: {string.Join(", ", updateResult.Errors)}");
                    return BadRequest("Failed to generate reset code. Please try again.");
                }

                // Send email
                var emailContent = $@"
                    <h2>Password Reset Code</h2>
                    <p>Your password reset code is: <strong>{resetPin}</strong></p>
                    <p>This code will expire in 15 minutes.</p>
                    <p>If you did not request a password reset, please ignore this email.</p>";

                await _emailSender.SendEmailAsync(
                    request.Email,
                    "Password Reset Code",
                    emailContent
                );

                _logger.LogInformation($"Password reset code sent successfully to: {request.Email}");
                return Ok(new { message = "Password reset code has been sent to your email." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process password reset for {request.Email}: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return BadRequest("Failed to send password reset code. Please try again later.");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Account/ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody]ResetPasswordDto request)
        {
            try
            {
                _logger.LogInformation($"Reset password request received for email: {request.Email}");
                
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for email: {request.Email}");
                    return BadRequest("Please try another email address.");
                }

                // Check pin validity
                if (string.IsNullOrEmpty(user.PasswordResetCode) || 
                    user.PasswordResetCodeExpiration == null || 
                    user.PasswordResetCodeExpiration < DateTime.UtcNow)
                {
                    _logger.LogWarning($"Reset code expired or invalid for user: {request.Email}");
                    return BadRequest("Reset code has expired. Please request a new one.");
                }

                // Check if the sent pin is correct
                if (user.PasswordResetCode != request.Code)
                {
                    _logger.LogWarning($"Invalid reset code provided for user: {request.Email}");
                    return BadRequest("Invalid reset code.");
                }

                // Update password
                var removePasswordResult = await _userManager.RemovePasswordAsync(user);
                if (!removePasswordResult.Succeeded)
                {
                    var removeErrors = string.Join(", ", removePasswordResult.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to remove old password for user {request.Email}: {removeErrors}");
                    return BadRequest($"Failed to remove old password: {removeErrors}");
                }

                var addPasswordResult = await _userManager.AddPasswordAsync(user, request.Password);
                if (!addPasswordResult.Succeeded)
                {
                    var addErrors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to set new password for user {request.Email}: {addErrors}");
                    return BadRequest($"Failed to set new password: {addErrors}");
                }

                // Pin'i temizle
                user.PasswordResetCode = null;
                user.PasswordResetCodeExpiration = null;
                await _userManager.UpdateAsync(user);
                
                _logger.LogInformation($"Password reset successful for user: {request.Email}");
                return Ok(new { message = "Your password has been reset successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to reset password for {request.Email}: {ex.Message}");
                return BadRequest("An error occurred while resetting your password. Please try again.");
            }
        }

        private async Task SendEmailVerificationPin(User user)
        {
            var pin = GenerateRandomPin();
            user.EmailVerificationPin = pin;
            user.EmailVerificationPinExpiration = DateTime.UtcNow.AddMinutes(15); // PIN expires in 15 minutes
            
            await _userManager.UpdateAsync(user);
            
            await _emailSender.SendEmailAsync(user.Email, "Email Verification PIN",
                $"Your email verification PIN is: {pin}. This PIN will expire in 15 minutes.");
        }

        private string GenerateRandomPin()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
} 