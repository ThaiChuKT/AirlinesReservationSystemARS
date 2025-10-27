using ARS.Models.DTO;
using ARS.Services;
using Microsoft.AspNetCore.Mvc;

namespace ARS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResDTO>> Register([FromBody] RegDTO regDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _userService.RegisterAsync(regDto);
            if (result == null || !result.Success)
            {
                return BadRequest(new { message = result?.Message ?? "Registration failed." });
            }
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResDTO>> Login([FromBody] LoginDTO loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _userService.LoginAsync(loginDto);
            if (result == null || !result.Success)
            {
                return Unauthorized(new { message = result?.Message ?? "Login failed." });
            }
            return Ok(result);
        }
    }
}