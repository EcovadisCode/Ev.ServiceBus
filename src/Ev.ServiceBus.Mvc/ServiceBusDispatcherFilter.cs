using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ev.ServiceBus.Mvc;

public class ServiceBusDispatcherFilter : IAsyncResultFilter
{
    private readonly IMessageDispatcher _dispatcher;

    public ServiceBusDispatcherFilter(IMessageDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        await next();
        await _dispatcher.ExecuteDispatches(context.HttpContext.RequestAborted);
    }
}