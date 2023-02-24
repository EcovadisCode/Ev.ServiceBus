using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions.Extensions;

public static class DiagnosticIdExtension
{
    private const string DiagnosticIdKey = "Diagnostic-Id";
    public static string? GetDiagnosticId(this ServiceBusReceivedMessage message)
    {
        return message.ApplicationProperties.ContainsKey(DiagnosticIdKey)
            ? message.ApplicationProperties[DiagnosticIdKey].ToString()
            : null;
    }

    public static string? GetDiagnosticId(this ServiceBusMessage message)
    {
        return message.ApplicationProperties.ContainsKey(DiagnosticIdKey)
            ? message.ApplicationProperties[DiagnosticIdKey].ToString()
            : null;
    }

    public static void SetDiagnosticIdIfIsNot(this ServiceBusMessage message, string diagnosticId)
    {
        if(message.ApplicationProperties.ContainsKey(DiagnosticIdKey))
            return;
        message.ApplicationProperties.Add(DiagnosticIdKey, diagnosticId);
    }
}