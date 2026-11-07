using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reader.Api.Application.Dtos;
using Reader.Api.Application.UseCases;

namespace reader_api.Controllers;

public sealed record UpdateUserProfileRequest([System.ComponentModel.DataAnnotations.Required] string DisplayName);

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(GetUserProfileHandler getUserProfile, UpdateUserProfileHandler updateUserProfile) : ControllerBase
{
    [HttpPut("me")]
    [HttpPatch("me")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateUserProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await updateUserProfile.HandleAsync(new UpdateUserProfileCommand(request.DisplayName), cancellationToken));

    [HttpGet("me")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<UserProfileDto>> GetMe(CancellationToken cancellationToken) => GetProfileAsync(cancellationToken);

    private async Task<ActionResult<UserProfileDto>> GetProfileAsync(CancellationToken cancellationToken) =>
        Ok(await getUserProfile.HandleAsync(new GetUserProfileQuery(), cancellationToken));
}