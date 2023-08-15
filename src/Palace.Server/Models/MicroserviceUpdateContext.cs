namespace Palace.Server.Models;

public sealed class MicroserviceUpdateContext : IDisposable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ManualResetEvent ManualResetEvent { get; set; } = new(false);

    public string Key => $"{HostName}-{ServiceSettings.ServiceName}".ToLower();
    public string HostName { get; set; } = null!;
    public Palace.Shared.MicroServiceSettings ServiceSettings { get; set; } = default!;
    public Models.ExtendedMicroServiceInfo ServiceInfo { get; set; } = default!;
    public string CurrentStep { get; set; } = null!;
    public ServiceState InitialServiceState { get; set; }
    public string Origin { get; set; } = null!;
    public string? OverridedArguments { get; set; }

    public void Dispose()
    {
        ManualResetEvent.Dispose();
        GC.SuppressFinalize(this);
    }
}
