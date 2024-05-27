## BeetleX.Light
Based on pipelines high performance dotnet core socket tcp communication components, support tls, http, https, websocket, rpc, mqtt, redis protocols, millions of connections are supported.

### client
``` csharp
NetClient client = "tcp://127.0.0.1:64943";
client.LogLevel = LogLevel.Trace;
client.AddLogOutputHandler<LogOutputToConsole>().AddLogOutputHandler<LogOutputToFile>();
client.Receive = (client, Stream, msg) =>
{
    string value = Stream.ReadString(Encoding.UTF8, (int)Stream.Stream.Length);
    Stream.WriteString(value);
    Stream.Flush();
};
ProtocolData data = "hello world";
await client.Send(data);
```
### client ssl
``` chsarp
NetClient client = "tcp://127.0.0.1:64944";
client.SslServiceName = "beetlex-io.com";
client.LogLevel = LogLevel.Trace;
client.AddLogOutputHandler<LogOutputToConsole>().AddLogOutputHandler<LogOutputToFile>();
client.Receive = (client, Stream, msg) =>
{
    string value = Stream.ReadString(Encoding.UTF8, (int)Stream.Stream.Length);
    Stream.WriteString(value);
    Stream.Flush();
};
ProtocolData data = "hello world";
await client.Send(data);
```
### client custom protocol
``` csharp
NetClient client = "tcp://127.0.0.1:64944";
client.SslServiceName = "beetlex-io.com";
client.LogLevel = LogLevel.Trace;
client.AddLogOutputHandler<LogOutputToConsole>().AddLogOutputHandler<LogOutputToFile>();
client.SetProtocolChannel<JsonChannel>();
client.RegisterHandler<User>((c, msg) =>
{
    c.GetLoger(LogLevel.Info)?.Write(c, "Client", "Receive", $"name:{msg.Name} email:{msg.Email}");
});
client.Receive = (c, stream, msg) =>
{
    Console.WriteLine($"receive:{msg}");
};
User user = new User { Name = "henry", Email = "henryfan@msn.com" };
while (true)
{
    await client.Send(user);
    await Task.Delay(10000);
}
Console.ReadLine();

public class JsonChannel : IProtocolChannel<NetClient>
{
    public void Encoding(IStreamWriter writer, object message)
    {
        writer.WriteBinaryObject(HeaderSizeType.Short, message, (stream, msg) =>
        {
            JsonSerializer.Serialize(stream, msg);
        });
    }

    public void Decoding(IStreamReader reader, Action<NetClient, object> completed)
    {
        while (reader.TryReadBinaryObject(HeaderSizeType.Short,
                out object result,
                memory => JsonSerializer.Deserialize<User>(memory.Span))
               )
        {
            completed(Context, result);
        }
    }

    public string Name => "JsonChannel";

    public NetClient Context { get; set; }

    public object Clone()
    {
        JsonChannel result = new JsonChannel();
        result.Context = Context;
        return result;
    }

    public void Dispose()
    {
    }

}

public class User
{
    public string Name { get; set; }

    public string Email { get; set; }
}
```
### Server
``` chsarp
NetServer<Application, Session> netServer = new NetServer<Application, Session>();
netServer.Options.SetDefaultListen(o =>
{
    o.EnabledSSL("generate.pfx", "12345678", System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11);
});
netServer.Options.LogLevel = LogLevel.Trace;
netServer.Options.AddLogOutputHandler<LogOutputToConsole>().AddLogOutputHandler<LogOutputToFile>();
netServer.Options.SetDefaultListen(p => { p.SetProtocolChannel<JsonChannel>(); });
netServer.Start();
Console.ReadLine();

public class Session : SesionBase
{
    public override void Receive(NetContext context, StreamHandler stream, object message)
    {
        User msg = (User)message;
        context.GetLoger(LogLevel.Info)?.Write(context, "Server", "Receive", $"name:{msg.Name} email:{msg.Email}");
    }
    public override void Connected(NetContext context)
    {
        base.Connected(context);
        Task.Run(async () =>
        {
            User user = new User { Name = "henry", Email = "henryfan@msn.com" };
            while (true)
            {
                context.Send(user);
                await Task.Delay(10000);
            }

        });
    }
}
public class Application : ApplicationBase
{

}

public class JsonChannel : IProtocolChannel<NetContext>
{
    public void Encoding(IStreamWriter writer, object message)
    {
        writer.WriteBinaryObject(HeaderSizeType.Short, message, (stream, msg) =>
        {
            JsonSerializer.Serialize(stream, msg);
        });
    }

    public void Decoding(IStreamReader reader, Action<NetContext, object> completed)
    {
        while (reader.TryReadBinaryObject(HeaderSizeType.Short,
                out object result,
                memory => JsonSerializer.Deserialize<User>(memory.Span))
               )
        {
            completed(Context, result);
        }
    }

    public string Name => "JsonChannel";

    public NetContext Context { get; set; }

    public object Clone()
    {
        JsonChannel result = new JsonChannel();
        result.Context = Context;
        return result;
    }

    public void Dispose()
    {

    }

}

public class User
{
    public string Name { get; set; }

    public string Email { get; set; }
}
```
