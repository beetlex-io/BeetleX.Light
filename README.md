## BeetleX.Light
Based on pipelines high performance dotnet core socket tcp communication components, support tls, http, https, websocket, rpc, mqtt, redis protocols, millions of connections are supported.

## base
``` csharp
NetServer<Application, UserSession> netServer = new NetServer<Application, UserSession>();
netServer.Options.SetDefaultListen(o =>
{
    o.Port = 8089;
    //o.EnabledSSL("generate.pfx", "12345678",
    //    System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11);
});
netServer.Options.LogLevel = LogLevel.Trace;
netServer.Options.AddLogOutputHandler<LogOutputToConsole>().AddLogOutputHandler<LogOutputToFile>();
netServer.Start();
await Task.Delay(1000);

NetClient client = "tcp://127.0.0.1:8089";
//client.SslServiceName = "beetlex-io.com";

client.AddLogOutputHandler<LogOutputToConsole>();
client.LogLevel = LogLevel.Trace;
client.Receive = (client, handler, msg) =>
{
    client.GetLoger(LogLevel.Info)?.Write(client, "Client", "Receive", handler.ReadString());
};
await client.Connect();
while (true)
{
    client.NetStreamHandler.WriteString("Hello World");
    client.NetStreamHandler.Flush();
    await Task.Delay(5000);
}
Console.ReadLine();

public class UserSession : SesionBase
{
    public override void Receive(NetContext context, StreamHandler stream, object message)
    {
        context.GetLoger(LogLevel.Info)?.Write(context, "Client", "Receive", stream.ReadString());
        stream.WriteString($"Hello {DateTime.Now}");
        stream.Flush();
    }
    public override void Connected(NetContext context)
    {
        base.Connected(context);
    }
}
public class Application : ApplicationBase
{

}
```
![image](https://github.com/beetlex-io/BeetleX.Light/assets/2564178/a1c1ac16-50c6-406e-8c3d-010969c0daa4)
## custom json protocol
``` csharp
ProtocolMessageMapperFactory.StringMapper.RegisterAssembly<UserSession>();
NetServer<Application, UserSession> netServer = new NetServer<Application, UserSession>();
netServer.Options.SetDefaultListen(o =>
{
    o.EnabledSSL("generate.pfx", "12345678", System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11);
});
netServer.Options.LogLevel = LogLevel.Trace;
netServer.Options.AddLogOutputHandler<LogOutputToConsole>();
netServer.Options.SetDefaultListen(p => { p.SetProtocolChannel<JsonChannel<NetContext>>(); });
netServer.Start();
await Task.Delay(1000);

AwaiterNetClient<JsonChannel<NetClient>> client = "tcp://127.0.0.1:8090";
client.SslServiceName = "beetlex-io.com";
client.SetProtocolChannel<JsonChannel<NetClient>>();
client.AddLogOutputHandler<LogOutputToConsole>();
client.LogLevel = LogLevel.Trace;
client.TimeOut = 1000000;
var request = new Register();
request.Name = "henryfan";
request.Email = "henryfan@msn.com"; ;
while (true)
{
    var response = await client.Request<RegComp>(request);
    client.GetLoger(LogLevel.Info)?.Write(client, "Client", "RegisterRequest", $"Success:{response.Success} Time:{response.RegTime}");
    await Task.Delay(5000);
}
Console.ReadLine();

public class UserSession : SesionBase
{
    public override void Receive(NetContext context, StreamHandler stream, object message)
    {
        if (message is Register reg)
        {
            context.GetLoger(LogLevel.Info)?.Write(context, "UserSession", "Receive", $"name:{reg.Name} email:{reg.Email}");
            var response = new RegComp();
            response.RegTime = DateTime.Now;
            response.Success = true;
            context.Send(response);
        }

    }
    public override void Connected(NetContext context)
    {
        base.Connected(context);
    }
}
public class Application : ApplicationBase
{

}

public class JsonChannel<T> : IProtocolChannel<T>
    where T : INetContext
{
    public void Encoding(IStreamWriter writer, object message)
    {
        writer.WriteBinaryObject(HeaderSizeType.Short, message,
            (stream, msg) =>
            {
                ProtocolMessageMapperFactory.StringMapper.WriteType(stream, message, writer.LittleEndian);
                JsonSerializer.Serialize(stream, msg);
            });
    }

    public void Decoding(IStreamReader reader, Action<T, object> completed)
    {
        while (reader.TryReadBinaryObject(HeaderSizeType.Short,
                out object result,
                memory =>
                {
                    var type = ProtocolMessageMapperFactory.StringMapper.ReadType(memory, reader.LittleEndian);

                    if (type.MessageType == null)
                    {
                        BXException ex = new BXException($"{type} not mapping type!");
                        Context.GetLoger(LogLevel.Error)?.WriteException(Context, "JsonChannel", "Decoding", ex);
                        Context.Close(ex);
                    }
                    return JsonSerializer.Deserialize(memory.Span.Slice(type.BUffersLength), type.MessageType);
                })
               )
        {
            completed(Context, result);
        }
    }

    public string Name => "JsonChannel";

    public T Context { get; set; }

    public object Clone()
    {
        JsonChannel<T> result = new JsonChannel<T>();
        result.Context = Context;
        return result;
    }

    public void Dispose()
    {

    }

}

[ProtocolObject]
public class Register
{
    public string Name { get; set; }

    public string Email { get; set; }

    public string Password { get; set; }
}
[ProtocolObject]
public class RegComp
{
    public bool Success { get; set; }

    public DateTime RegTime { get; set; }
}
```
![image](https://github.com/beetlex-io/BeetleX.Light/assets/2564178/28a9ab60-1769-41fb-b673-43d92656564f)
