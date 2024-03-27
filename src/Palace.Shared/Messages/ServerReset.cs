namespace Palace.Shared.Messages;

public class ServerReset : IMessage
{
	public DateTime Timeout { get; set; } = DateTime.Now.AddSeconds(30);
}
