using System.Xml;

namespace Palace.Shared.Messages;

public class InstallService : IMessage
{
    public required Guid ActionId { get; set; }
    public required string HostName { get; set; } = null!;
    public required  MicroServiceSettings ServiceSettings { get; set; } = new();
    public required string DownloadUrl { get; set; } = null!;
    public string? OverridedArguments { get; set; }
    public string Trigger { get; set; } = null!;
    public DateTime Timeout { get; set; } = DateTime.Now.AddSeconds(15);
}
