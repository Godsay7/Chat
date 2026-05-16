using Microsoft.AspNetCore.Mvc;
using Messenger.API.Models;
using Messenger.API.Services;

namespace Messenger.API.Api;

[ApiController]
[Route("conversations")]
public class ConversationsController : ControllerBase
{
    private readonly ConversationService _convs;
    private readonly MessageService _messages;

    public ConversationsController(ConversationService convs, MessageService messages)
    {
        _convs = convs;
        _messages = messages;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConversationRequest req)
    {
        try
        {
            if (req.Type == ConversationType.Direct && req.MemberIds.Count == 2)
            {
                var conv = await _convs.FindOrCreateDirectAsync(req.MemberIds[0], req.MemberIds[1]);
                return Ok(ToDto(conv));
            }

            var created = await _convs.CreateAsync(req.Type, req.MemberIds);
            return Created($"/conversations/{created.Id}", ToDto(created));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("direct")]
    public async Task<IActionResult> FindOrCreateDirect([FromBody] DirectConversationRequest req)
    {
        try
        {
            var conv = await _convs.FindOrCreateDirectAsync(req.UserId, req.OtherUserId);
            return Ok(ToDto(conv));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(string id)
    {
        try
        {
            var messages = await _messages.GetHistoryAsync(id);
            return Ok(messages);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    private static ConversationDto ToDto(Conversation conv) =>
        new(
            conv.Id,
            conv.Type.ToString(),
            conv.Members.Select(m => new UserDto(m.UserId, m.User?.Username ?? "")).ToList()
        );
}

public record DirectConversationRequest(string UserId, string OtherUserId);
