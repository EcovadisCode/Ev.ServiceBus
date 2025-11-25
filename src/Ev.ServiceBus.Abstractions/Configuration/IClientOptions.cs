// ReSharper disable once CheckNamespace

using Ev.ServiceBus.Abstractions.Configuration;

namespace Ev.ServiceBus.Abstractions;

public interface IClientOptions
{
    /// <summary>
    /// Unique identifier for the registered resource.
    /// </summary>
    string ResourceId { get; }

    /// <summary>
    /// The client's type for the current resource.
    /// </summary>
    ClientType ClientType { get; }

    /// <summary>
    /// The connection settings for the current resource.
    /// If this is not set, it will try to get the default connection
    /// set by calling `services.AddServiceBus<>(settings => {})`.
    /// If there's is no default connection Then the system will not instantiate the current resource.
    /// </summary>
    public ConnectionSettings? ConnectionSettings { get; }
}