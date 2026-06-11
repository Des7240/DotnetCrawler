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
                bool isFirstUser = !await _db.AppUsers.AnyAsync();
                user = new AppUser { Username = req.Username, PasswordHash = req.Password, IsApproved = isFirstUser };
                _db.AppUsers.Add(user);
                await _db.SaveChangesAsync();
                
                if (!user.IsApproved)
                    return Unauthorized("Tài khoản đang chờ duyệt.");

                return Ok(new { user.Id, user.Username, Message = "Created new user and logged in." });
            }

            if (user.PasswordHash != req.Password)
            {
                return Unauthorized("Sai mật khẩu.");
            }

            if (!user.IsApproved)
            {
                return Unauthorized("Tài khoản đang chờ duyệt.");
            }

            return Ok(new { user.Id, user.Username, Message = "Logged in." });
        }

        // --- Admin Endpoints ---
        
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _db.AppUsers.Select(u => new { u.Id, u.Username, u.IsApproved }).ToListAsync();
            return Ok(users);
        }

        [HttpPut("approve/{id}")]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var user = await _db.AppUsers.FindAsync(id);
            if (user == null) return NotFound();

            user.IsApproved = true;
            await _db.SaveChangesAsync();
            return Ok(new { Message = "Đã duyệt tài khoản." });
        }

        [HttpPut("revoke/{id}")]
        public async Task<IActionResult> RevokeUser(int id)
        {
            var user = await _db.AppUsers.FindAsync(id);
            if (user == null) return NotFound();
            
            // Prevent locking out the main admin if needed, but we trust the admin.
            if (user.Username.ToLower() == "admin") return BadRequest("Không thể khóa tài khoản admin mặc định.");

            user.IsApproved = false;
            await _db.SaveChangesAsync();
            return Ok(new { Message = "Đã khóa tài khoản." });
        }

        [HttpDelete("reject/{id}")]
        public async Task<IActionResult> RejectUser(int id)
        {
            var user = await _db.AppUsers.FindAsync(id);
            if (user == null) return NotFound();
            
            if (user.Username.ToLower() == "admin") return BadRequest("Không thể xóa tài khoản admin mặc định.");

            _db.AppUsers.Remove(user);
            await _db.SaveChangesAsync();
            return Ok(new { Message = "Đã xóa tài khoản." });
        }
    }
}
