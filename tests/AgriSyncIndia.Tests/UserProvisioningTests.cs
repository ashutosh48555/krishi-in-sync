using System.Security.Claims;
using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using AgriSyncIndia.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgriSyncIndia.Tests;

public class UserProvisioningTests : IDisposable
{
    readonly SqliteConnection _conn = new("Data Source=:memory:");
    readonly AgriSyncDbContext _db;

    public UserProvisioningTests()
    {
        _conn.Open();
        _db = new AgriSyncDbContext(new DbContextOptionsBuilder<AgriSyncDbContext>().UseSqlite(_conn).Options);
        _db.Database.EnsureCreated();
    }

    public void Dispose() { _db.Dispose(); _conn.Dispose(); }

    UserProvisioning Sut() => new(_db, new HttpClient(), new ClerkOptions());

    sealed class Stub(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c) =>
            Task.FromResult(new HttpResponseMessage { Content = new StringContent(json) });
    }

    UserProvisioning WithBackend(string status, string email = "old@farm.in") => new(_db,
        new HttpClient(new Stub("{\"primary_email_address_id\":\"e1\",\"first_name\":\"O\",\"last_name\":\"L\",\"email_addresses\":[{\"id\":\"e1\",\"email_address\":\"" + email + "\",\"verification\":{\"status\":\"" + status + "\"}}]}")),
        new ClerkOptions { SecretKey = "test-secret" });

    async Task SeedOld() { _db.Users.Add(new User { FullName = "Old", Email = "old@farm.in", PasswordHash = "x", Role = "Farm Manager" }); await _db.SaveChangesAsync(); }

    [Fact]
    public async Task New_user_is_inserted()
    {
        var r = await Sut().ProvisionAsync(new ClerkIdentity("user_1", "New@Farm.in", "Nina"));
        Assert.True(r.Succeeded);
        var u = await _db.Users.SingleAsync();
        Assert.Equal(("Nina", "new@farm.in", "Viewer", "", "user_1"),
            (u.FullName, u.Email, u.Role, u.PasswordHash, u.ClerkUserId));
    }

    [Fact]
    public async Task Unverified_email_on_existing_row_is_rejected_and_not_linked()
    {
        await SeedOld();
        var r = await Sut().ProvisionAsync(new ClerkIdentity("user_2", "old@farm.in", "Old"));
        Assert.False(r.Succeeded);
        Assert.Equal("This email belongs to an existing account. Use the demo login or ask an admin.", r.Error);
        var u = await _db.Users.SingleAsync();
        Assert.Null(u.ClerkUserId);
    }

    [Fact]
    public async Task Token_email_verified_claim_links_existing_row()
    {
        await SeedOld();
        var r = await Sut().ProvisionAsync(new ClerkIdentity("user_2", "OLD@farm.in", "Old", true));
        Assert.True(r.Succeeded);
        var u = await _db.Users.SingleAsync();
        Assert.Equal(("user_2", "Farm Manager"), (u.ClerkUserId, u.Role));
    }

    [Fact]
    public async Task Backend_api_verified_email_links_existing_row()
    {
        await SeedOld();
        Assert.True((await WithBackend("verified").ProvisionAsync(new ClerkIdentity("user_2", "old@farm.in", "Old"))).Succeeded);
        Assert.Equal("user_2", (await _db.Users.SingleAsync()).ClerkUserId);
    }

    [Fact]
    public async Task Backend_api_unverified_email_is_rejected()
    {
        await SeedOld();
        Assert.False((await WithBackend("unverified").ProvisionAsync(new ClerkIdentity("user_2", "old@farm.in", "Old"))).Succeeded);
        Assert.Null((await _db.Users.SingleAsync()).ClerkUserId);
    }

    [Fact]
    public async Task Backend_api_different_email_is_rejected()
    {
        await SeedOld();
        Assert.False((await WithBackend("verified", "other@farm.in").ProvisionAsync(new ClerkIdentity("user_2", "old@farm.in", "Old"))).Succeeded);
    }

    [Fact]
    public async Task Email_already_linked_to_other_clerk_user_is_rejected()
    {
        _db.Users.Add(new User { FullName = "A", Email = "a@farm.in", PasswordHash = "", ClerkUserId = "user_A" });
        await _db.SaveChangesAsync();
        Assert.False((await Sut().ProvisionAsync(new ClerkIdentity("user_B", "a@farm.in", "B"))).Succeeded);
    }

    [Fact]
    public async Task Known_clerk_id_is_found()
    {
        _db.Users.Add(new User { FullName = "A", Email = "a@farm.in", PasswordHash = "", ClerkUserId = "user_A" });
        await _db.SaveChangesAsync();
        var r = await Sut().ProvisionAsync(new ClerkIdentity("user_A", null, null));
        Assert.True(r.Succeeded);
        Assert.Equal(1, await _db.Users.CountAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task Empty_email_without_secret_is_rejected(string? email)
    {
        var r = await Sut().ProvisionAsync(new ClerkIdentity("user_3", email, "X"));
        Assert.False(r.Succeeded);
        Assert.Equal(0, await _db.Users.CountAsync());
    }

    [Fact]
    public void Principal_has_claims_layout_reads()
    {
        var p = UserProvisioning.BuildPrincipal(new User { UserId = 7, FullName = "N", Email = "n@f.in", Role = "Agronomist" });
        Assert.Equal("N", p.Identity!.Name);
        Assert.Equal("Agronomist", p.FindFirst(ClaimTypes.Role)!.Value);
        Assert.Equal("n@f.in", p.FindFirst(ClaimTypes.Email)!.Value);
        Assert.Equal("All Farm Clusters", p.FindFirst("AffiliatedCluster")!.Value);
    }
}
