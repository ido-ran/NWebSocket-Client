using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Collections.Specialized;
using System.Net;

namespace NWebSocketLib {
  public class WebSocket {

    private Uri uri;
    private Socket socket;
    private bool handshakeComplete;
    private NetworkStream networkStream;
    private StreamReader inputStream;
    private StreamWriter outputStream;
    private Dictionary<string,string> headers;

    /// <summary>
    /// Creates a new WebSocket targeting the specified URL.
    /// </summary>
    /// <param name="uri"></param>
    public WebSocket(Uri uri) {
      this.uri = uri;
      string protocol = uri.Scheme;
      if (!protocol.Equals("ws") && !protocol.Equals("wss")) {
        throw new ArgumentException("Unsupported protocol: " + protocol);
      }
      headers = new Dictionary<string, string>();
    }

    /// <summary>
    /// Extra headers to be sent
    /// </summary>
    public Dictionary<string, string> Headers {
      get { return headers; }
    }

    /// <summary>
    /// Establishes the connection
    /// </summary>
    public void Connect() {
      string host = uri.Host;
      StringBuilder path = new StringBuilder(uri.AbsolutePath);
      if (path.Length == 0) {
        path.Append('/');
      }

      string query = uri.Query;
      if (!string.IsNullOrEmpty(query)) {
        path.Append("?");
        path.Append(query);
      }

      string origin = "http://" + host;

      socket = CreateSocket();
      IPEndPoint localEndPoint = (IPEndPoint)socket.LocalEndPoint;
      int port = localEndPoint.Port;
      if (port != 80) {
        host = host + ":" + port;
      }

      networkStream = new NetworkStream(socket);
      outputStream = new StreamWriter(networkStream, Encoding.UTF8);
      StringBuilder extraHeaders = new StringBuilder();
      foreach (var headerEntry in headers) {
        extraHeaders.AppendFormat("{0}: {1}\r\n", headerEntry.Key, headerEntry.Value);
      }

      string request = string.Format(
         "GET {0} HTTP/1.1\r\n" +
         "Upgrade: WebSocket\r\n" +
         "Connection: Upgrade\r\n" +
         "Host: {1}\r\n" +
         "Origin: {2}\r\n" +
         "{3}" +
         "\r\n",
         path, host, origin, extraHeaders);

      byte[] encodedHandshake = Encoding.UTF8.GetBytes(request);
      networkStream.Write(encodedHandshake, 0, encodedHandshake.Length);
      networkStream.Flush();

      inputStream = new StreamReader(networkStream);
      string header = inputStream.ReadLine();
      if (!header.Equals("HTTP/1.1 101 Web Socket Protocol Handshake")) {
        throw new InvalidOperationException("Invalid handshake response");
      }
      
      header = inputStream.ReadLine();
      if (!header.Equals("Upgrade: WebSocket")) {
        throw new InvalidOperationException("Invalid handshake response");
      }

      header = inputStream.ReadLine();
      if (!header.Equals("Connection: Upgrade")) {
        throw new InvalidOperationException("Invalid handshake response");
      }

      // Ignore any further response
      do {
        header = inputStream.ReadLine();
      } while (!header.Equals(""));

      handshakeComplete = true;
    }

    /// <summary>
    /// Sends the specified string as a data frame.
    /// </summary>
    /// <param name="payload"></param>
    public void Send(string payload) {
      DemandHandshake();

      networkStream.WriteByte(0x00);
      byte[] encodedPayload = Encoding.UTF8.GetBytes(payload);
      networkStream.Write(encodedPayload, 0, encodedPayload.Length);
      networkStream.WriteByte(0xFF);
      networkStream.Flush();
    }

    /// <summary>
    /// Receives the next data frame.
    /// </summary>
    /// <returns></returns>
    public string Receive() {
      DemandHandshake();

      List<byte> buf = new List<byte>();

      int b = networkStream.ReadByte();
      if ((b & 0x80) == 0x80) {
        // Skip data frame
        int len = 0;
        do {
          b = networkStream.ReadByte() & 0x7f;
          len = b * 128 + len;
        } while ((b & 0x80) != 0x80);

        for (int i = 0; i < len; i++) {
          networkStream.ReadByte();
        }
      }

      while (true) {
        b = networkStream.ReadByte();
        if (b == 0xff) {
          break;
        }

        buf.Add((byte)b);
      }

      string decodedString = Encoding.UTF8.GetString(buf.ToArray());
      return decodedString;
    }

    /// <summary>
    /// Closes the socket.
    /// </summary>
    public void Close() {
      if (handshakeComplete) {
        networkStream.WriteByte(0xFF);
        networkStream.WriteByte(0x00);
        networkStream.Flush();
      }

      socket.Close();
    }

    private void DemandHandshake() {
      if (!handshakeComplete) {
        throw new InvalidOperationException("Handshake not complete yet");
      }
    }

    private Socket CreateSocket() {
      string scheme = uri.Scheme;
      string host = uri.Host;

      int port = uri.Port;
      if (port == -1) {
        if (scheme.Equals("wss")) {
          port = 443;
        }
        else if (scheme.Equals("ws")) {
          port = 80;
        }
        else {
          throw new ArgumentException("Unsupported scheme");
        }
      }

      if (scheme.Equals("wss")) {
        throw new NotSupportedException("Not support secure WebSocket yet");
        //SocketFactory factory = SSLSocketFactory.getDefault();
        //return factory.createSocket(host, port);
      }
      else {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(host, port);
        return socket;
      }
    }
  }
}
