import { useEffect, useState } from "react";
import type { User, UserProfile } from "../types";
import { getUserProfile, updateUserProfile } from "../services/api";

interface Props {
    user: User;
    onClose: () => void;
    onLogout: () => void;
    onUserUpdated: (user: User) => void;
}

export function AccountPanel({ user, onClose, onLogout, onUserUpdated }: Props) {
    const [profile, setProfile] = useState<UserProfile | null>(null);
    const [currentPassword, setCurrentPassword] = useState("");
    const [newUsername, setNewUsername] = useState("");
    const [newPassword, setNewPassword] = useState("");
    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        getUserProfile(user.id)
            .then((p) => {
                setProfile(p);
                setNewUsername(p.username);
            })
            .catch(() => setError("Could not load profile"));
    }, [user.id]);

    const handleSave = async () => {
        if (!currentPassword) {
            setError("Enter your current password to save changes");
            return;
        }

        const usernameChanged =
            newUsername.trim() && newUsername.trim().toLowerCase() !== user.username;
        const passwordChanged = newPassword.length > 0;

        if (!usernameChanged && !passwordChanged) {
            setError("Change username or password, or both");
            return;
        }

        setError("");
        setSuccess("");
        setLoading(true);

        try {
            const updated = await updateUserProfile(user.id, {
                currentPassword,
                newUsername: usernameChanged ? newUsername.trim() : undefined,
                newPassword: passwordChanged ? newPassword : undefined,
            });
            setProfile(updated);
            onUserUpdated({ id: updated.id, username: updated.username });
            setCurrentPassword("");
            setNewPassword("");
            setSuccess("Profile updated");
        } catch (e: unknown) {
            const err = e as { response?: { data?: { error?: string } } };
            setError(err?.response?.data?.error ?? "Update failed");
        } finally {
            setLoading(false);
        }
    };

    const formatDate = (iso: string | null) => {
        if (!iso) return "";
        return new Date(iso).toLocaleDateString(undefined, {
            year: "numeric",
            month: "long",
            day: "numeric",
        });
    };

    return (
        <div className="account-overlay" onClick={onClose}>
            <div className="account-panel" onClick={(e) => e.stopPropagation()}>
                <header className="account-panel-header">
                    <h3>Account</h3>
                    <button type="button" className="account-close" onClick={onClose}>
                        ×
                    </button>
                </header>

                <div className="account-avatar">{(profile?.username ?? user.username).charAt(0).toUpperCase()}</div>
                <p className="account-current-user">@{profile?.username ?? user.username}</p>

                <label className="account-label">Current password (required)</label>
                <input
                    className="account-input"
                    type="password"
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    autoComplete="current-password"
                />

                <label className="account-label">New username</label>
                <input
                    className="account-input"
                    value={newUsername}
                    onChange={(e) => setNewUsername(e.target.value)}
                    disabled={profile !== null && !profile.canChangeUsername}
                    spellCheck={false}
                />
                {profile && !profile.canChangeUsername && profile.nextUsernameChangeAt && (
                    <p className="account-hint">
                        Username can be changed again on {formatDate(profile.nextUsernameChangeAt)}
                    </p>
                )}
                {profile?.canChangeUsername && (
                    <p className="account-hint">You can change your username once per month</p>
                )}

                <label className="account-label">New password</label>
                <input
                    className="account-input"
                    type="password"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder="Leave empty to keep current"
                    autoComplete="new-password"
                />

                {error && <p className="account-error">{error}</p>}
                {success && <p className="account-success">{success}</p>}

                <button
                    type="button"
                    className="account-save-btn"
                    onClick={handleSave}
                    disabled={loading}
                >
                    {loading ? "Saving..." : "Save changes"}
                </button>

                <button type="button" className="account-logout-btn" onClick={onLogout}>
                    Log out
                </button>
            </div>
        </div>
    );
}
