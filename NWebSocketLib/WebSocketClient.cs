using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Collections.Specialized;
using System.Net;
using WebSocketServer;

namespace NWebSocketLib {
  /// <summary>
  /// WebSocket client.
  /// </summary>
  /// <remarks>
  /// This class design to mimic JavaScript's WebSocket class.
  /// It handle the handshake and framing of the data and provide MessageReceived event to handle messages.
  /// Please note that this event is raised on a arbitrary pooled background thread.
  /// </remarks>
  public class WebSocketClient {

    private Uri uri;
    private Socket socket;
    private bool handshakeComplete;
    private NetworkStream networkStream;
    private StreamReader inputStream;
    private StreamWriter outputStream;
    private WebSocketConnection connection;
    private Dictionary<string,string> headers;

    public event EventHandler<DataReceivedEventArgs> OnMessage;
    public event EventHandler OnError;
    public event EventHandler OnClose;

    /// <summary>
    /// Creates a new WebSocket targeting the specified URL.
    /// </summary>
    /// <param name="uri"></param>
    public WebSocketClient(Uri uri) {
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

      connection = new WebSocketConnection(socket);
      connection.DataReceived += new DataReceivedEventHandler(DataReceivedHandler);
      connection.Disconnected += new WebSocketDisconnectedEventHandler(DisconnectedHandler);
      connection.Error += new EventHandler(ErrorHandler);
    }

    private void DataReceivedHandler(WebSocketConnection sender, DataReceivedEventArgs e) {
      FireOnMessage(e);
    }

    private void ErrorHandler(object sender, EventArgs e) {
      FireOnError();
      Close();
    }

    private void DisconnectedHandler(WebSocketConnection sender, EventArgs e) {
      handshakeComplete = false;
    }

    protected virtual void FireOnMessage(DataReceivedEventArgs e) {
      var h = OnMessage;
      if (h != null) h(this, e);
    }

    protected virtual void FireOnError() {
      var h = OnError;
      if (h != null) h(this, EventArgs.Empty);
    }

    protected virtual void FireOnClose() {
      var h = OnClose;
      if (h != null) h(this, EventArgs.Empty);
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
    /// Closes the socket.
    /// </summary>
    public void Close() {
      if (handshakeComplete) {
        try {
          networkStream.WriteByte(0xFF);
          networkStream.WriteByte(0x00);
          networkStream.Flush();
        }
        catch {
          // Ignore any excption during close handshake.
        }
      }

      connection.Close();
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
