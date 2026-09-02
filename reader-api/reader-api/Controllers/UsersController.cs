using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reader.Api.Application.Dtos;
using Reader.Api.Application.UseCases;

namespace reader_api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(GetUserProfileHandler getUserProfile) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<UserProfileDto>> GetMe(CancellationToken cancellationToken) => GetProfileAsync(cancellationToken);

    private async Task<ActionResult<UserProfileDto>> GetProfileAsync(CancellationToken cancellationToken) =>
        Ok(await getUserProfile.HandleAsync(new GetUserProfileQuery(), cancellationToken));
}