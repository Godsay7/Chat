import { useState } from "react";
import { registerUser, loginUser } from "../services/api";
import type { User } from "../types";

interface Props {
    onAuthenticated: (user: User) => void;
}

export function AuthScreen({ onAuthenticated }: Props) {
    const [mode, setMode] = useState<"login" | "register">("login");
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);

    const handleSubmit = async () => {
        const user = username.trim();
        if (!user || !password) return;

        setError("");
        setLoading(true);
        try {
            const account =
                mode === "register"
                    ? await registerUser(user, password)
                    : await loginUser(user, password);
            onAuthenticated(account);
        } catch (e: unknown) {
            const err = e as { response?: { data?: { error?: string } }; message?: string };
            setError(
                err?.response?.data?.error ??
                err?.message ??
                (mode === "login" ? "Login failed" : "Registration failed")
            );
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="auth-screen">
            <div className="auth-card">
                <h1>Messenger</h1>
                <p className="auth-subtitle">
                    {mode === "login"
                        ? "Sign in with your username and password"
                        : "Create an account with a unique username"}
                </p>

                <div className="auth-tabs">
                    <button
                        type="button"
                        className={mode === "login" ? "active" : ""}
                        onClick={() => {
                            setMode("login");
                            setError("");
                        }}
                    >
                        Log in
                    </button>
                    <button
                        type="button"
                        className={mode === "register" ? "active" : ""}
                        onClick={() => {
                            setMode("register");
                            setError("");
                        }}
                    >
                        Sign up
                    </button>
                </div>

                <input
                    className="auth-input"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    placeholder="username"
                    autoComplete="username"
                    spellCheck={false}
                />
                <input
                    className="auth-input"
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    onKeyDown={(e) => e.key === "Enter" && handleSubmit()}
                    placeholder="password"
                    autoComplete={mode === "login" ? "current-password" : "new-password"}
                />
                <p className="auth-hint">
                    Username: 3–32 chars (letters, numbers, underscore). Password: min 6 chars.
                </p>

                <button
                    type="button"
                    className="auth-submit"
                    onClick={handleSubmit}
                    disabled={loading || !username.trim() || !password}
                >
                    {loading ? "..." : mode === "login" ? "Log in" : "Create account"}
                </button>

                {error && <p className="auth-error">{error}</p>}
            </div>
        </div>
    );
}
