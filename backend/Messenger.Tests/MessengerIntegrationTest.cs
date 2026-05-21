using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Messenger.API.Models;
using Messenger.API.Storage;
using Xunit;

namespace Messenger.Tests
{
    public class MessengerIntegrationTest : IClassFixture<TestWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly TestWebApplicationFactory _factory;
        private const string TestPassword = "secret123";

        public MessengerIntegrationTest(TestWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }

        [Fact]
        public async Task FullFlow_SendAndEditMessage_ReturnsEditedMessage()
        {
            var alice = await RegisterUser("alice");
            var bob = await RegisterUser("bob");

            var conv = await CreateConversation(alice.Id, bob.Id);

            var msg = await SendMessage(conv.Id, alice.Id, "Hello Bob!");

            var editRes = await _client.PatchAsJsonAsync(
                $"/messages/{msg.Id}?requesterId={alice.Id}",
                new { text = "Hello Bob, edited!" });
            editRes.EnsureSuccessStatusCode();

            var historyRes = await _client.GetAsync($"/conversations/{conv.Id}/messages");
            historyRes.EnsureSuccessStatusCode();
            var history = await historyRes.Content.ReadFromJsonAsync<List<MessageDto>>();

            Assert.Single(history!);
            Assert.Equal("Hello Bob, edited!", history![0].Text);
            Assert.True(history[0].IsEdited);
            Assert.NotNull(history[0].EditedAt);
        }

        [Fact]
        public async Task EditMessage_WrongUser_ReturnsForbidden()
        {
            var alice = await RegisterUser("alice2");
            var bob = await RegisterUser("bob2");
            var conv = await CreateConversation(alice.Id, bob.Id);

            var msg = await SendMessage(conv.Id, alice.Id, "Hi");

            var editRes = await _client.PatchAsJsonAsync(
                $"/messages/{msg.Id}?requesterId={bob.Id}",
                new { text = "Hacked!" });

            Assert.Equal(HttpStatusCode.Forbidden, editRes.StatusCode);
        }

        [Fact]
        public async Task Register_DuplicateUsername_ReturnsConflict()
        {
            await RegisterUser("dupe_user");
            var res = await _client.PostAsJsonAsync("/users/register",
                new { username = "dupe_user", password = TestPassword });
            Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        }

        [Fact]
        public async Task Login_WithWrongPassword_ReturnsUnauthorized()
        {
            await RegisterUser("login_user");
            var res = await _client.PostAsJsonAsync("/users/login",
                new { username = "login_user", password = "wrongpass" });
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task Login_ExistingUser_ReturnsUser()
        {
            var registered = await RegisterUser("login_user2");
            var res = await _client.PostAsJsonAsync("/users/login",
                new { username = "login_user2", password = TestPassword });
            res.EnsureSuccessStatusCode();
            var loggedIn = await res.Content.ReadFromJsonAsync<UserDto>();
            Assert.Equal(registered.Id, loggedIn!.Id);
        }

        [Fact]
        public async Task DirectConversation_IsReused_NotDuplicated()
        {
            var alice = await RegisterUser("alice_dedup");
            var bob = await RegisterUser("bob_dedup");

            var conv1 = await CreateConversation(alice.Id, bob.Id);
            var conv2 = await CreateConversation(alice.Id, bob.Id);

            Assert.Equal(conv1.Id, conv2.Id);
        }

        [Fact]
        public async Task UpdateUsername_KeepsSameUserId()
        {
            var user = await RegisterUser("rename_user");
            var res = await _client.PatchAsJsonAsync($"/users/{user.Id}/profile", new
            {
                currentPassword = TestPassword,
                newUsername = "renamed_user"
            });
            res.EnsureSuccessStatusCode();
            var profile = await res.Content.ReadFromJsonAsync<UserProfileDto>();
            Assert.Equal(user.Id, profile!.Id);
            Assert.Equal("renamed_user", profile.Username);
        }

        private async Task<UserDto> RegisterUser(string username)
        {
            var res = await _client.PostAsJsonAsync("/users/register",
                new { username, password = TestPassword });
            res.EnsureSuccessStatusCode();
            return (await res.Content.ReadFromJsonAsync<UserDto>())!;
        }

        private async Task<ConversationDto> CreateConversation(string idA, string idB)
        {
            var res = await _client.PostAsJsonAsync("/conversations", new
            {
                type = 0,
                memberIds = new[] { idA, idB }
            });
            res.EnsureSuccessStatusCode();
            return (await res.Content.ReadFromJsonAsync<ConversationDto>())!;
        }

        private async Task<MessageDto> SendMessage(string convId, string senderId, string text)
        {
            var res = await _client.PostAsJsonAsync("/messages", new
            {
                conversationId = convId,
                senderId,
                text
            });
            res.EnsureSuccessStatusCode();
            return (await res.Content.ReadFromJsonAsync<MessageDto>())!;
        }
    }
}
