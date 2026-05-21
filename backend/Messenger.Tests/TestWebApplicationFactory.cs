using System.Data.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite; // Переконайся, що є цей using
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Messenger.API.Storage;

namespace Messenger.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("environment", "Testing");

        builder.ConfigureServices(services =>
        {
            // 1. Видаляємо старі реєстрації контексту
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(DbConnection));

            // 2. Створюємо з'єднання з SQLite у пам'яті
            // Важливо: воно має залишатися відкритим протягом всього часу життя тестів
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // 3. Реєструємо DbContext з нашим In-Memory з'єднанням SQLite
            services.AddDbContext<AppDbContext>(opt =>
                opt.UseSqlite(_connection));
        });
    }

    // Закриваємо з'єднання після завершення тестів
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}