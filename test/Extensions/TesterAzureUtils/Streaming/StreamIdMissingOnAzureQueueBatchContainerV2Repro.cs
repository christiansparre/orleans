using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Providers.Streams.AzureQueue;
using Orleans.Runtime;
using Orleans.Serialization;
using TestExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Tester.AzureUtils.Streaming;

[Collection(TestEnvironmentFixture.DefaultCollection)]
public class StreamIdMissingOnAzureQueueBatchContainerV2Repro
{
    private readonly TestEnvironmentFixture _fixture;
    private readonly ITestOutputHelper _testOutputHelper;

    [GenerateSerializer]
    public record TestEvent(Guid Id) : TestEventBase(Id);

    [GenerateSerializer]
    public record TestEventBase([property: Id(1)] Guid Id);

    [GenerateSerializer]
    public record AnotherTestEvent([property: Id(1)] string Foo, Guid Id) : TestEventBase(Id);

    public StreamIdMissingOnAzureQueueBatchContainerV2Repro(TestEnvironmentFixture fixture, ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Container_With_TestEventBase()
    {
        // This work as expected, and the StreamId is on the AzureQueueBatchContainerV2 instance

        var serializer = _fixture.Services.GetService<Serializer>().GetSerializer<AzureQueueBatchContainerV2>();

        var streamId = StreamId.Create("ns", "test");

        var containerWithTestEventBase = new AzureQueueBatchContainerV2(streamId, new List<object> { new TestEventBase(Guid.NewGuid()) }, new Dictionary<string, object>());

        var deserializeContainerWithTestEventBase = serializer.Deserialize(serializer.SerializeToArray(containerWithTestEventBase));

        _testOutputHelper.WriteLine(deserializeContainerWithTestEventBase.StreamId);
    }

    [Fact]
    public void Container_With_TestEvent()
    {
        // This does not work, for some reason the StreamId is missing from AzureQueueBatchContainerV2
        // when the "event" inherits from a base type and does not itself have any properties

        var serializer = _fixture.Services.GetService<Serializer>().GetSerializer<AzureQueueBatchContainerV2>();

        var streamId = StreamId.Create("ns", "test");

        var containerWithTestEvent = new AzureQueueBatchContainerV2(streamId, new List<object> { new TestEvent(Guid.NewGuid()) }, new Dictionary<string, object>());

        var deserializeContainerWithTestEvent = serializer.Deserialize(serializer.SerializeToArray(containerWithTestEvent));

        _testOutputHelper.WriteLine(deserializeContainerWithTestEvent.StreamId);
    }

    [Fact]
    public void Container_With_AnotherTestEvent()
    {
        // This work as expected, and the StreamId is on the AzureQueueBatchContainerV2 instance

        var serializer = _fixture.Services.GetService<Serializer>().GetSerializer<AzureQueueBatchContainerV2>();

        var streamId = StreamId.Create("ns", "test");

        var containerWithAnotherTestEvent = new AzureQueueBatchContainerV2(streamId, new List<object> { new AnotherTestEvent("Foo", Guid.NewGuid()) }, new Dictionary<string, object>());

        var deserializeContainerWithAnotherTestEvent = serializer.Deserialize(serializer.SerializeToArray(containerWithAnotherTestEvent));

        _testOutputHelper.WriteLine(deserializeContainerWithAnotherTestEvent.StreamId);
    }

    [Fact]
    public void Serialize_TestEvent()
    {
        // This is just to show that Orleans can in fact serialize the TestEvent type
        // even though it does not itself have properties

        var serializer = _fixture.Services.GetService<Serializer>();

        var testEvent = new TestEvent(Guid.NewGuid());

        var bytes = serializer.SerializeToArray(testEvent);

        var deserializedTestEvent = serializer.Deserialize<TestEvent>(bytes);

        deserializedTestEvent.Should().BeEquivalentTo(testEvent);
    }
}