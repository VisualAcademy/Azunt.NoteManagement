using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Azunt.NoteManagement;

public class NoteDbContextFactory
{
    private readonly IConfiguration? _configuration;

    public NoteDbContextFactory() { }

    public NoteDbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public NoteDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<NoteDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new NoteDbContext(options);
    }

    public NoteDbContext CreateDbContext(DbContextOptions<NoteDbContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new NoteDbContext(options);
    }

    public NoteDbContext CreateDbContext()
    {
        if (_configuration == null)
        {
            throw new InvalidOperationException("Configuration is not provided.");
        }

        var defaultConnection = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("DefaultConnection is not configured properly.");
        }

        return CreateDbContext(defaultConnection);
    }
}