export interface User {
    id: string;
    username: string;
}

export interface UserProfile {
    id: string;
    username: string;
    canChangeUsername: boolean;
    nextUsernameChangeAt: string | null;
}

export interface Message {
    id: string;
    conversationId: string;
    senderId: string;
    senderName: string;
    text: string;
    createdAt: string;
    isEdited: boolean;
    editedAt: string | null;
}

export interface Conversation {
    id: string;
    type: string;
    members: User[];
}

export interface ConversationListItem {
    id: string;
    type: string;
    title: string;
    lastMessageText: string | null;
    lastMessageAt: string | null;
    members: User[];
}
