// See https://aka.ms/new-console-template for more information
using BeetleX.Light;
using BeetleX.Light.Clients;
using BeetleX.Light.Logs;
using BeetleX.Light.Memory;
using BeetleX.Light.Protocols;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

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