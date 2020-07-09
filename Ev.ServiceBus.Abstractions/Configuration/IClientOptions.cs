// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public interface IClientOptions
    {
        string EntityPath { get; }
        string ConnectionString { get; }
    }
}