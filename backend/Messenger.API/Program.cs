using Microsoft.EntityFrameworkCore;
using Messenger.API.Storage;
using Messenger.API.Services;

var builder = WebApplication.CreateBuilder(args);

var useInMemoryDb = builder.Configuration["Database:Provider"] == "InMemory";

if (useInMemoryDb)
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
    {
        opt.UseInMemoryDatabase("Messenger_TestDb_" + Guid.NewGuid());
        opt.EnableServiceProviderCaching(false);
    });
}
else
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlite("Data Source=messenger.db"));
}

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<MessageService>();

builder.Services.AddControllers();

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p =>
        p.WithOrigins("http://localhost:5173")
         .AllowAnyMethod()
         .AllowAnyHeader()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

    if (!env.IsEnvironment("Testing")) // <-- обгорнути в цю перевірку
    {
        if (useInMemoryDb)
            db.Database.EnsureCreated();
        else
            DbInitializer.Initialize(db, env);
    }
}

app.UseCors();
app.MapControllers();
app.Run();

// Needed for integration test project access
public partial class Program { }