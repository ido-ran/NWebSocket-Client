using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NWebSocketLib.Test {
  /// <summary>
  /// Summary description for WebSocketTest
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
    // [TestInitialize()]
    // public void MyTestInitialize() { }
    //
    // Use TestCleanup to run code after each test has run
    // [TestCleanup()]
    // public void MyTestCleanup() { }
    //
    #endregion

    [TestMethod]
    public void Connect() {
      WebSocket ws = new WebSocket(new Uri("ws://localhost:9080/WebSocketChat/chat"));
      ws.Connect();
      ws.Close();
    }

    [TestMethod]
    public void Send() {
      WebSocket ws = new WebSocket(new Uri("ws://localhost:9080/WebSocketChat/chat"));
      ws.Connect();
      ws.Send("User1: Hello Sir");
      ws.Close();
    }

    [TestMethod]
    public void Receive() {
      WebSocket ws = new WebSocket(new Uri("ws://localhost:9080/WebSocketChat/chat"));
      ws.Connect();
      string msg = ws.Receive();
      Assert.IsNotNull(msg);
      ws.Close();
    }
  }
}
