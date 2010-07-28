/*
 * This file is part of D.N.A. Aurora Project (dna-7.com)
 * Copyright (c) All Rights Reserved 2009-2010
 * DNA Team
 */
package org.dotdotnet.websockets;

import com.sun.grizzly.websockets.DataFrame;
import com.sun.grizzly.websockets.NetworkHandler;
import com.sun.grizzly.websockets.WebSocket;
import com.sun.grizzly.websockets.WebSocketApplication;
import com.sun.grizzly.websockets.WebSocketListener;
import java.io.IOException;
import java.text.MessageFormat;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 *
 * @author Ido
 */
public class ChatApplication extends WebSocketApplication {

  @Override
  public WebSocket createSocket(NetworkHandler handler, WebSocketListener... listeners) throws IOException {
    ChatWebSocket ws = new ChatWebSocket(handler, listeners);
    return ws;
  }

  @Override
  public void onMessage(WebSocket socket, DataFrame data) throws IOException {
    super.onMessage(socket, data);
    
    ChatWebSocket chatws = (ChatWebSocket) socket;
    final String msg = data.getTextPayload();
    if (msg.startsWith("login:")) {
      login(chatws, msg);
    } else {
      broadcast(chatws.getUser(), msg);
    }
  }

  private void login(ChatWebSocket chatWebSocket, String msg) {
    if (chatWebSocket.getUser() == null) {
      chatWebSocket.setUser(msg.split(":")[1].trim());
      broadcast("room", chatWebSocket.getUser() + " has join the room");
    }
  }
  
  private void broadcast(String owner, String msg) {
    for (WebSocket currSocket : getWebSockets()) {
      try {
        ChatWebSocket chatws = (ChatWebSocket) currSocket;
        chatws.sendMessage(owner, msg);
      } catch (IOException ex) {
        Logger.getLogger(ChatApplication.class.getName()).log(Level.SEVERE, null, ex);
        remove(currSocket);
      }
    }
  }

  @Override
  public void onClose(WebSocket socket) throws IOException {
    super.onClose(socket);
    ChatWebSocket chatws = (ChatWebSocket) socket;
    ChatWebSocketServlet.logger.log(Level.INFO, "{0} socket was closed", chatws.getUser());
    broadcast("room", chatws.getUser() + "has left the room");
  }

}
