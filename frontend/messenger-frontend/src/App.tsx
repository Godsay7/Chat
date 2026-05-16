import { useEffect, useState } from "react";
import type { User, ConversationListItem } from "./types";
import { getUserById } from "./services/api";
import { loadSession, saveSession, clearSession } from "./services/session";
import { AuthScreen } from "./components/AuthScreen";
import { Sidebar } from "./components/Sidebar";
import { Chat } from "./components/Chat";
import "./messenger.css";

export default function App() {
    const [currentUser, setCurrentUser] = useState<User | null>(null);
    const [bootstrapping, setBootstrapping] = useState(true);
    const [selectedChat, setSelectedChat] = useState<ConversationListItem | null>(null);
    const [refreshKey, setRefreshKey] = useState(0);

    useEffect(() => {
        const stored = loadSession();
        if (!stored) {
            setBootstrapping(false);
            return;
        }

        getUserById(stored.id)
            .then((user) => {
                setCurrentUser(user);
                saveSession(user);
            })
            .catch(() => clearSession())
            .finally(() => setBootstrapping(false));
    }, []);

    const handleAuthenticated = (user: User) => {
        saveSession(user);
        setCurrentUser(user);
    };

    const handleUserUpdated = (user: User) => {
        saveSession(user);
        setCurrentUser(user);
        setRefreshKey((k) => k + 1);
    };

    const handleLogout = () => {
        clearSession();
        setCurrentUser(null);
        setSelectedChat(null);
    };

    if (bootstrapping) {
        return <div className="auth-screen">Loading...</div>;
    }

    if (!currentUser) {
        return <AuthScreen onAuthenticated={handleAuthenticated} />;
    }

    return (
        <div className="messenger-app">
            <Sidebar
                currentUser={currentUser}
                selectedConversationId={selectedChat?.id ?? null}
                onSelectConversation={setSelectedChat}
                onLogout={handleLogout}
                onUserUpdated={handleUserUpdated}
                refreshKey={refreshKey}
            />

            <main className="chat-panel">
                {selectedChat ? (
                    <>
                        <header className="chat-panel-header">
                            <h2>{selectedChat.title}</h2>
                        </header>
                        <Chat
                            conversationId={selectedChat.id}
                            currentUser={currentUser}
                            onMessageSent={() => setRefreshKey((k) => k + 1)}
                        />
                    </>
                ) : (
                    <div className="chat-panel-empty">
                        <p>Select a chat or search for a user to start messaging</p>
                    </div>
                )}
            </main>
        </div>
    );
}
