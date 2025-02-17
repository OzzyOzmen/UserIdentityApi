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
using System.Text.Json;

namespace UserIdentityApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public UserController(
            UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute]int id)
        {
            var user = await _userManager.Users
                .Include(u => u.Claims)
                .Include(u => u.Logins)
                .Include(u => u.Tokens)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(new
            {
                id = user.Id,
                userName = user.UserName,
                email = user.Email,
                name = user.Name,
                surname = user.Surname,
                photo = user.Photo,
                phoneNumber = user.PhoneNumber,
                claims = user.Claims.Select(c => new { c.ClaimType, c.ClaimValue }),
                logins = user.Logins.Select(l => l.LoginProvider)
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userManager.Users
                .Include(u => u.Claims)
                .ToListAsync();

            return Ok(users.Select(user => new
            {
                id = user.Id,
                userName = user.UserName,
                email = user.Email,
                name = user.Name,
                surname = user.Surname,
                photo = user.Photo,
                phoneNumber = user.PhoneNumber,
                claims = user.Claims.Select(c => new { c.ClaimType, c.ClaimValue })
            }));
        }

        [HttpGet("statistics")]
        [Authorize]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var totalUsers = await _userManager.Users.CountAsync();
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var consultants = await _userManager.GetUsersInRoleAsync("SuperUser");
                var customers = await _userManager.GetUsersInRoleAsync("User");

                // Log the counts
                Console.WriteLine($"Statistics - Total Users: {totalUsers}, Admins: {admins.Count}, Consultants: {consultants.Count}, Customers: {customers.Count}");

                var response = new
                {
                    totalUsers = totalUsers,
                    totalAdmins = admins.Count,
                    totalConsultants = consultants.Count,
                    totalCustomers = customers.Count
                };

                // Log the response
                Console.WriteLine($"Response: {JsonSerializer.Serialize(response)}");

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStatistics: {ex.Message}");
                return StatusCode(500, "An error occurred while getting user statistics");
            }
        }
    }
} 