import { useEffect, useState } from "react";
import type { User, ConversationListItem } from "../types";
import { searchUsers, getConversations, findOrCreateDirectConversation } from "../services/api";
import { AccountPanel } from "./AccountPanel";

type SidebarMode = "chats" | "search";

interface Props {
    currentUser: User;
    selectedConversationId: string | null;
    onSelectConversation: (item: ConversationListItem) => void;
    onLogout: () => void;
    onUserUpdated: (user: User) => void;
    refreshKey: number;
}

export function Sidebar({
    currentUser,
    selectedConversationId,
    onSelectConversation,
    onLogout,
    onUserUpdated,
    refreshKey,
}: Props) {
    const [mode, setMode] = useState<SidebarMode>("chats");
    const [conversations, setConversations] = useState<ConversationListItem[]>([]);
    const [searchQuery, setSearchQuery] = useState("");
    const [searchResults, setSearchResults] = useState<User[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");
    const [accountOpen, setAccountOpen] = useState(false);

    const loadConversations = () => {
        getConversations(currentUser.id)
            .then(setConversations)
            .catch(() => setConversations([]));
    };

    useEffect(() => {
        loadConversations();
    }, [currentUser.id, refreshKey]);

    useEffect(() => {
        if (mode !== "search") return;
        const q = searchQuery.trim();
        if (q.length < 1) {
            setSearchResults([]);
            return;
        }

        const timer = setTimeout(() => {
            searchUsers(q, currentUser.id)
                .then(setSearchResults)
                .catch(() => setSearchResults([]));
        }, 300);

        return () => clearTimeout(timer);
    }, [searchQuery, mode, currentUser.id]);

    const handleStartChat = async (other: User) => {
        setError("");
        setLoading(true);
        try {
            const conv = await findOrCreateDirectConversation(currentUser.id, other.id);
            const item: ConversationListItem = {
                id: conv.id,
                type: conv.type,
                title: other.username,
                lastMessageText: null,
                lastMessageAt: null,
                members: conv.members,
            };
            loadConversations();
            onSelectConversation(item);
            setMode("chats");
            setSearchQuery("");
        } catch (e: unknown) {
            const err = e as { response?: { data?: { error?: string } } };
            setError(err?.response?.data?.error ?? "Could not start chat");
        } finally {
            setLoading(false);
        }
    };

    const formatTime = (iso: string | null) => {
        if (!iso) return "";
        const d = new Date(iso);
        const now = new Date();
        if (d.toDateString() === now.toDateString()) {
            return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
        }
        return d.toLocaleDateString([], { month: "short", day: "numeric" });
    };

    return (
        <aside className="sidebar">
            <header className="sidebar-header">
                <div className="sidebar-header-top">
                    <button
                        type="button"
                        className={`icon-btn ${mode === "chats" ? "active" : ""}`}
                        title="Chats"
                        onClick={() => setMode("chats")}
                    >
                        💬
                    </button>
                    <button
                        type="button"
                        className={`icon-btn ${mode === "search" ? "active" : ""}`}
                        title="Find user"
                        onClick={() => setMode("search")}
                    >
                        🔍
                    </button>
                </div>

                {mode === "search" && (
                    <input
                        className="sidebar-search"
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        placeholder="Search by username..."
                        autoFocus
                    />
                )}
            </header>

            <div className="sidebar-list">
                {mode === "chats" && (
                    <>
                        {conversations.length === 0 && (
                            <p className="sidebar-empty">
                                No chats yet. Tap 🔍 to find someone.
                            </p>
                        )}
                        {conversations.map((c) => (
                            <button
                                key={c.id}
                                type="button"
                                className={`chat-item ${
                                    selectedConversationId === c.id ? "selected" : ""
                                }`}
                                onClick={() => onSelectConversation(c)}
                            >
                                <div className="chat-item-avatar">
                                    {c.title.charAt(0).toUpperCase()}
                                </div>
                                <div className="chat-item-body">
                                    <div className="chat-item-row">
                                        <span className="chat-item-title">{c.title}</span>
                                        <span className="chat-item-time">
                                            {formatTime(c.lastMessageAt)}
                                        </span>
                                    </div>
                                    <p className="chat-item-preview">
                                        {c.lastMessageText ?? "No messages yet"}
                                    </p>
                                </div>
                            </button>
                        ))}
                    </>
                )}

                {mode === "search" && (
                    <>
                        {searchQuery.trim().length < 1 && (
                            <p className="sidebar-empty">Type a username to search</p>
                        )}
                        {searchResults.map((u) => (
                            <button
                                key={u.id}
                                type="button"
                                className="chat-item"
                                onClick={() => handleStartChat(u)}
                                disabled={loading}
                            >
                                <div className="chat-item-avatar">
                                    {u.username.charAt(0).toUpperCase()}
                                </div>
                                <div className="chat-item-body">
                                    <span className="chat-item-title">@{u.username}</span>
                                    <p className="chat-item-preview">Start chat</p>
                                </div>
                            </button>
                        ))}
                        {searchQuery.trim().length >= 1 && searchResults.length === 0 && (
                            <p className="sidebar-empty">No users found</p>
                        )}
                    </>
                )}

                {error && <p className="sidebar-error">{error}</p>}
            </div>

            <footer className="sidebar-footer">
                <button
                    type="button"
                    className="account-btn"
                    onClick={() => setAccountOpen(true)}
                    title="Account settings"
                >
                    <span className="account-btn-avatar">
                        {currentUser.username.charAt(0).toUpperCase()}
                    </span>
                    <span className="account-btn-label">@{currentUser.username}</span>
                </button>
            </footer>

            {accountOpen && (
                <AccountPanel
                    user={currentUser}
                    onClose={() => setAccountOpen(false)}
                    onLogout={() => {
                        setAccountOpen(false);
                        onLogout();
                    }}
                    onUserUpdated={(u) => {
                        onUserUpdated(u);
                        loadConversations();
                    }}
                />
            )}
        </aside>
    );
}
