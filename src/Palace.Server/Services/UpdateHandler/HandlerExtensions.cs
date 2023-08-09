namespace Palace.Server.Services.UpdateHandler;

public static class HandlerExtensions
{
    public static void AddNextHandler(this IUpdateHandler rootHandler, IUpdateHandler nextHandler)
    {
        var b = rootHandler;
        while (true)
        {
            if (b.NextHandler is null)
            {
                b.NextHandler = nextHandler;
                break;
            }
            b = b.NextHandler;
        }
    }
}
