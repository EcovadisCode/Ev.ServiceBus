using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions.Extensions;

public static class DiagnosticIdExtension
{
    private const string DiagnosticIdKey = "Diagnostic-Id";

    public static string? GetDiagnosticId(this ServiceBusReceivedMessage message)
    {
        if (message.ApplicationProperties.ContainsKey(DiagnosticIdKey) && message.ApplicationProperties[DiagnosticIdKey] != null)
        {
            return message.ApplicationProperties[DiagnosticIdKey].ToString();
        }
        return null;
    }

    public static string? GetDiagnosticId(this ServiceBusMessage message)
    {
        if (message.ApplicationProperties.ContainsKey(DiagnosticIdKey) && message.ApplicationProperties[DiagnosticIdKey] != null)
        {
            return message.ApplicationProperties[DiagnosticIdKey].ToString();
        }
        return null;
    }

    public static void SetDiagnosticIdIfIsNot(this ServiceBusMessage message, string diagnosticId)
    {
        if (message.ApplicationProperties.ContainsKey(DiagnosticIdKey) && message.ApplicationProperties[DiagnosticIdKey] != null)
            return;
        message.ApplicationProperties.Add(DiagnosticIdKey, diagnosticId);
    }
}