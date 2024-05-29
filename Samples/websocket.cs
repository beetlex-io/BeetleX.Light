using BeetleX.Light;
using BeetleX.Light.Memory;
using BeetleX.Light.Protocols;
using System.Buffers;
using BeetleX.Light.Extension;
using System.Text;
using BeetleX.Light.Clients;
using System.Text.Json;
using BeetleX.Light.Logs;
using System.Security.Cryptography;
using System.IO.Pipes;
using System.Net;
using System.Reflection.PortableExecutable;
using System;
using System.Reflection;
using System.Threading.Tasks.Dataflow;
NetServer<ApplicationBase, WSSession> netServer = new NetServer<ApplicationBase, WSSession>();
netServer.Options.SetDefaultListen(o =>
{
    o.EnabledSSL("generate.pfx", "12345678",
        System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11);
    o.Port = 80;
    o.SetProtocolChannel<WSServerChannel>();
});
netServer.Options.LogLevel = LogLevel.Trace;
netServer.Options.AddLogOutputHandler<LogOutputToConsole>();
netServer.Start();
await Task.Delay(1000);

Console.ReadLine();

public class WSSession : SesionBase
{
    public override void Receive(NetContext context, StreamHandler stream, object message)
    {
        if (message is WSHttpRequest request)
        {
            OnConnect(context, request);
        }
        else
        {
            DataFrame frame = (DataFrame)message;
            if (frame.Type == DataFrame.PacketType.ping)
            {
                context.GetLoger(LogLevel.Debug)?.Write(context, "WSSession", "Receive", "Ping");
                frame = new DataFrame();
                frame.Type = DataFrame.PacketType.ping;
                context.Send(frame);
            }
            else
            {
                OnDataFrame(context, (DataFrame)message, Request.Url);
            }
        }
    }

    public override void Connected(NetContext context)
    {
        base.Connected(context);
        context.NetStreamHandler.LittleEndian = false;
    }
    public WSHttpRequest Request { get; set; }

    protected virtual void OnConnect(NetContext context, WSHttpRequest request)
    {
        Request = request;
        context.GetLoger(LogLevel.Debug)?.Write(context, "WSSession", "✔ Upgrade", "Success");
        UpgradeWebsocketSuccess success = new UpgradeWebsocketSuccess(request.SecWebSocketKey);
        context.Send(success);
    }
    protected virtual void OnDataFrame(NetContext context, DataFrame data, string url)
    {

        string value = Encoding.UTF8.GetString(data.Body.Value);
        context.GetLoger(LogLevel.Info)?.Write(context, "WSSession", "ReceiveFrame", value);
        DataFrame result = ("Hello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello world");
        context.Send(result);
    }
}

public interface IWSDataWrite
{
    void Write(IStreamWriter writer);
}

public class WSServerChannel : IProtocolChannel<NetContext>
{
    public string Name => "Websocket";

    public NetContext Context { get; set; }

    private bool _connectCompleted = false;
    public object Clone()
    {
        WSServerChannel result = new WSServerChannel();
        result.Context = Context;
        return result;
    }
    private WSHttpRequest _httpRequest = new WSHttpRequest();

    private DataFrame _dataFrame = new DataFrame();
    public void Decoding(IStreamReader reader, Action<NetContext, object> completed)
    {
        while (reader.Length > 0)
        {
            if (_connectCompleted)
            {
                if (_dataFrame.Read(reader) == DataFrame.DataPacketAnalysisState.Completed)
                {
                    var data = _dataFrame;

                    if (data.Type == DataFrame.PacketType.connectionClose)
                    {
                        Context.Dispose();
                        return;
                    }
                    else
                    {
                        _dataFrame = new DataFrame();
                        completed(Context, data);
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                if (_httpRequest.Read(reader) == AnalysisStatus.Completed)
                {
                    if (string.Compare(_httpRequest.Method, "get", true) != 0 || string.Compare(_httpRequest.Upgrade, "websocket", true) != 0)
                    {
                        _httpRequest = new WSHttpRequest();
                        Context.GetLoger(LogLevel.Warring)?.Write(Context, "WSSession", "Upgrade", "Error");
                        UpgradeWebsocketError error = new UpgradeWebsocketError();
                        Context.Send(error);
                    }
                    else
                    {
                        _connectCompleted = true;
                        completed(Context, _httpRequest);
                    }
                }
                else
                {
                    return;

                }
            }
        }
    }
    public void Dispose()
    {
        Context = null;
    }
    public void Encoding(IStreamWriter writer, object message)
    {
        IWSDataWrite wsResponse = (IWSDataWrite)message;
        wsResponse.Write(writer);
    }
}

public class WSClientChannel : IProtocolChannel<NetClient>
{
    public string Name => "HTTPClient";

    public NetClient Context { get; set; }

    public object Clone()
    {
        WSClientChannel result = new WSClientChannel();
        result.Context = Context;
        return result;
    }

    private WSResponse httpResponse = new WSResponse();

    public void Decoding(IStreamReader reader, Action<NetClient, object> completed)
    {
        if (httpResponse.Read(reader) == AnalysisStatus.Completed)
        {
            var result = httpResponse;
            httpResponse = new WSResponse();
            completed(Context, result);
        }
    }
    public void Dispose()
    {
        Context = default;
    }

    public void Encoding(IStreamWriter writer, object message)
    {
        IWSDataWrite request = (IWSDataWrite)message;
        request.Write(writer);
    }
}

public class WSHttpRequest : IWSDataWrite
{
    public WSHttpRequest()
    {
        byte[] key = new byte[16];
        new Random().NextBytes(key);
        SecWebSocketKey = Convert.ToBase64String(key);
    }

    public int SecWebSocketVersion { get; set; } = 13;
    public string SecWebSocketKey { get; set; }
    public string HttpVersion { get; set; } = "1.1";

    public string Method { get; set; } = "GET";

    public string BaseUrl { get; set; }

    public string ClientIP { get; set; }

    public string Path { get; set; }

    public string QueryString { get; set; }

    public string Url { get; set; } = "/";

    public string Upgrade { get; set; }

    public string Connection { get; set; }

    public string Origin { get; set; }

    public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>(new StringComparerIgnoreCase());

    protected AnalysisStatus _analysisStatus { get; set; } = AnalysisStatus.None;
    public AnalysisStatus Read(IStreamReader reader)
    {
        LoadRequestLine(reader);
        LoadRequestHeader(reader);

        return _analysisStatus;
    }
    protected virtual void OnWriteHeader(IStreamWriter writer)
    {
        foreach (var item in Headers)
            writer.WriteLine($"{item.Key}: {item.Value}");
    }
    public void Write(IStreamWriter writer)
    {
        writer.WriteLine($"{Method} {Url} {HttpVersion}");
        OnWriteHeader(writer);
        writer.WriteLine("");
    }
    private void LoadRequestLine(IStreamReader reader)
    {
        if (_analysisStatus == AnalysisStatus.None)
        {
            if (reader.TryReadLine(out string line))
            {
                var subItem = line.SubLeftWith(' ', out string value);
                Method = value;
                subItem = subItem.SubLeftWith(' ', out value);
                Url = value;
                HttpVersion = subItem;

                subItem = Url.SubRightWith('?', out value);
                QueryString = value;
                BaseUrl = subItem;
                Path = subItem.SubRightWith('/', out value);
                if (Path != "/")
                    Path += "/";
                _analysisStatus = AnalysisStatus.LoadingHeader;
            }
        }
    }
    private void LoadRequestHeader(IStreamReader stream)
    {
        if (_analysisStatus == AnalysisStatus.LoadingHeader)
        {
            while (stream.TryReadLine(out string line))
            {
                if (string.IsNullOrEmpty(line))
                {
                    Headers.TryGetValue("Upgrade", out var v);
                    Upgrade = v;

                    Headers.TryGetValue("Connection", out v);
                    Connection = v;

                    Headers.TryGetValue("Origin", out v);
                    Origin = v;

                    Headers.TryGetValue("Sec-WebSocket-Key", out v);
                    SecWebSocketKey = v;

                    if (Headers.TryGetValue("Sec-WebSocket-Version", out v))
                        SecWebSocketVersion = int.Parse(v);
                    _analysisStatus = AnalysisStatus.Completed;
                    return;
                }
                var name = line.SubRightWith(':', out string value);
                value = value.Trim();
                Headers[name] = value;
            }

        }
    }

}
public class StringComparerIgnoreCase : IEqualityComparer<string>
{
    public bool Equals(string x, string y)
    {
        if (x != null && y != null)
        {
            return x.ToLowerInvariant() == y.ToLowerInvariant();
        }
        return false;
    }

    public int GetHashCode(string obj)
    {
        return obj.GetHashCode();
    }
}

public class WSResponse : IWSDataWrite
{

    public WSResponse()
    {
        Headers["Content-Type"] = "text/html";
    }

    public string HttpVersion { get; set; } = "HTTP/1.1";

    public int Status { get; set; }

    public string StatusMessage { get; set; } = "OK";

    public int ContentLength { get; set; }

    public byte[] Body { get; set; }

    public Dictionary<string, string> Headers = new Dictionary<string, string>();
    private AnalysisStatus _analysisStatus { get; set; } = AnalysisStatus.None;
    public AnalysisStatus Read(IStreamReader reader)
    {
        LoadRequestLine(reader);
        LoadRequestHeader(reader);
        LoadRequestBody(reader);
        return _analysisStatus;
    }
    private void LoadRequestLine(IStreamReader reader)
    {
        if (_analysisStatus == AnalysisStatus.None)
        {
            if (reader.TryReadLine(out string line))
            {
                string[] values = line.Split(' ', StringSplitOptions.TrimEntries);
                _analysisStatus = AnalysisStatus.LoadingHeader;
                HttpVersion = values[0];
                Status = int.Parse(values[1]);
                StatusMessage = values[2];
            }
        }
    }
    private void LoadRequestHeader(IStreamReader stream)
    {
        if (_analysisStatus == AnalysisStatus.LoadingHeader)
        {
            while (stream.TryReadLine(out string line))
            {
                if (string.IsNullOrEmpty(line))
                {
                    if (ContentLength == 0)
                    {
                        _analysisStatus = AnalysisStatus.Completed;
                    }
                    else
                    {
                        _analysisStatus = AnalysisStatus.LoadingBody;
                    }
                    return;
                }
                var name = line.SubRightWith(':', out string value);
                if (String.Compare(name, "Content-Length", true) == 0)
                {
                    ContentLength = int.Parse(value);
                }
                else if (string.Compare(name, "Transfer-Encoding", true) == 0)
                {
                    throw new BXException("Transfer-Encoding not support!");
                }
                Headers[name] = value.Trim();
            }
        }
    }
    private void LoadRequestBody(IStreamReader stream)
    {
        if (_analysisStatus == AnalysisStatus.LoadingBody)
        {
            if (stream.Length >= ContentLength)
            {
                var data = new byte[ContentLength]; ;
                stream.Read(data, 0, data.Length);
                Body = data;
                _analysisStatus = AnalysisStatus.Completed;
            }
        }
    }

    protected virtual void OnWriteHeader(IStreamWriter writer)
    {

    }
    public void Write(IStreamWriter writer)
    {
        writer.WriteLine($"{HttpVersion} {Status} {StatusMessage}");
        OnWriteHeader(writer);
        foreach (var item in Headers)
            writer.WriteLine($"{item.Key}: {item.Value}");
        byte[] bodyData = Body;

        if (bodyData != null)
        {
            writer.WriteLine($"Content-Length: {bodyData.Length}");
        }
        else
        {
            writer.WriteLine($"Content-Length: 0");
        }
        writer.WriteLine("");
        if (bodyData != null)
        {
            writer.Write(bodyData, 0, bodyData.Length);
        }
    }

    public void SetText(string text)
    {
        Body = Encoding.UTF8.GetBytes(text);
        Headers["Content-Type"] = "text/html; charset=utf-8";
    }

    public void SetJson(object obj)
    {
        using (System.IO.MemoryStream stream = new MemoryStream())
        {
            JsonSerializer.Serialize(stream, obj);
            stream.Position = 0;
            Body = stream.ToArray();
            Headers["Content-Type"] = "application/json; charset=utf-8";
        }
    }
}

public class UpgradeWebsocketError : WSResponse
{
    public UpgradeWebsocketError()
    {
        Status = 400;
        StatusMessage = "Bad Request";
    }

}

public class UpgradeWebsocketSuccess : WSResponse
{
    public UpgradeWebsocketSuccess(string websocketKey)
    {
        Status = 101;
        StatusMessage = "Switching Protocols";
        WebsocketKey = websocketKey;
    }

    public string WebsocketKey { get; set; }
    protected override void OnWriteHeader(IStreamWriter writer)
    {
        SHA1 sha1 = new SHA1CryptoServiceProvider();
        byte[] bytes_sha1_in = Encoding.UTF8.GetBytes(WebsocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
        byte[] bytes_sha1_out = sha1.ComputeHash(bytes_sha1_in);
        string str_sha1_out = Convert.ToBase64String(bytes_sha1_out);
        Headers.Add("Connection", "Upgrade");
        Headers.Add("Upgrade", "websocket");
        Headers.Add("Sec_WebSocket_Version", "13");
        Headers.Add("Sec-WebSocket-Accept", str_sha1_out);
        Headers.Add("Date", DateTime.Now.ToUniversalTime().ToString("r"));
    }
}

public enum AnalysisStatus
{
    None,
    LoadingHeader,
    LoadingBody,
    Completed
}

public class DataFrame : IWSDataWrite
{
    public DataFrame()
    {
        this.FIN = true;
        Type = PacketType.text;
        IsMask = false;
    }

    public static implicit operator DataFrame(string data)
    {
        DataFrame frame = new DataFrame();
        frame.Body = Encoding.UTF8.GetBytes(data);
        return frame;
    }

    public static implicit operator DataFrame((string, byte[]) data)
    {
        DataFrame frame = new DataFrame();
        frame.Body = Encoding.UTF8.GetBytes(data.Item1);
        frame.MaskKey = data.Item2;
        return frame;
    }

    const int CHECK_B1 = 0x1;

    const int CHECK_B2 = 0x2;

    const int CHECK_B3 = 0x4;

    const int CHECK_B4 = 0x8;

    const int CHECK_B5 = 0x10;

    const int CHECK_B6 = 0x20;

    const int CHECK_B7 = 0x40;

    const int CHECK_B8 = 0x80;

    public bool FIN { get; set; }

    public bool RSV1 { get; set; }

    public bool RSV2 { get; set; }

    public bool RSV3 { get; set; }

    public PacketType Type { get; set; }

    public ArraySegment<byte>? Body { get; set; }

    public bool IsMask { get; set; }

    internal byte PayloadLen { get; set; }

    public ulong Length { get; set; }

    public byte[] MaskKey { get; set; }

    private DataPacketAnalysisState _status = DataPacketAnalysisState.None;

    public DataPacketAnalysisState Read(IStreamReader stream)
    {
        if (_status == DataPacketAnalysisState.None)
        {
            if (stream.Length >= 2)
            {
                byte value = (byte)stream.ReadByte();
                this.FIN = (value & CHECK_B8) > 0;
                this.RSV1 = (value & CHECK_B7) > 0;
                this.RSV2 = (value & CHECK_B6) > 0;
                this.RSV3 = (value & CHECK_B5) > 0;
                this.Type = (PacketType)(byte)(value & 0xF);
                value = (byte)stream.ReadByte();
                this.IsMask = (value & CHECK_B8) > 0;
                this.PayloadLen = (byte)(value & 0x7F);
                _status = DataPacketAnalysisState.Header;
            }
        }

        if (_status == DataPacketAnalysisState.Header)
        {
            if (this.PayloadLen == 127)
            {
                if (stream.Length >= 8)
                {
                    Length = stream.ReadUInt64();
                    _status = DataPacketAnalysisState.Length;
                }
            }
            else if (this.PayloadLen == 126)
            {
                if (stream.Length >= 2)
                {
                    Length = stream.ReadUInt16();
                    _status = DataPacketAnalysisState.Length;
                }
            }
            else
            {
                this.Length = this.PayloadLen;
                _status = DataPacketAnalysisState.Length;
            }
        }

        if (_status == DataPacketAnalysisState.Length)
        {
            if (IsMask)
            {
                if (stream.Length >= 4)
                {
                    this.MaskKey = new byte[4];
                    stream.Read(this.MaskKey, 0, 4);
                    _status = DataPacketAnalysisState.Mask;
                }
            }
            else
            {
                _status = DataPacketAnalysisState.Mask;
            }
        }
        if (_status == DataPacketAnalysisState.Mask)
        {
            if (this.Length == 0)
            {
                _status = DataPacketAnalysisState.Completed;
            }
            else
            {
                if ((ulong)stream.Length >= this.Length)
                {
                    var len = (int)this.Length;
                    var data = new byte[len];
                    stream.Read(data, 0, len);
                    Body = new ArraySegment<byte>(data, 0, len);
                    ulong index = 0;
                    if (this.IsMask)
                    {
                        MarkBytes(data, 0, len - 1, index);
                    }
                    _status = DataPacketAnalysisState.Completed;
                }
            }
        }
        return _status;
    }

    private ulong MarkBytes(Span<byte> bytes, int start, int end, ulong index)
    {
        for (int i = start; i <= end; i++)
        {
            bytes[i] = (byte)(bytes[i] ^ MaskKey[index % 4]);
            index++;
            if (index >= this.Length)
                break;
        }
        return index;
    }

    public void Write(IStreamWriter stream)
    {

        byte[] header = new byte[2];
        if (FIN)
            header[0] |= CHECK_B8;
        if (RSV1)
            header[0] |= CHECK_B7;
        if (RSV2)
            header[0] |= CHECK_B6;
        if (RSV3)
            header[0] |= CHECK_B5;
        header[0] |= (byte)Type;
        if (Body != null)
        {
            ArraySegment<byte> data = Body.Value;
            if (MaskKey == null || MaskKey.Length != 4)
                this.IsMask = false;
            else
                this.IsMask = true;
            if (this.IsMask)
            {
                header[1] |= CHECK_B8;
                int offset = data.Offset;
                for (int i = offset; i < data.Count; i++)
                {
                    data.Array[i] = (byte)(data.Array[i] ^ MaskKey[(i - offset) % 4]);
                }
            }
            int len = data.Count;
            if (len > 125 && len <= UInt16.MaxValue)
            {
                header[1] |= (byte)126;
                stream.Write(header, 0, 2);
                stream.WriteUInt16((UInt16)len);
            }
            else if (len > UInt16.MaxValue)
            {
                header[1] |= (byte)127;
                stream.Write(header, 0, 2);
                stream.WriteUInt64((ulong)len);
            }
            else
            {
                header[1] |= (byte)data.Count;
                stream.Write(header, 0, 2);
            }
            if (IsMask)
                stream.Write(MaskKey, 0, 4);
            stream.Write(data.Array, data.Offset, data.Count);

        }
        else
        {
            stream.Write(header, 0, 2);
        }

    }
    public enum DataPacketAnalysisState
    {
        None,
        Header,
        Length,
        Mask,
        Completed
    }
    public enum PacketType : byte
    {
        continuation = 0x0,
        text = 0x1,
        binary = 0x2,
        non_control3 = 0x3,
        non_control4 = 0x4,
        non_control5 = 0x5,
        non_control6 = 0x6,
        non_control7 = 0x7,
        connectionClose = 0x8,
        ping = 0x9,
        pong = 0xA,
        controlB = 0xB,
        controlE = 0xE,
        controlF = 0xF
    }
}