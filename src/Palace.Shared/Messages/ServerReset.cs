namespace Palace.Shared.Messages;

public class ServerReset : IMessage
{
	public DateTime Timeout => DateTime.Now.AddSeconds(15);
}
