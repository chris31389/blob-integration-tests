# Integration Testing in Containers

## TL;DR

[Here is an example](https://github.com/chris31389/redis-integration-tests) of using Testcontainers to spin up local throwaway instances of a Redis container and targeting it when running Integration tests.

## Introduction

This blog post aims to guide you through an alternative way of running Integration Tests within your project.  We look to make use of [Testcontainers](https://github.com/testcontainers/testcontainers-dotnet) which support tests with throwaway instances of Docker containers.  A list of the pre-configured containers can he found [here](https://github.com/testcontainers/testcontainers-dotnet#pre-configured-containers).  This means we can run tests without going to a deployed instance of the service we look to integrate with, saving time on round-trip-times.  All code talked about in this post will be [available here](https://github.com/chris31389/redis-integration-tests)

For the purposes of our project, an Integration Test is defined as an automated test written to test the interaction between code that we have written with a third party service.  The goal is to prove that the code can communicate as expected with the third party service so that we can identify any defects before it reaches a later stage of the development cycle.

We will use Docker containers.  To follow along, you will need to have Docker (or equivalent) set up and running.  Testcontainers will spin up container instances which will be used to execute tests against.  Each test will have a dedicated container instance so that tests can be run in parallel without affecting other tests.

By using a container, we can execute tests locally without depending on the third party service to host an instance and we can execute each test against a new instance of the third party service without having to set up anything locally.

## Creating an Integration Test

### The class under test

For this example, we will test a class that will integrate with Redis.  I've created two projects, one to contain the class that will be tested and another to execute tests.

I've created a class called `Cache`

``` csharp
// https://github.com/chris31389/redis-integration-tests/blob/main/RedisExample/Cache.cs
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
```

### Writing the test

The test we want to execute needs to interact with the instance of redis.  We need each test to:

1. Spin up an instance of the redis container
2. Create an instance of the Cache with a link to the instance of redis
3. Setup the test (optional)
4. Execute the test
5. Assert whether the test passed
6. Dispose of the instance of the redis container

The DotNet NuGet `Testcontainers` (currently version 2.20) gives us the ability to spin up container instances per test.  For Redis there is a pre-configured `RedisTestcontainer` we can use to provide us an instance of redis within a container.

`CacheTests`
``` csharp
// https://github.com/chris31389/redis-integration-tests/blob/main/RedisExample.IntegrationTests/CacheTests.cs
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using StackExchange.Redis;

namespace RedisExample.IntegrationTests;

public class CacheTests : IAsyncLifetime
{
    // The class that will create instances of redis based of the configuration we pass in
    private readonly TestcontainerDatabase _testcontainers = new TestcontainersBuilder<RedisTestcontainer>()
        .WithDatabase(new RedisTestcontainerConfiguration())
        .Build();

    private IConnectionMultiplexer? _connectionMultiplexer;

    [Fact]
    public async Task GivenKeyValuePair_WhenICache_ThenItIsPersisted()
    {
        // Arrange
        // 2. Create an instance of the Cache with a link to the instance of redis
        var cache = new Cache(_connectionMultiplexer!);

        // Act
        // 4. Execute the test
        await cache.SetAsync("myKey", "myValue");

        // Assert
        var database = _connectionMultiplexer!.GetDatabase();
        var actualValue = await database.StringGetAsync("myKey");
        // 5. Assert whether the test passed
        actualValue.Should().Be("myValue");
    }

    // This is executed before each test is executed
    public async Task InitializeAsync()
    {
        // 1. Spin up an instance of the redis container
        await _testcontainers.StartAsync();

        // Setup a link to the instance of redis
        _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(_testcontainers.ConnectionString);
    }

    // This is executed after each test is executed
    public async Task DisposeAsync() 
    {
        // 6. Dispose of the instance of the redis container
        await _testcontainers.DisposeAsync().AsTask();
    } 
}
```

## Conclusion

This blog post has shown that we can execute integration tests locally which target a Docker container.  Anyone with Docker installed and running can clone this repository, build and test it.  They are not required to install or provision an instance of Redis to execute these tests.  This should help speed up and improve the development process.

There are limitations:

- Not all third party services can be containerised (e.g. Azure Service Bus)  
- Not every third party service has a pre-made setup class, which means there will need be a Testcontainer class written for the missing service.

This repository is actively being developed and there are already people developing a cosmos and azurite Testcontainer setup class.  As these get introduced, it will be quicker and easier to get started with these services.

## Useful links

- [My example using Redis](https://github.com/chris31389/redis-integration-tests)
- [Docker](https://www.docker.com/get-started/)
- [Testcontainers](https://github.com/testcontainers/testcontainers-dotnet)