// See https://aka.ms/new-console-template for more information
using BeetleX.Light;
using BeetleX.Light.Memory;
using BeetleX.Light.Protocols;
using System.Buffers;
using BeetleX.Light.Extension;
using System.Text;
using BeetleX.Light.Clients;
using System.Text.Json;
using BeetleX.Light.Logs;

NetServer<ApplicationBase, HttpSession> netServer = new NetServer<ApplicationBase, HttpSession>();
ThreadPool.SetMinThreads(Environment.ProcessorCount * 2, Environment.ProcessorCount * 2);
netServer.Options.SetDefaultListen(o =>
{
    //o.EnabledSSL("generate.pfx", "12345678",
    //    System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11);
    o.Port = 80;
    o.SetProtocolChannel<HttpServerChannel>();
});
netServer.Options.LogLevel = LogLevel.Info;
netServer.Options.AddLogOutputHandler<LogOutputToConsole>();
netServer.Start();
Console.ReadLine();

public class HttpSession : SesionBase
{
    public override void Receive(NetContext context, object message)
    {
        var request = (HttpRequest)message;
        if (!int.TryParse(request.QueryString, out var count))
        {
            count = 1;
        }
        HttpResponse response = new HttpResponse();
        List<object> data = new List<object>();
        for (int i = 0; i < count; i++)
        {
            data.Add(new { Text = "Hello World", Time = DateTime.Now });
        }
        response.SetJson(data);
        context.Send(response);
    }
}

public class HttpServerChannel : IProtocolChannel<NetContext>
{
    public string Name => "HTTPServer";

    public NetContext Context { get; set; }

    public object Clone()
    {
        HttpServerChannel result = new HttpServerChannel();
        result.Context = Context;
        return result;
    }
    private HttpRequest httpRequest = new HttpRequest();
    public void Decoding(IStreamReader reader, Action<NetContext, object> completed)
    {
        if (httpRequest.Read(reader) == AnalysisStatus.Completed)
        {
            var result = httpRequest;
            httpRequest = new HttpRequest();
            completed(Context, result);
        }
    }
    public void Dispose()
    {
        Context = null;
    }
    public void Encoding(IStreamWriter writer, object message)
    {
        HttpResponse httpResponse = (HttpResponse)message;
        httpResponse.Write(writer);
    }
}

public class HttpClientChannel : IProtocolChannel<NetClient>
{
    public string Name => "HTTPClient";

    public NetClient Context { get; set; }

    public object Clone()
    {
        HttpClientChannel result = new HttpClientChannel();
        result.Context = Context;
        return result;
    }

    private HttpResponse httpResponse = new HttpResponse();

    public void Decoding(IStreamReader reader, Action<NetClient, object> completed)
    {
        if (httpResponse.Read(reader) == AnalysisStatus.Completed)
        {
            var result = httpResponse;
            httpResponse = new HttpResponse();
            completed(Context, result);
        }
    }
    public void Dispose()
    {
        Context = default;
    }

    public void Encoding(IStreamWriter writer, object message)
    {
        HttpRequest request = (HttpRequest)message;
        request.Write(writer);
    }
}

class HttpRequest
{
    public string HttpVersion { get; set; } = "1.1";

    public string Method { get; set; } = "GET";

    public string BaseUrl { get; set; }

    public string ClientIP { get; set; }

    public string Path { get; set; }

    public string QueryString { get; set; }

    public string Url { get; set; } = "/";

    public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();

    public byte[] Body { get; set; }

    public int ContentLength { get; set; }

    protected AnalysisStatus _analysisStatus { get; set; } = AnalysisStatus.None;
    public AnalysisStatus Read(IStreamReader reader)
    {
        LoadRequestLine(reader);
        LoadRequestHeader(reader);
        LoadRequestBody(reader);
        return _analysisStatus;
    }

    public void Write(IStreamWriter writer)
    {
        writer.WriteLine($"{Method} {Url} {HttpVersion}");
        foreach (var item in Headers)
            writer.WriteLine($"{item.Key}: {item.Value}");
        byte[] bodyData = Body;

        if (bodyData != null)
        {
            writer.WriteLine($"Content-Length: {bodyData.Length}");
        }
        writer.WriteLine("");
        if (bodyData != null)
        {
            writer.Write(bodyData, 0, bodyData.Length);
        }
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
                Headers[name] = value.Trim();
            }
        }
    }
    private void LoadRequestBody(IStreamReader reader)
    {
        if (_analysisStatus == AnalysisStatus.LoadingBody)
        {
            if (reader.Length >= ContentLength)
            {
                var data = new byte[ContentLength]; ;
                reader.ReadSequenceNetStream.GetReadOnlySequence().Slice(0, ContentLength)
                    .CopyTo(data);
                reader.ReadSequenceNetStream.ReadAdvance(ContentLength);
                Body = data;
                _analysisStatus = AnalysisStatus.Completed;
            }
        }
    }

}

class HttpResponse
{

    public HttpResponse()
    {
        Headers["Content-Type"] = "text/html";
    }

    public string HttpVersion { get; set; } = "HTTP/1.1";

    public int Status { get; set; } = 200;

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
    public void Write(IStreamWriter writer)
    {
        writer.WriteLine($"{HttpVersion} {Status} {StatusMessage}");
        foreach (var item in Headers)
            writer.WriteLine($"{item.Key}: {item.Value}");
        byte[] bodyData = Body;

        if (bodyData != null)
        {
            writer.WriteLine($"Content-Length: {bodyData.Length}");
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

public enum AnalysisStatus
{
    None,
    LoadingHeader,
    LoadingBody,
    Completed
}

