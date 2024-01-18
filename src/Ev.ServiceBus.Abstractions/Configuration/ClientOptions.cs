// ReSharper disable once CheckNamespace

namespace Ev.ServiceBus.Abstractions;

public abstract class ClientOptions : IClientOptions
{
    protected ClientOptions(string resourceId, ClientType clientType)
    {
        OriginalResourceId = resourceId;
        ResourceId = resourceId;
        ClientType = clientType;
    }

    /// <inheritdoc />
    public string ResourceId { get; private set; }

    public string OriginalResourceId { get; }

    internal void UpdateResourceId(string resourceId)
    {
        ResourceId = resourceId;
    }

    /// <inheritdoc />
    public ClientType ClientType { get; }

    /// <inheritdoc />
    public ConnectionSettings? ConnectionSettings { get; internal set; }
}