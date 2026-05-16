import axios from "axios";
import type { User, UserProfile, Message, Conversation, ConversationListItem } from "../types";

const api = axios.create({ baseURL: "http://localhost:5000" });

export const registerUser = (username: string, password: string) =>
    api.post<User>("/users/register", { username, password }).then((r) => r.data);

export const loginUser = (username: string, password: string) =>
    api.post<User>("/users/login", { username, password }).then((r) => r.data);

export const getUserById = (id: string) =>
    api.get<User>(`/users/${id}`).then((r) => r.data);

export const getUserProfile = (id: string) =>
    api.get<UserProfile>(`/users/${id}/profile`).then((r) => r.data);

export const updateUserProfile = (
    id: string,
    data: { currentPassword: string; newUsername?: string; newPassword?: string }
) => api.patch<UserProfile>(`/users/${id}/profile`, data).then((r) => r.data);

export const searchUsers = (query: string, excludeUserId: string) =>
    api
        .get<User[]>("/users/search", { params: { q: query, excludeUserId } })
        .then((r) => r.data);

export const getConversations = (userId: string) =>
    api.get<ConversationListItem[]>(`/users/${userId}/conversations`).then((r) => r.data);

export const findOrCreateDirectConversation = (userId: string, otherUserId: string) =>
    api
        .post<Conversation>("/conversations/direct", { userId, otherUserId })
        .then((r) => r.data);

export const sendMessage = (conversationId: string, senderId: string, text: string) =>
    api.post<Message>("/messages", { conversationId, senderId, text }).then((r) => r.data);

export const editMessage = (messageId: string, requesterId: string, text: string) =>
    api
        .patch<Message>(`/messages/${messageId}?requesterId=${requesterId}`, { text })
        .then((r) => r.data);

export const getMessages = (conversationId: string) =>
    api.get<Message[]>(`/conversations/${conversationId}/messages`).then((r) => r.data);
