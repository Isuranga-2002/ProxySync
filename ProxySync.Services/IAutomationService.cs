namespace ProxySync.Services;

public sealed record AutomationResult(bool Success, string Message, string? ProfileName = null, string? NetworkIdentifier = null);

public interface IAutomationService
{
    Task<AutomationResult> EnableAsync();

    Task<AutomationResult> DisableAsync();

    Task<AutomationResult> DetectAsync();

    Task<AutomationResult> AutoSwitchAsync();
}