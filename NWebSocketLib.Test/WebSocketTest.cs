using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace NWebSocketLib.Test {
  /// <summary>
  /// Summary description for WebSocketTest.
  /// 
  /// The test curently relaying on WebSocketServer sample application WebSocketChatServer.
  /// </summary>
  [TestClass]
  public class WebSocketTest {
    public WebSocketTest() {
      //
      // TODO: Add constructor logic here
      //
    }

    private TestContext testContextInstance;

    /// <summary>
    ///Gets or sets the test context which provides
    ///information about and functionality for the current test run.
    ///</summary>
    public TestContext TestContext {
      get {
        return testContextInstance;
      }
      set {
        testContextInstance = value;
      }
    }

    //private WebSocketServer.WebSocketServer wss;

    #region Additional test attributes
    //
    // You can use the following additional attributes as you write your tests:
    //
    // Use ClassInitialize to run code before running the first test in the class
    // [ClassInitialize()]
    // public static void MyClassInitialize(TestContext testContext) { }
    //
    // Use ClassCleanup to run code after all tests in a class have run
    // [ClassCleanup()]
    // public static void MyClassCleanup() { }
    //
    // Use TestInitialize to run code before running each test 
    [TestInitialize()]
    public void MyTestInitialize() {
      //wss = new WebSocketServer.WebSocketServer(8181, "http://localhost:8080", "ws://localhost:8080/chat");
      //wss.ClientConnected += new WebSocketServer.ClientConnectedEventHandler(wss_ClientConnected);
      //wss.Start();
    }

    //private void wss_ClientConnected(WebSocketServer.WebSocketConnection sender, EventArgs e) {
    //  sender.DataReceived += new WebSocketServer.DataReceivedEventHandler(Client_DataReceived);
    //}

    //void Client_DataReceived(WebSocketServer.WebSocketConnection sender, WebSocketServer.DataReceivedEventArgs e) {
    //  // Echo
    //  sender.Send(e.Data);
    //}

    //
    // Use TestCleanup to run code after each test has run
    [TestCleanup()]
    public void MyTestCleanup() {
      //wss.Stop();
    }
    //
    #endregion

    [TestMethod]
    public void Connect() {
      WebSocketClient ws = new WebSocketClient(new Uri("ws://localhost:8080/YASO/chat"));
      ws.Connect();
      ws.Close();
    }

    [TestMethod]
    public void Send() {
      WebSocketClient ws = new WebSocketClient(new Uri("ws://localhost:8080/YASO/chat"));
      ws.Connect();
      ws.Send("login: User1");
      ws.Close();
    }

    [TestMethod]
    public void Receive() {
      WebSocketClient ws = new WebSocketClient(new Uri("ws://localhost:8080/YASO/chat"));
      
      ManualResetEvent receiveEvent = new ManualResetEvent(false);
      ws.OnMessage += (s, e) =>
      {
        receiveEvent.Set();
      };

      ws.Connect();
      ws.Send("login: User1");

      bool received = receiveEvent.WaitOne(TimeSpan.FromSeconds(2));
      Assert.IsTrue(received);

      receiveEvent.Close();
      ws.Close();
    }

    [TestMethod]
    public void SendAndRecieve() {
      WebSocketClient ws = new WebSocketClient(new Uri("ws://localhost:8080/YASO/chat"));

      ManualResetEvent receiveEvent = new ManualResetEvent(false);
      ws.OnMessage += (s, e) =>
      {
        receiveEvent.Set();
      };

      ws.Connect();
      ws.Send("login: " + DateTime.Now.Second);
      bool received = receiveEvent.WaitOne(TimeSpan.FromSeconds(2));
      Assert.IsTrue(received);

      for (int i = 0; i < 10; i++) {
        receiveEvent.Reset();
        ws.Send("Msg #" + i);
        received = receiveEvent.WaitOne(TimeSpan.FromSeconds(2));
        Assert.IsTrue(received, "Fail on round " + i);
      }
      
      receiveEvent.Close();

      ws.Close();
    }

  }
}
