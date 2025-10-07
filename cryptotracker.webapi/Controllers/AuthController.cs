using cryptotracker.core.Interfaces;
using cryptotracker.database.Models;
using cryptotracker.webapi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace cryptotracker.webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICryptoTrackerConfig _config;
        private readonly JwtService _jwtService;

        public AuthController(UserManager<ApplicationUser> userManager, ICryptoTrackerConfig config, JwtService jwtService)
        {
            _userManager = userManager;
            _config = config;
            _jwtService = jwtService;
        }

        [HttpGet("oidc-login")]
        public IActionResult OidcLogin([FromQuery] string? returnUrl = "/")
        {
            var props = new AuthenticationProperties { RedirectUri = returnUrl };
            return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, request.Password))
            {
                var jwt = _jwtService.GenerateJwtToken(user);
                _jwtService.SetJwtCookie(Response, jwt);
                return Ok();
            }
            return Unauthorized();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var existingUser = await _userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
                return BadRequest("Username already exists.");

            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            var jwt = _jwtService.GenerateJwtToken(user);
            _jwtService.SetJwtCookie(Response, jwt);

            return Ok();
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return Ok();
        }

        public record RegisterRequest(string Username, string Email, string Password);
        public record LoginRequest(string Username, string Password);
    }
}