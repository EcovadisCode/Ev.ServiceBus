namespace Ev.ServiceBus.Abstractions.Listeners;

/// <summary>
/// Optional extension for <see cref="ITransactionManager"/> implementations that need to react
/// to receive-loop cancellations (e.g. pod graceful shutdown) — for example, to prevent APM
/// from recording the cancellation as an error transaction.
/// </summary>
public interface ICancellationAwareTransactionManager
{
    void OnReceiveCancelled();
}
