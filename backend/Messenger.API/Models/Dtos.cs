namespace Messenger.API.Models;

public record AuthRequest(string Username, string Password);

public record UpdateProfileRequest(
    string CurrentPassword,
    string? NewUsername,
    string? NewPassword
);

public record CreateConversationRequest(
    ConversationType Type,
    List<string> MemberIds
);

public record SendMessageRequest(
    string ConversationId,
    string SenderId,
    string Text
);

public record EditMessageRequest(string Text);

public record UserDto(string Id, string Username);

public record UserProfileDto(
    string Id,
    string Username,
    bool CanChangeUsername,
    DateTime? NextUsernameChangeAt
);

public record MessageDto(
    string Id,
    string ConversationId,
    string SenderId,
    string SenderName,
    string Text,
    DateTime CreatedAt,
    bool IsEdited,
    DateTime? EditedAt
);

public record ConversationDto(
    string Id,
    string Type,
    List<UserDto> Members
);

public record ConversationListItemDto(
    string Id,
    string Type,
    string Title,
    string? LastMessageText,
    DateTime? LastMessageAt,
    List<UserDto> Members
);
