using System;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace WebSocketServer {
  /// <summary>
  /// This class was downloaded from http://nugget.codeplex.com/
  /// 
  /// Copyright (c) 2010 
  ///Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
  ///The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
  /// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
  /// </summary>
  public class DataReceivedEventArgs : EventArgs {
    public int Size { get; private set; }
    public string Data { get; private set; }
    public DataReceivedEventArgs(int size, string data) {
      Size = size;
      Data = data;
    }
  }

  public delegate void DataReceivedEventHandler(WebSocketConnection sender, DataReceivedEventArgs e);
  public delegate void WebSocketDisconnectedEventHandler(WebSocketConnection sender, EventArgs e);

  public class WebSocketConnection : IDisposable {

    #region Private members
    private byte[] dataBuffer;                                  // buffer to hold the data we are reading
    bool readingData;                                           // are we in the proccess of reading data or not
    private StringBuilder dataString;                           // holds the currently accumulated data
    private enum WrapperBytes : byte { Start = 0, End = 255 };  // data passed between client and server are wrapped in start and end bytes according to the protocol (0x00, 0xFF)
    #endregion

    /// <summary>
    /// An event that is triggered whenever the connection has read some data from the client
    /// </summary>
    public event DataReceivedEventHandler DataReceived;

    public event WebSocketDisconnectedEventHandler Disconnected;

    public event EventHandler Error;

    /// <summary>
    /// Guid for the connection - thouhgt it might be usable in some way
    /// </summary>
    public System.Guid GUID { get; private set; }

    /// <summary>
    /// Gets the socket used for the connection
    /// </summary>
    public Socket ConnectionSocket { get; private set; }

    private bool isClosed;

    #region Constructors
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="connection">The socket on which to esablish the connection</param>
    /// <param name="webSocketOrigin">The origin from which the server is willing to accept connections, usually this is your web server. For example: http://localhost:8080.</param>
    /// <param name="webSocketLocation">The location of the web socket server (the server on which this code is running). For example: ws://localhost:8181/service. The '/service'-part is important it could be '/somethingelse' but it needs to be there.</param>
    public WebSocketConnection(Socket socket)
      : this(socket, 255) {

    }

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="connection">The socket on which to esablish the connection</param>
    /// <param name="webSocketOrigin">The origin from which the server is willing to accept connections, usually this is your web server. For example: http://localhost:8080.</param>
    /// <param name="webSocketLocation">The location of the web socket server (the server on which this code is running). For example: ws://localhost:8181/service. The '/service'-part is important it could be '/somethingelse' but it needs to be there.</param>
    /// <param name="bufferSize">The size of the buffer used to receive data</param>
    public WebSocketConnection(Socket socket, int bufferSize) {
      ConnectionSocket = socket;
      dataBuffer = new byte[bufferSize];
      dataString = new StringBuilder();
      GUID = System.Guid.NewGuid();
      Listen();
    }
    #endregion

    /// <summary>
    /// Invoke the DataReceived event, called whenever the client has finished sending data.
    /// </summary>
    protected virtual void OnDataReceived(DataReceivedEventArgs e) {
      if (DataReceived != null)
        DataReceived(this, e);
    }

    /// <summary>
    /// Listens for incomming data
    /// </summary>
    private void Listen() {
      try {
        ConnectionSocket.BeginReceive(dataBuffer, 0, dataBuffer.Length, 0, Read, null);
      }
      catch (SocketException) {
        Close();
      }
    }

    /// <summary>
    /// Send a string to the client
    /// </summary>
    /// <param name="str">the string to send to the client</param>
    public void Send(string str) {
      if (ConnectionSocket.Connected) {
        try {
          // start with a 0x00
          ConnectionSocket.Send(new byte[] { (byte)WrapperBytes.Start }, 1, 0);
          // send the string
          ConnectionSocket.Send(Encoding.UTF8.GetBytes(str));
          /*
          writer.Write(str);
          writer.Flush();*/

          // end with a 0xFF
          ConnectionSocket.Send(new byte[] { (byte)WrapperBytes.End }, 1, 0);
        }
        catch (Exception ex) {
          OnError(ex);
        }
      }
    }

    /// <summary>
    /// reads the incomming data and triggers the DataReceived event when done
    /// </summary>
    private void Read(IAsyncResult ar) {
      if (isClosed) {
        return;
      }

      int sizeOfReceivedData = -1;
      try {
        sizeOfReceivedData = ConnectionSocket.EndReceive(ar);
      }
      catch (SocketException se) {
        // TODO: log
        OnError(se);
        return;
      }
      if (sizeOfReceivedData > 0) {
        int start = 0, end = dataBuffer.Length - 1;

        // if we are not already reading something, look for the start byte as specified in the protocol
        if (!readingData) {
          for (start = 0; start < dataBuffer.Length - 1; start++) {
            if (dataBuffer[start] == (byte)WrapperBytes.Start) {
              readingData = true; // we found the begining and can now start reading
              start++; // we dont need the start byte. Incrementing the start counter will walk us past it
              break;
            }
          }
        } // no else here, the value of readingData might have changed

        // if a begining was found in the buffer, or if we are continuing from another buffer
        if (readingData) {
          bool endIsInThisBuffer = false;
          // look for the end byte in the received data
          for (end = start; end < sizeOfReceivedData; end++) {
            byte currentByte = dataBuffer[end];
            if (dataBuffer[end] == (byte)WrapperBytes.End) {
              endIsInThisBuffer = true; // we found the ending byte
              break;
            }
          }

          // the end is in this buffer, which means that we are done reading
          if (endIsInThisBuffer) {
            // we are no longer reading data
            readingData = false;
            // put the data into the string builder
            dataString.Append(Encoding.UTF8.GetString(dataBuffer, start, end - start));
            // trigger the event
            int size = Encoding.UTF8.GetBytes(dataString.ToString().ToCharArray()).Length;
            OnDataReceived(new DataReceivedEventArgs(size, dataString.ToString()));
            dataString = null;
            dataString = new StringBuilder();
          }
          else // if the end is not in this buffer then put everyting from start to the end of the buffer into the datastring and keep on reading
                    {
            dataString.Append(Encoding.UTF8.GetString(dataBuffer, start, end - start));
          }
        }

        // continue listening for more data
        Listen();
      }
      else // the socket is closed
            {
        OnDisconnected();
      }
    }

    private void OnDisconnected() {
      if (Disconnected != null)
        Disconnected(this, EventArgs.Empty);
    }

    private void OnError(Exception ex) {
      if (Error != null) {
        Error(this, EventArgs.Empty);
      }
    }

    #region cleanup
    /// <summary>
    /// Closes the socket
    /// </summary>
    public void Close() {
      isClosed = true;
      ConnectionSocket.Close();
    }

    /// <summary>
    /// Closes the socket
    /// </summary>
    public void Dispose() {
      Close();
    }
    #endregion
  }
}
