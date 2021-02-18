// ReSharper disable once CheckNamespace

namespace Ev.ServiceBus.Abstractions
{
    public abstract class ClientOptions : IClientOptions
    {
        protected ClientOptions(string resourceId, ClientType clientType, bool strictMode)
        {
            ResourceId = resourceId;
            ClientType = clientType;
            StrictMode = strictMode;
        }

        /// <summary>
        /// When StrictMode is true, the <see cref="ResourceId"/> cannot be changed and the engine will throw if a conflict is found.
        /// When StrictMode is false, the <see cref="ResourceId"/> may change depending on how the configuration is to avoid conflicts.
        /// </summary>
        internal bool StrictMode { get; }

        /// <summary>
        /// Internal identifier of the Resource
        /// </summary>
        public string ResourceId { get; private set; }

        internal void UpdateResourceId(string resourceId)
        {
            if (StrictMode)
            {
                throw new CantRenameOnStrictModeException();
            }

            ResourceId = resourceId;
        }

        public ClientType ClientType { get; }
        public ConnectionSettings? ConnectionSettings { get; internal set; }
    }
}
