using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Reception;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public class CourseCreated { }

    public class StudentCreatedHandler : IMessageReceptionHandler<StudentCreated>
    {
        public Task Handle(StudentCreated @event, CancellationToken cancellationToken) { return Task.CompletedTask; }
    }

    public class CourseCreatedHandler : IMessageReceptionHandler<CourseCreated>
    {
        public Task Handle(CourseCreated @event, CancellationToken cancellationToken) { return Task.CompletedTask; }
    }

    public class StudentCreated { }
}
