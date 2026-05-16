namespace Messenger.API.Models;

public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConversationId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Message editing fields
    public bool IsEdited { get; set; } = false;
    public DateTime? EditedAt { get; set; }

    public User? Sender { get; set; }
}
