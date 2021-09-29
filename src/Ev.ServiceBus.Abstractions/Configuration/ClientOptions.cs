// ReSharper disable once CheckNamespace

namespace Ev.ServiceBus.Abstractions
{
    public abstract class ClientOptions : IClientOptions
    {
        protected ClientOptions(string resourceId, ClientType clientType, bool strictMode)
        {
            OriginalResourceId = resourceId;
            ResourceId = resourceId;
            ClientType = clientType;
            StrictMode = strictMode;
        }

        /// <summary>
        /// When StrictMode is true, the <see cref="ResourceId"/> cannot be changed and the engine will throw if a conflict is found.
        /// When StrictMode is false, the <see cref="ResourceId"/> may change depending on how the configuration is to avoid conflicts.
        /// </summary>
        internal bool StrictMode { get; }

        /// <inheritdoc />
        public string ResourceId { get; private set; }

        public string OriginalResourceId { get; }

        internal void UpdateResourceId(string resourceId)
        {
            if (StrictMode)
            {
                throw new CantRenameOnStrictModeException();
            }

            ResourceId = resourceId;
        }

        /// <inheritdoc />
        public ClientType ClientType { get; }

        /// <inheritdoc />
        public ConnectionSettings? ConnectionSettings { get; internal set; }
    }
}
