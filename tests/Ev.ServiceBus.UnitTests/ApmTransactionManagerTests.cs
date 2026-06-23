using System;
using Ev.ServiceBus.Apm;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

public sealed class ApmTransactionManagerTests : IDisposable
{
    public ApmTransactionManagerTests()
    {
        ApmTransactionManager.ResetForTests();
    }

    public void Dispose()
    {
        ApmTransactionManager.ResetForTests();
    }

    [Fact]
    public void ShouldSuppressError_WithTrackedTransactionId_ReturnsTrue()
    {
        ApmTransactionManager.AddCancelledTransactionIdForTests("tx-abc");

        var result = ApmTransactionManager.ShouldSuppressError(
            "tx-abc",
            "System.Threading.Tasks.TaskCanceledException",
            "some.culprit");

        Assert.True(result);
    }

    [Fact]
    public void ShouldSuppressError_WithTrackedTransactionId_RemovesIdAfterMatch()
    {
        ApmTransactionManager.AddCancelledTransactionIdForTests("tx-abc");
        ApmTransactionManager.ShouldSuppressError("tx-abc", "System.Threading.Tasks.TaskCanceledException", "some.culprit");

        var secondResult = ApmTransactionManager.ShouldSuppressError(
            "tx-abc",
            "System.Threading.Tasks.TaskCanceledException",
            "some.culprit");

        Assert.False(secondResult);
    }

    [Fact]
    public void ShouldSuppressError_WithAmqpReceiverCulpritAndTaskCanceledException_ReturnsTrue()
    {
        var result = ApmTransactionManager.ShouldSuppressError(
            "untracked-tx",
            "System.Threading.Tasks.TaskCanceledException",
            "Azure.Messaging.ServiceBus.Amqp.AmqpReceiver+<ReceiveMessagesAsyncInternal>d__45");

        Assert.True(result);
    }

    [Fact]
    public void ShouldSuppressError_WithAmqpReceiverCulpritAndOperationCanceledException_ReturnsTrue()
    {
        var result = ApmTransactionManager.ShouldSuppressError(
            "untracked-tx",
            "System.OperationCanceledException",
            "Azure.Messaging.ServiceBus.Amqp.AmqpReceiver+SomeInternalMethod");

        Assert.True(result);
    }

    [Fact]
    public void ShouldSuppressError_WithAmqpReceiverCulpritButServiceBusException_ReturnsFalse()
    {
        var result = ApmTransactionManager.ShouldSuppressError(
            "untracked-tx",
            "Azure.Messaging.ServiceBus.ServiceBusException",
            "Azure.Messaging.ServiceBus.Amqp.AmqpReceiver+SomeInternalMethod");

        Assert.False(result);
    }

    [Fact]
    public void ShouldSuppressError_WithNonAmqpCulpritAndTaskCanceledException_ReturnsFalse()
    {
        var result = ApmTransactionManager.ShouldSuppressError(
            null,
            "System.Threading.Tasks.TaskCanceledException",
            "Some.Other.Namespace.SomeClass+SomeMethod");

        Assert.False(result);
    }

    [Fact]
    public void ShouldSuppressError_WithUntrackedIdAndNonCancelledException_ReturnsFalse()
    {
        var result = ApmTransactionManager.ShouldSuppressError(
            "not-tracked",
            "System.InvalidOperationException",
            "Some.Class+SomeMethod");

        Assert.False(result);
    }

    [Fact]
    public void ShouldSuppressError_WithNullTransactionIdAndNullCulprit_ReturnsFalse()
    {
        var result = ApmTransactionManager.ShouldSuppressError(null, null, null);

        Assert.False(result);
    }

    [Fact]
    public void ShouldSuppressError_WhenCapExceeded_CulpritPathStillSuppresses()
    {
        // Simulate the cap-exceeded scenario: OnReceiveCancelled stops tracking IDs once the
        // dictionary is full. Errors for those untracked transactions must still be suppressed
        // by the culprit-based path (Case 2).
        for (var i = 0; i < 1000; i++)
            ApmTransactionManager.AddCancelledTransactionIdForTests($"capped-tx-{i}");

        var result = ApmTransactionManager.ShouldSuppressError(
            "untracked-due-to-cap",
            "System.Threading.Tasks.TaskCanceledException",
            "Azure.Messaging.ServiceBus.Amqp.AmqpReceiver+<ReceiveMessagesAsyncInternal>d__45");

        Assert.True(result);
    }
}
