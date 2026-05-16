import { useState } from "react";
import type { Message } from "../types";
import { editMessage } from "../services/api";

interface Props {
    message: Message;
    currentUserId: string;
    onEdited: (updated: Message) => void;
}

export function MessageItem({ message, currentUserId, onEdited }: Props) {
    const [editing, setEditing] = useState(false);
    const [draft, setDraft] = useState(message.text);
    const isOwn = message.senderId === currentUserId;

    const handleSave = async () => {
        if (draft.trim() === message.text) {
            setEditing(false);
            return;
        }
        const updated = await editMessage(message.id, currentUserId, draft.trim());
        onEdited(updated);
        setEditing(false);
    };

    return (
        <div className={`message-row ${isOwn ? "own" : "other"}`}>
            {!isOwn && <small className="message-sender">{message.senderName}</small>}

            {editing ? (
                <div className="message-edit">
                    <input
                        value={draft}
                        onChange={(e) => setDraft(e.target.value)}
                        onKeyDown={(e) => e.key === "Enter" && handleSave()}
                        autoFocus
                    />
                    <button type="button" onClick={handleSave}>
                        Save
                    </button>
                    <button type="button" onClick={() => setEditing(false)}>
                        Cancel
                    </button>
                </div>
            ) : (
                <div className="message-bubble-wrap">
                    <span className={`message-bubble ${isOwn ? "own" : "other"}`}>{message.text}</span>
                    {message.isEdited && <span className="message-edited">edited</span>}
                    {isOwn && (
                        <button
                            type="button"
                            className="message-edit-btn"
                            onClick={() => setEditing(true)}
                            title="Edit"
                        >
                            ✏️
                        </button>
                    )}
                </div>
            )}
        </div>
    );
}
