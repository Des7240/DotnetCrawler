using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotnetCrawler.Data;
using DotnetCrawler.Entities;
using System.Threading.Tasks;

namespace DotnetCrawler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AuthController(AppDbContext db)
        {
            _db = db;
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrEmpty(req.Username) || string.IsNullOrEmpty(req.Password))
                return BadRequest("Username and Password required.");

            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Username == req.Username);
            
            // For simplicity, plain text password match (since this is a simple reviewer account system)
            // In production, use BCrypt.
            if (user == null)
            {
                // Register if not found
                user = new AppUser { Username = req.Username, PasswordHash = req.Password };
                _db.AppUsers.Add(user);
                await _db.SaveChangesAsync();
                return Ok(new { user.Id, user.Username, Message = "Created new user and logged in." });
            }

            if (user.PasswordHash != req.Password)
            {
                return Unauthorized("Invalid password.");
            }

            return Ok(new { user.Id, user.Username, Message = "Logged in." });
        }
    }
}
