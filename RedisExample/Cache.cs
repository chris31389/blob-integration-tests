using StackExchange.Redis;

namespace RedisExample;

public class Cache
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public Cache(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task SetAsync(string key, string value)
    {
        var database = _connectionMultiplexer.GetDatabase();
        await database.StringSetAsync(key, value);
    }
}