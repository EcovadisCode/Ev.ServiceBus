using System.Diagnostics;

namespace Ev.ServiceBus.TestHelpers;

public static class DiagnosticIdHelper
{
    public static string GetNewId()
    {
        var activity = new Activity("no activity");
        activity.Start();
        var id = activity.Id;
        activity.Stop();
        return id;
    }
}