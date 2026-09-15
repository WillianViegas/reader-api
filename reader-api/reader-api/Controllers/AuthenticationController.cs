using Microsoft.AspNetCore.Mvc;
using Reader.Api.Application.UseCases;
using reader_api.Authentication;

namespace reader_api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController(UserAuthenticationService authentication, JwtTokenService tokens) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await authentication.AuthenticateAsync(request.Email, request.Password, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new LoginResponse(tokens.Create(user)));
    }

    [HttpPost("register")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return BadRequest(new { message = "The password must contain at least 8 characters." });
        }

        try
        {
            var user = await authentication.RegisterAsync(request.Email, request.Password, request.DisplayName, cancellationToken);
            return Created(string.Empty, new LoginResponse(tokens.Create(user)));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record LoginResponse(string AccessToken);