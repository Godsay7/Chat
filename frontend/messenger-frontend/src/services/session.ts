import type { User } from "../types";

const STORAGE_KEY = "messenger_user";

export function saveSession(user: User): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
}

export function loadSession(): User | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
        return JSON.parse(raw) as User;
    } catch {
        return null;
    }
}

export function clearSession(): void {
    localStorage.removeItem(STORAGE_KEY);
}
