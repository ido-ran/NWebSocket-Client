/*
 * This file is part of D.N.A. Aurora Project (dna-7.com)
 * Copyright (c) All Rights Reserved 2009-2010
 * DNA Team
 */
package org.dotdotnet.websockets;

import com.sun.grizzly.websockets.BaseServerWebSocket;
import com.sun.grizzly.websockets.NetworkHandler;
import com.sun.grizzly.websockets.WebSocketListener;
import java.io.IOException;

/**
 *
 * @author Ido
 */
public class ChatWebSocket extends BaseServerWebSocket {

  private String user;

  public ChatWebSocket(NetworkHandler handler, WebSocketListener... listeners) {
    super(handler, listeners);
  }

  public String getUser() {
    return user;
  }

  public void setUser(String user) {
    this.user = user;
  }

  private String toJsonp(String name, String message) {
    String script = "window.parent.app.update({ name: \"" + escape(name) + "\", message: \"" + escape(message) + "\" });\n";
    return script;
  }

  private String escape(String orig) {
    StringBuilder buffer = new StringBuilder(orig.length());

    for (int i = 0; i < orig.length(); i++) {
      char c = orig.charAt(i);
      switch (c) {
        case '\b':
          buffer.append("\\b");
          break;
        case '\f':
          buffer.append("\\f");
          break;
        case '\n':
          buffer.append("<br />");
          break;
        case '\r':
          // ignore
          break;
        case '\t':
          buffer.append("\\t");
          break;
        case '\'':
          buffer.append("\\'");
          break;
        case '\"':
          buffer.append("\\\"");
          break;
        case '\\':
          buffer.append("\\\\");
          break;
        case '<':
          buffer.append("&lt;");
          break;
        case '>':
          buffer.append("&gt;");
          break;
        case '&':
          buffer.append("&amp;");
          break;
        default:
          buffer.append(c);
      }
    }

    return buffer.toString();
  }

  void sendMessage(String owner, String msg) throws IOException {
    send(toJsonp(owner, msg));
  }

}
