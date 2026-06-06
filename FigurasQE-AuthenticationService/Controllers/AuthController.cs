using FigurasQE_AuthenticationService.Models;
using FigurasQE_AuthenticationService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FigurasQE_AuthenticationService.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var response = await _authService.Login(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("admin/login")]
    public async Task<IActionResult> AdminLogin(LoginRequest request)
    {
        try
        {
            var response = await _authService.AdminLogin(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Signup(RegisterRequest request)
    {
        var response = await _authService.Signup(request);
        return Ok(response);
    }
}