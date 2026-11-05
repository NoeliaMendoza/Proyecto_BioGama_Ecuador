using BioGamaEcuador.Data;
using BioGamaEcuador.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class LogEventsTests
{
    [Fact]
    public void Disabled_logging_does_not_emit_an_event()
    {
        var logger = new CaptureLogger { Enabled = false };
        LogEvents.EmailSent(logger, "test@example.invalid");
        Assert.Empty(logger.Events);
    }

    [Fact]
    public void Audit_logging_preserves_structured_boolean_and_user_identity()
    {
        var logger = new CaptureLogger { Enabled = true };
        LogEvents.AuditLogin(logger, "visitor", true, "127.0.0.1");
        var entry = Assert.Single(logger.Events);
        Assert.Equal("visitor", entry["UserId"]);
        Assert.Equal(true, entry["Success"]);
        Assert.Equal("127.0.0.1", entry["IP"]);
    }

    [Fact]
    public async Task Database_audit_is_saved_even_when_console_logging_is_disabled()
    {
        await using var database = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var service = new AuditService(new CaptureLogger { Enabled = false }, database);
        await service.LogLoginAsync("visitor", true);
        var entry = Assert.Single(database.AuditLogs);
        Assert.Equal("visitor", entry.UserId);
        Assert.Equal("LoginSuccess", entry.Action);
    }

    private sealed class CaptureLogger : ILogger<AuditService>
    {
        public bool Enabled { get; init; }
        public List<Dictionary<string, object?>> Events { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => Enabled;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Events.Add(((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary(pair => pair.Key, pair => pair.Value));
        }
    }
}
