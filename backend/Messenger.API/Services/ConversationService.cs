using Microsoft.EntityFrameworkCore;
using Messenger.API.Models;
using Messenger.API.Storage;

namespace Messenger.API.Services;

public class ConversationService
{
    private readonly AppDbContext _db;

    public ConversationService(AppDbContext db) => _db = db;

    public async Task<Conversation> CreateAsync(ConversationType type, List<string> memberIds)
    {
        foreach (var id in memberIds)
        {
            if (await _db.Users.FindAsync(id) is null)
                throw new KeyNotFoundException($"User '{id}' not found.");
        }

        var conversation = new Conversation { Type = type };
        conversation.Members = memberIds
            .Distinct()
            .Select(uid => new ConversationMember { ConversationId = conversation.Id, UserId = uid })
            .ToList();

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(conversation.Id) ?? conversation;
    }

    public async Task<Conversation> FindOrCreateDirectAsync(string userId, string otherUserId)
    {
        if (userId == otherUserId)
            throw new ArgumentException("Cannot start a conversation with yourself.");

        var existing = await _db.Conversations
            .Include(c => c.Members).ThenInclude(m => m.User)
            .Where(c => c.Type == ConversationType.Direct)
            .Where(c => c.Members.Count == 2)
            .Where(c => c.Members.Any(m => m.UserId == userId) && c.Members.Any(m => m.UserId == otherUserId))
            .FirstOrDefaultAsync();

        if (existing is not null)
            return existing;

        return await CreateAsync(ConversationType.Direct, new List<string> { userId, otherUserId });
    }

    public async Task<List<ConversationListItemDto>> GetForUserAsync(string userId)
    {
        var conversationIds = await _db.ConversationMembers
            .Where(cm => cm.UserId == userId)
            .Select(cm => cm.ConversationId)
            .ToListAsync();

        var conversations = await _db.Conversations
            .Include(c => c.Members).ThenInclude(m => m.User)
            .Where(c => conversationIds.Contains(c.Id))
            .ToListAsync();

        var lastMessages = await _db.Messages
            .Where(m => conversationIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.OrderByDescending(m => m.CreatedAt).First())
            .ToListAsync();

        var lastByConv = lastMessages.ToDictionary(m => m.ConversationId);

        return conversations
            .Select(c =>
            {
                var others = c.Members.Where(m => m.UserId != userId).ToList();
                var title = c.Type == ConversationType.Direct
                    ? others.FirstOrDefault()?.User?.Username ?? "Unknown"
                    : string.Join(", ", others.Select(m => m.User?.Username ?? "?"));

                lastByConv.TryGetValue(c.Id, out var last);

                return new ConversationListItemDto(
                    c.Id,
                    c.Type.ToString(),
                    title,
                    last?.Text,
                    last?.CreatedAt,
                    c.Members.Select(m => new UserDto(m.UserId, m.User?.Username ?? "")).ToList()
                );
            })
            .OrderByDescending(c => c.LastMessageAt ?? DateTime.MinValue)
            .ToList();
    }

    public async Task<Conversation?> GetByIdAsync(string id) =>
        await _db.Conversations
            .Include(c => c.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == id);
}
