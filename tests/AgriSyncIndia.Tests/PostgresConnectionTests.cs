using Xunit;
using AgriSyncIndia.Services;
using Npgsql;

namespace AgriSyncIndia.Tests;

public class PostgresConnectionTests
{
    [Fact]
    public void Uri_is_converted_to_npgsql_keywords()
    {
        var cs = PostgresConnection.FromEnvironmentValue("postgresql://u:p%40ss@db.example.com/app?sslmode=require");
        var b = new NpgsqlConnectionStringBuilder(cs);
        Assert.Equal("db.example.com", b.Host);
        Assert.Equal("app", b.Database);
        Assert.Equal("u", b.Username);
        Assert.Equal("p@ss", b.Password);
        Assert.Equal(SslMode.Require, b.SslMode);
    }

    [Fact]
    public void Keyword_string_passes_through()
    {
        const string raw = "Host=h;Database=d;Username=u;Password=p";
        Assert.Equal(raw, PostgresConnection.FromEnvironmentValue(raw));
    }

    [Fact]
    public void Blank_returns_empty()
    {
        Assert.Equal(string.Empty, PostgresConnection.FromEnvironmentValue(""));
    }
}
