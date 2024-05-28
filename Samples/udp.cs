// See https://aka.ms/new-console-template for more information

using BeetleX.Light.Logs;
using BeetleX.Light.Protocols;
using BeetleX.Light.UDP;
using Microsoft.VisualBasic;
using System.Data;
using System.Text.Json;

ProtocolMessageMapperFactory.StringMapper.RegisterAssembly<JsonChannel>();
UdpServer udp = "udp://127.0.0.1:8088";
udp.AddLogOutputHandler<LogOutputToConsole>();
udp.SetProtocolChannel<JsonChannel>();
udp.Receive = async (s, d) =>
{
    Register reg = (Register)d.Message;
    s.GetLoger(LogLevel.Info)?.Write(d.RemoteEndPoint, "UdpData", "Receive", $"name:{reg.Name} email:{reg.Email}");
    RegComp regComp = new RegComp();
    regComp.Success = true;
    regComp.RegTime = DateTime.Now;
    d.Reply(regComp);
};
udp.Start();

UdpServer client = "udp://127.0.0.1:8089";
client.AddLogOutputHandler<LogOutputToConsole>();
client.SetProtocolChannel<JsonChannel>();
client.Start();
client.Receive = (s, d) =>
{
    RegComp regComp = (RegComp)d.Message;
    s.GetLoger(LogLevel.Info)?.Write(d.RemoteEndPoint, "UdpData", "Receive", $"Success:{regComp.Success} Time:{regComp.RegTime}");
    return Task.CompletedTask;
};
while (true)
{
    Register reg = new Register();
    reg.Name = "henry";
    reg.Email = "henryfan@msn.com";
    client.Send(reg, udp.EndPoint);
    await Task.Delay(2000);
}
Console.ReadLine();

public class JsonChannel : IUdpProtocolChannel
{
    public string Name { get => "JsonChannel"; }

    public object Read(ReadOnlyMemory<byte> buffer, bool littleEndian)
    {
        var result = ProtocolMessageMapperFactory.StringMapper.ReadType(buffer, littleEndian);
        buffer = buffer.Slice(result.BuffersLength);
        return JsonSerializer.Deserialize(buffer.Span, result.MessageType);
    }

    public void Write(Stream stream, object data, bool littleEndian)
    {
        ProtocolMessageMapperFactory.StringMapper.WriteType(stream, data, littleEndian);
        JsonSerializer.Serialize(stream, data);
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