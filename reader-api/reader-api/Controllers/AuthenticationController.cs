using Microsoft.AspNetCore.Mvc;
using Reader.Api.Application.UseCases;
using reader_api.Authentication;

namespace reader_api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController(LocalCredentialStore credentials, JwtTokenService tokens, RegisterUserHandler registerUser) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        if (!credentials.IsValid(request.Email, request.Password))
        {
            return Unauthorized();
        }

        return Ok(new LoginResponse(tokens.Create(request.Email)));
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
            if (!credentials.Register(request.Email, request.Password))
            {
                return Conflict(new { message = "An account with this email already exists." });
            }

            await registerUser.HandleAsync(new RegisterUserCommand(request.Email, request.DisplayName), cancellationToken);
            return Created(string.Empty, new LoginResponse(tokens.Create(request.Email)));
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