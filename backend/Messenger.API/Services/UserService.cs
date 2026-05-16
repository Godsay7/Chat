using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Messenger.API.Models;
using Messenger.API.Storage;

namespace Messenger.API.Services;

public class UserService
{
    private static readonly Regex UsernamePattern = new(@"^[a-zA-Z0-9_]{3,32}$", RegexOptions.Compiled);
    private static readonly TimeSpan UsernameChangeCooldown = TimeSpan.FromDays(30);

    private readonly AppDbContext _db;

    public UserService(AppDbContext db) => _db = db;

    public static string NormalizeUsername(string username) =>
        username.Trim().ToLowerInvariant();

    public static void ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty.");
        if (!UsernamePattern.IsMatch(username.Trim()))
            throw new ArgumentException("Username must be 3–32 characters: letters, numbers, underscore only.");
    }

    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.");
        if (password.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters.");
    }

    public static UserProfileDto ToProfileDto(User user)
    {
        var canChange = CanChangeUsername(user);
        DateTime? nextChange = null;
        if (!canChange && user.UsernameChangedAt.HasValue)
            nextChange = user.UsernameChangedAt.Value.Add(UsernameChangeCooldown);

        return new UserProfileDto(user.Id, user.Username, canChange, nextChange);
    }

    public static bool CanChangeUsername(User user) =>
        user.UsernameChangedAt is null ||
        DateTime.UtcNow - user.UsernameChangedAt.Value >= UsernameChangeCooldown;

    public async Task<User> RegisterAsync(string username, string password)
    {
        ValidateUsername(username);
        ValidatePassword(password);
        var normalized = NormalizeUsername(username);

        if (await _db.Users.AnyAsync(u => u.Username == normalized))
            throw new InvalidOperationException("Username is already taken.");

        var user = new User
        {
            Username = normalized,
            PasswordHash = PasswordHasher.Hash(password)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<User> LoginAsync(string username, string password)
    {
        ValidateUsername(username);
        ValidatePassword(password);
        var normalized = NormalizeUsername(username);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == normalized)
            ?? throw new UnauthorizedAccessException("Invalid username or password.");

        if (string.IsNullOrEmpty(user.PasswordHash))
            throw new UnauthorizedAccessException(
                "This account has no password. Please register a new account.");

        if (!PasswordHasher.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        return user;
    }

    public async Task<User> UpdateProfileAsync(
        string userId,
        string currentPassword,
        string? newUsername,
        string? newPassword)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            !PasswordHasher.Verify(currentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        var hasUsernameChange = !string.IsNullOrWhiteSpace(newUsername);
        var hasPasswordChange = !string.IsNullOrWhiteSpace(newPassword);

        if (!hasUsernameChange && !hasPasswordChange)
            throw new ArgumentException("Nothing to update.");

        if (hasUsernameChange)
        {
            ValidateUsername(newUsername!);
            var normalized = NormalizeUsername(newUsername!);

            if (normalized != user.Username)
            {
                if (!CanChangeUsername(user))
                {
                    var next = user.UsernameChangedAt!.Value.Add(UsernameChangeCooldown);
                    throw new InvalidOperationException(
                        $"Username can only be changed once per month. Next change available on {next:yyyy-MM-dd}.");
                }

                if (await _db.Users.AnyAsync(u => u.Username == normalized && u.Id != userId))
                    throw new InvalidOperationException("Username is already taken.");

                user.Username = normalized;
                user.UsernameChangedAt = DateTime.UtcNow;
            }
        }

        if (hasPasswordChange)
        {
            ValidatePassword(newPassword!);
            user.PasswordHash = PasswordHasher.Hash(newPassword!);
        }

        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<List<User>> SearchAsync(string query, string? excludeUserId = null)
    {
        var term = query.Trim().ToLowerInvariant();
        if (term.Length < 1)
            return new List<User>();

        var q = _db.Users.Where(u => u.Username.Contains(term));
        if (!string.IsNullOrEmpty(excludeUserId))
            q = q.Where(u => u.Id != excludeUserId);

        return await q.OrderBy(u => u.Username).Take(20).ToListAsync();
    }

    public async Task<User?> GetByIdAsync(string id) =>
        await _db.Users.FindAsync(id);
}
