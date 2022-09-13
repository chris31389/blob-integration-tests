using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using StackExchange.Redis;


namespace RedisExample.IntegrationTests;

public class CacheTests : IAsyncLifetime
{
    private readonly TestcontainerDatabase _testcontainers = new TestcontainersBuilder<RedisTestcontainer>()
        .WithDatabase(new RedisTestcontainerConfiguration())
        .Build();

    private IConnectionMultiplexer? _connectionMultiplexer;

    [Fact]
    public async Task GivenKeyValuePair_WhenICache_ThenItIsPersisted()
    {
        // Arrange
        var cache = new Cache(_connectionMultiplexer!);

        // Act
        await cache.SetAsync("myKey", "myValue");

        // Assert
        var database = _connectionMultiplexer!.GetDatabase();
        var actualValue = await database.StringGetAsync("myKey");
        actualValue.Should().Be("myValue");
    }

    public async Task InitializeAsync()
    {
        await _testcontainers.StartAsync();
        _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(_testcontainers.ConnectionString);
    }

    public async Task DisposeAsync() => await _testcontainers.DisposeAsync().AsTask();
}