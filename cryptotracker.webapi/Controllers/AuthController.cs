using System.Security.Claims;
using cryptotracker.core.Interfaces;
using cryptotracker.database.Models;
using cryptotracker.webapi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
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

        [Authorize]
        [HttpGet("me", Name = "GetMe")]
        public async Task<ActionResult<MeResponse>> Me()
        {
            Console.WriteLine("Me called");
            Console.WriteLine(User?.Identity?.IsAuthenticated);
            var email = User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            Console.WriteLine($"Claims: {string.Join(", ", User?.Claims?.Select(c => $"{c.Type}: {c.Value}") ?? Array.Empty<string>())}");
            if (email == null)
                throw new ArgumentNullException("User not found");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return Unauthorized();

            return Ok(new MeResponse(user.UserName, user.Email, user.UserName));
        }

        public record MeResponse(string? UserName, string? Email, string? DisplayName);

        [HttpGet("oidc-login", Name = "OidcLogin")]
        public IActionResult OidcLogin([FromQuery] string? returnUrl = "/")
        {
            Console.WriteLine("OIDC Login called");
            var targetUrl = string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl)
                ? "/"
                : returnUrl;

            var props = new AuthenticationProperties { RedirectUri = targetUrl };
            return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpPost("login", Name = "Login")]
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

        [HttpPost("register", Name = "Register")]
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

        [HttpPost("logout", Name = "Logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return Ok();
        }

        public record RegisterRequest(string Username, string Email, string Password);
        public record LoginRequest(string Username, string Password);
    }
}