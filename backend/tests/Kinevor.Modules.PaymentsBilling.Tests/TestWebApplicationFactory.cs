using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Kinevor.Modules.PaymentsBilling.Tests;

/// <summary>Hosts the demo API against a unique temporary SQLite database.</summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"kinevor-payments-{Guid.NewGuid():N}.db");

    /// <summary>Points the host at the temporary database.</summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:PaymentsBilling", $"Data Source={_databasePath}");
        builder.UseEnvironment("Development");
    }

    /// <summary>Clears pooled connections before deleting the temporary database.</summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();

        try
        {
            if (File.Exists(_databasePath))
                File.Delete(_databasePath);
        }
        catch (IOException)
        {
            // A finalizer still holding the file must not fail the test run.
        }
    }
}
