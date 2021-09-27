using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Reception;

namespace Ev.ServiceBus.Sample.Contracts
{
    public class UserCreatedHandler : IMessageReceptionHandler<UserCreated>
    {
        public Task Handle(UserCreated @event, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public class UserCreated
    {
        public string UserId { get; set; }
    }

    public class UserPreferencesUpdatedHandler: IMessageReceptionHandler<UserPreferencesUpdated>
    {
        public Task Handle(UserPreferencesUpdated @event, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public class UserPreferencesUpdated
    {
        public bool SendNotifications { get; set; }
    }
}
