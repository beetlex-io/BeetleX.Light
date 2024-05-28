using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using BeetleX.Light.Logs;
using BeetleX.Light.Protocols;

namespace BeetleX.Light
{
    public class ListenHandler : IDisposable, ILocation
    {
        public ListenHandler(string name)
        {
            Name = name;
            LineEof = Encoding.UTF8.GetBytes("\r\n");
        }
        public int Port { get; set; } = 8089;

        public string Host { get; set; }

        public bool LittleEndian { get; set; } = true;

        public int LineMaxLength { get; set; } = 1024 * 4;

        public byte[] LineEof { get; set; }

        public int? StartRegionPort { get; set; }

        public int? EndRegionPort { get; set; }

        public int MaxProtocolPacketSize { get; set; } = 1024 * 1024;

        public int ReceiveBufferSize { get; set; } = 1024 * 4;

        public string CertificateFile { get; internal set; }

        public SslProtocols SslProtocols { get; internal set; } = SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13;

        public string CertificatePassword { get; internal set; }

        public string Name { get; set; }

        public bool SSL { get; set; }

        internal Socket Socket { get; set; }

        public EndPoint EndPoint { get; set; }

        internal INetServer Server { get; set; }

        public bool IsUnixDomainSocket { get; set; } = false;

        public bool ReuseAddress { get; set; } = false;

        public bool Pause { get; set; } = false;

        public bool UseIPv6 { get; set; } = true;

        public X509Certificate2 Certificate { get; internal set; }

        private bool mIsDisposed = false;

        internal Exception Error { get; set; }

        public bool NoDelay { get; set; } = false;

        public IProtocolChannel<NetContext> ProtocolChannel { get; set; }

        public void SetProtocolChannel<T>()
        where T : IProtocolChannel<NetContext>, new()
        {
            SetProtocolChannel(new T());
        }
        public void SetProtocolChannel(IProtocolChannel<NetContext> protocolChannel)
        {
            ProtocolChannel = protocolChannel;
        }

        public int Backlog { get; set; } = 512;

        public int AcceptErrorRetry { get; set; } = 10;
        EndPoint ILocation.EndPoint { get; set; }

        public void ListenPortRange(int start, int end)
        {
            this.StartRegionPort = start;
            this.EndRegionPort = end;
        }
        public void EnabledSSL(string certificateFile, string certificatePassword, SslProtocols? sslProtocols = null)
        {
            SSL = true;
            CertificateFile = certificateFile;
            CertificatePassword = certificatePassword;
            if (sslProtocols != null)
                SslProtocols = sslProtocols.Value;
        }


        internal Task Run(INetServer server)
        {
            Server = server;
            if (SSL)
            {
                if (string.IsNullOrEmpty(CertificateFile))
                {
                    Server.GetLoger(LogLevel.Error)?.Write(this, "ListenHandler", "Run", $"{Host}@{Port} enabled ssl error certificate file name can not be null!");
                }

                try
                {
                    Certificate = new X509Certificate2(CertificateFile, CertificatePassword);
                    Server.GetLoger(LogLevel.Info)?.Write(this, "ListenHandler", "Run", $"load ssl certificate {Certificate}");
                }
                catch (Exception e_)
                {
                    Error = e_;
                    Server.GetLoger(LogLevel.Error)?.Write(this, "ListenHandler", "Run", $"{Host}@{Port} enabled ssl load certificate file error {e_.Message}!");
                }
            }
            return IPListen();
        }



        private IPAddress MatchIPAddress(string matchIP)
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (ip.ToString().IndexOf(matchIP) == 0)
                    {
                        return ip;
                    }
                }
            }
            throw new Exception($"No {matchIP} IPv4 address in the system!");
        }

        private Task ListenUnixDomainSocket(UnixSocketUri uri)
        {
            EndPoint = new UnixDomainSocketEndPoint(uri.SockFile);
            Socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            Socket.Bind(EndPoint);
            Socket.Listen(Backlog);
            Server.GetLoger(LogLevel.Info)?.Write(this, "ListenHandler", "✔ UnixSocketListen", $"Success");
            return StartAccept();

        }

        private Task IPListen()
        {
            try
            {
                if (StartRegionPort != null)
                {
                    Port = StartRegionPort.Value;
                    if (EndRegionPort == null)
                        EndRegionPort = StartRegionPort + 100;
                }

                var unixSocket = UnixSocketUri.GetUnixSocketUrl(Host);
                if (unixSocket.IsUnixSocket)
                {
                    if (System.IO.File.Exists(unixSocket.SockFile))
                        System.IO.File.Delete(unixSocket.SockFile);
                    IsUnixDomainSocket = true;
                    return ListenUnixDomainSocket(unixSocket);

                }
                System.Net.IPAddress address;
                if (string.IsNullOrEmpty(Host))
                {
                    if (Socket.OSSupportsIPv6 && UseIPv6)
                    {
                        address = IPAddress.IPv6Any;
                    }
                    else
                    {
                        address = IPAddress.Any;
                    }
                }
                else
                {
                    if (Host.EndsWith("*"))
                    {
                        address = MatchIPAddress(Host.Replace("*", ""));
                        Host = address.ToString();
                    }
                    else
                    {
                        address = System.Net.IPAddress.Parse(Host);
                    }
                }
                NEXT_PORT:
                var ipaddress = new System.Net.IPEndPoint(address, Port);
                EndPoint = ipaddress;
                Socket = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (ipaddress.Address == IPAddress.IPv6Any)
                {
                    Socket.DualMode = true;
                }
                if (this.ReuseAddress)
                {
                    Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                }
                try
                {
                    Socket.Bind(EndPoint);
                }
                catch (SocketException socket_err)
                {
                    if (socket_err.SocketErrorCode != SocketError.AddressAlreadyInUse)
                        throw socket_err;
                    if (StartRegionPort == null)
                        throw socket_err;
                    if (StartRegionPort < EndRegionPort)
                    {
                        Server.GetLoger(LogLevel.Info)?.Write(this, "ListenHandler", "IPSocket", $"{Host}@{Port} address already in use,continue listen ...");
                        StartRegionPort++;
                        Port = StartRegionPort.Value;
                        Socket?.Dispose();
                        goto NEXT_PORT;
                    }
                    else
                    {
                        throw socket_err;
                    }
                }

                Socket.Listen(Backlog);
                Server.GetLoger(LogLevel.Info)?.Write(this, "ListenHandler", "IPSocket", $"listen {Host}@{Port} success ssl:{SSL}");
                return StartAccept();

            }
            catch (Exception e_)
            {
                Error = e_;
                Server.GetLoger(LogLevel.Error)?.WriteException(this, "ListenHandler", "IPSocket", e_);
            }
            return Task.CompletedTask;
        }

        private int mAccetpError = 0;

        private async Task OnPause()
        {
            while (Pause)
                await Task.Delay(2000);
        }

        private async Task StartAccept()
        {
            while (true)
            {
                try
                {
                    await OnPause();
                    var acceptSocket = await Socket.AcceptAsync();
                    acceptSocket.NoDelay = NoDelay;
                    Server.SocketConnecting(acceptSocket, this);
                    mAccetpError = 0;
                }
                catch (Exception e_)
                {
                    if (mIsDisposed)
                        break;
                    Error = e_;
                    mAccetpError++;
                    Server.GetLoger(LogLevel.Error)?.WriteException(this, "ListenHandler", "AcceptSocket", e_);
                    if (mAccetpError >= AcceptErrorRetry)
                    {
                        Server.GetLoger(LogLevel.Warring)?.Write(this, "ListenHandler", "AcceptSocket", "Stoped!");
                        break;
                    }
                }
            }
        }


        public override string ToString()
        {
            if (!IsUnixDomainSocket)
                return $"[ TCP socket]Listen {Host}:{Port}\t[TLS:{SSL}]\t[Status:{(Error == null ? "success" : $"error {Error.Message}")}]";
            else
                return $"[Unix socket]Listen {Host}\t[TLS:{SSL}]\t[Status:{(Error == null ? "success" : $"error {Error.Message}")}]";
        }


        public void Dispose()
        {
            try
            {
                CloseSocket(Socket);
            }
            catch
            {

            }
            finally
            {
                mIsDisposed = true;
            }
        }

        internal static void CloseSocket(System.Net.Sockets.Socket socket)
        {

            try
            {
                socket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
            }
            catch
            {
            }
            try
            {
                socket.Dispose();

            }
            catch
            {

            }

        }
    }
}
