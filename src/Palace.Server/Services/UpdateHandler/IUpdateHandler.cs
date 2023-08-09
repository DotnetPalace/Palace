namespace Palace.Server.Services.UpdateHandler;

public interface IUpdateHandler : IDisposable
{
    string Name { get; }
    Task ProcessUpdateAsync(Models.MicroserviceUpdateContext context, CancellationToken cancellationToken);
    IUpdateHandler? NextHandler { get; set; }
}
