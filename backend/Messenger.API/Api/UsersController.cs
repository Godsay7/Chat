using Microsoft.AspNetCore.Mvc;
using Messenger.API.Models;
using Messenger.API.Services;

namespace Messenger.API.Api;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly UserService _users;

    public UsersController(UserService users) => _users = users;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest req)
    {
        try
        {
            var user = await _users.RegisterAsync(req.Username, req.Password);
            return Created($"/users/{user.Id}", new UserDto(user.Id, user.Username));
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest req)
    {
        try
        {
            var user = await _users.LoginAsync(req.Username, req.Password);
            return Ok(new UserDto(user.Id, user.Username));
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] string? excludeUserId)
    {
        var users = await _users.SearchAsync(q, excludeUserId);
        return Ok(users.Select(u => new UserDto(u.Id, u.Username)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null) return NotFound(new { error = "User not found." });
        return Ok(new UserDto(user.Id, user.Username));
    }

    [HttpGet("{id}/profile")]
    public async Task<IActionResult> GetProfile(string id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null) return NotFound(new { error = "User not found." });
        return Ok(UserService.ToProfileDto(user));
    }

    [HttpPatch("{id}/profile")]
    public async Task<IActionResult> UpdateProfile(string id, [FromBody] UpdateProfileRequest req)
    {
        try
        {
            var user = await _users.UpdateProfileAsync(
                id, req.CurrentPassword, req.NewUsername, req.NewPassword);
            return Ok(UserService.ToProfileDto(user));
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpGet("{userId}/conversations")]
    public async Task<IActionResult> GetConversations(string userId, [FromServices] ConversationService convs)
    {
        if (await _users.GetByIdAsync(userId) is null)
            return NotFound(new { error = "User not found." });

        var list = await convs.GetForUserAsync(userId);
        return Ok(list);
    }
}
