using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Messenger.API.Storage;

public static class DbInitializer
{
    public static void Initialize(AppDbContext db, IHostEnvironment env)
    {
        try
        {
            db.Database.EnsureCreated();
            if (db.Database.GetDbConnection() is SqliteConnection)
                MigrateLegacySchema(db);
        }
        catch (Exception ex) when (env.IsDevelopment() && db.Database.GetDbConnection() is SqliteConnection)
        {
            Console.WriteLine($"Database init failed ({ex.Message}). Recreating messenger.db...");
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }
    }

    private static void MigrateLegacySchema(AppDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        connection.Open();

        try
        {
            var columns = GetColumnNames(connection, "Users");
            if (columns.Count == 0)
                return;

            if (columns.Contains("Name") && !columns.Contains("Username"))
            {
                Execute(connection, """ALTER TABLE "Users" RENAME COLUMN "Name" TO "Username";""");
            }

            DeduplicateUsernames(connection);

            if (!IndexExists(connection, "IX_Users_Username"))
            {
                Execute(connection, """CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");""");
            }

            columns = GetColumnNames(connection, "Users");
            if (!columns.Contains("PasswordHash"))
            {
                Execute(connection, """ALTER TABLE "Users" ADD COLUMN "PasswordHash" TEXT NOT NULL DEFAULT '';""");
            }

            columns = GetColumnNames(connection, "Users");
            if (!columns.Contains("UsernameChangedAt"))
            {
                Execute(connection, """ALTER TABLE "Users" ADD COLUMN "UsernameChangedAt" TEXT NULL;""");
            }
        }
        finally
        {
            connection.Close();
        }
    }

    private static void DeduplicateUsernames(System.Data.Common.DbConnection connection)
    {
        using var find = connection.CreateCommand();
        find.CommandText = """
            SELECT "Username", GROUP_CONCAT("Id")
            FROM "Users"
            GROUP BY "Username"
            HAVING COUNT(*) > 1
            """;

        using var reader = find.ExecuteReader();
        var updates = new List<(string id, string username)>();

        while (reader.Read())
        {
            var baseName = reader.GetString(0);
            var ids = reader.GetString(1).Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 1; i < ids.Length; i++)
            {
                var suffix = ids[i][..Math.Min(6, ids[i].Length)];
                updates.Add((ids[i], $"{baseName}_{suffix}"));
            }
        }

        reader.Close();

        foreach (var (id, username) in updates)
        {
            using var update = connection.CreateCommand();
            update.CommandText = """UPDATE "Users" SET "Username" = $username WHERE "Id" = $id;""";
            update.Parameters.Add(new SqliteParameter("$username", username));
            update.Parameters.Add(new SqliteParameter("$id", id));
            update.ExecuteNonQuery();
        }
    }

    private static List<string> GetColumnNames(System.Data.Common.DbConnection connection, string table)
    {
        var columns = new List<string>();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{table}\");";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            columns.Add(reader.GetString(1));
        return columns;
    }

    private static bool IndexExists(System.Data.Common.DbConnection connection, string indexName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'index' AND name = $name;";
        cmd.Parameters.Add(new SqliteParameter("$name", indexName));
        return cmd.ExecuteScalar() is not null;
    }

    private static void Execute(System.Data.Common.DbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
