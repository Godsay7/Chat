using Microsoft.AspNetCore.Mvc;
using Messenger.API.Models;
using Messenger.API.Services;

namespace Messenger.API.Api;

[ApiController]
[Route("messages")]
public class MessagesController : ControllerBase
{
    private readonly MessageService _messages;
    public MessagesController(MessageService messages) => _messages = messages;

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest req)
    {
        try
        {
            var message = await _messages.SendAsync(req.ConversationId, req.SenderId, req.Text);
            return Created($"/messages/{message.Id}", new MessageDto(
                message.Id, message.ConversationId, message.SenderId,
                "", message.Text, message.CreatedAt, message.IsEdited, message.EditedAt
            ));
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Edit(string id, [FromBody] EditMessageRequest req,
        [FromQuery] string requesterId)
    {
        try
        {
            var message = await _messages.EditAsync(id, requesterId, req.Text);
            return Ok(new MessageDto(
                message.Id, message.ConversationId, message.SenderId,
                "", message.Text, message.CreatedAt, message.IsEdited, message.EditedAt
            ));
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}