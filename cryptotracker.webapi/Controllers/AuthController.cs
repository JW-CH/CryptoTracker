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
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<ApplicationUser> userManager, ICryptoTrackerConfig config, JwtService jwtService, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _config = config;
            _logger = logger;
            _jwtService = jwtService;
        }

        [Authorize]
        [HttpGet("me", Name = "GetMe")]
        public async Task<ActionResult<MeResponse>> Me()
        {
            var email = User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (email == null)
                return Unauthorized("Email claim not found");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return Unauthorized("User not found");

            return Ok(new MeResponse(user.UserName, user.Email, user.DisplayName));
        }

        public record MeResponse(string? UserName, string? Email, string? DisplayName);

        [HttpGet("oidc-enabled", Name = "OidcEnabled")]
        public ActionResult<bool> OidcEnabled()
        {
            return Ok(_config.Oidc.IsEnabled);
        }

        [HttpGet("oidc-login", Name = "OidcLogin")]
        public IActionResult OidcLogin([FromQuery] string? returnUrl = "/")
        {
            if (!_config.Oidc.IsEnabled)
                return NotFound();

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
            _logger.LogTrace("Login attempt for username: {Username}", request.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, request.Password))
            {
                var jwt = _jwtService.GenerateJwtToken(user, Request);
                _jwtService.SetJwtCookie(Response, jwt);
                _logger.LogTrace("User logged in successfully: {Username}", request.Username);
                return Ok();
            }
            _logger.LogWarning("Invalid login attempt for username: {Username}", request.Username);
            return Unauthorized();
        }

        [HttpPost("register", Name = "Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var existingUser = await _userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration attempt with existing username: {Username}", request.Username);
                return BadRequest("Username already exists.");
            }

            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                _logger.LogWarning("User registration failed for {Username}: {Errors}", request.Username, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors);
            }

            var jwt = _jwtService.GenerateJwtToken(user, Request);
            _jwtService.SetJwtCookie(Response, jwt);

            _logger.LogTrace("User registered successfully: {Username}", request.Username);

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