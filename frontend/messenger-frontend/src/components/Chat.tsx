import { useState, useEffect, useRef } from "react";
import type { Message, User } from "../types";
import { getMessages, sendMessage } from "../services/api";
import { MessageItem } from "./MessageItem";

interface Props {
    conversationId: string;
    currentUser: User;
    onMessageSent?: () => void;
}

export function Chat({ conversationId, currentUser, onMessageSent }: Props) {
    const [messages, setMessages] = useState<Message[]>([]);
    const [text, setText] = useState("");
    const bottomRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const load = () => getMessages(conversationId).then(setMessages);
        load();
        const interval = setInterval(load, 3000);
        return () => clearInterval(interval);
    }, [conversationId]);

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: "smooth" });
    }, [messages]);

    const handleSend = async () => {
        if (!text.trim()) return;
        const msg = await sendMessage(conversationId, currentUser.id, text.trim());
        setMessages((prev) => [...prev, msg]);
        setText("");
        onMessageSent?.();
    };

    const handleEdited = (updated: Message) => {
        setMessages((prev) => prev.map((m) => (m.id === updated.id ? updated : m)));
    };

    return (
        <div className="chat-view">
            <div className="chat-messages">
                {messages.map((m) => (
                    <MessageItem
                        key={m.id}
                        message={m}
                        currentUserId={currentUser.id}
                        onEdited={handleEdited}
                    />
                ))}
                <div ref={bottomRef} />
            </div>

            <div className="chat-input-bar">
                <input
                    className="chat-input"
                    value={text}
                    onChange={(e) => setText(e.target.value)}
                    onKeyDown={(e) => e.key === "Enter" && handleSend()}
                    placeholder="Type a message..."
                />
                <button type="button" className="chat-send-btn" onClick={handleSend}>
                    Send
                </button>
            </div>
        </div>
    );
}
