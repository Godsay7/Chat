namespace Messenger.API.Models;

public enum ConversationType { Direct, Group }

public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ConversationType Type { get; set; } = ConversationType.Direct;
    public List<ConversationMember> Members { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
}

public class ConversationMember
{
    public int Id { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }
}
