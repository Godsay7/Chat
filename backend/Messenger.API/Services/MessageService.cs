using Microsoft.EntityFrameworkCore;
using Messenger.API.Models;
using Messenger.API.Storage;

namespace Messenger.API.Services;

public class MessageService
{
    private readonly AppDbContext _db;

    public MessageService(AppDbContext db) => _db = db;

    public async Task<Message> SendAsync(string conversationId, string senderId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Message text cannot be empty.");

        var conversation = await _db.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new KeyNotFoundException("Conversation not found.");

        if (await _db.Users.FindAsync(senderId) is null)
            throw new KeyNotFoundException("Sender not found.");

        bool isMember = conversation.Members.Any(m => m.UserId == senderId);
        if (!isMember)
            throw new UnauthorizedAccessException("Sender is not a member of this conversation.");

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Text = text.Trim()
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();
        return message;
    }

    public async Task<Message> EditAsync(string messageId, string requesterId, string newText)
    {
        if (string.IsNullOrWhiteSpace(newText))
            throw new ArgumentException("Edited text cannot be empty.");

        var message = await _db.Messages.FindAsync(messageId)
            ?? throw new KeyNotFoundException("Message not found.");

        if (message.SenderId != requesterId)
            throw new UnauthorizedAccessException("Only the sender can edit this message.");

        message.Text = newText.Trim();
        message.IsEdited = true;
        message.EditedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return message;
    }

    public async Task<List<MessageDto>> GetHistoryAsync(string conversationId)
    {
        if (!await _db.Conversations.AnyAsync(c => c.Id == conversationId))
            throw new KeyNotFoundException("Conversation not found.");

        return await _db.Messages
            .Where(m => m.ConversationId == conversationId)
            .Include(m => m.Sender)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto(
                m.Id,
                m.ConversationId,
                m.SenderId,
                m.Sender!.Username,
                m.Text,
                m.CreatedAt,
                m.IsEdited,
                m.EditedAt
            ))
            .ToListAsync();
    }
}
