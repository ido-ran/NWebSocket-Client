/*
 * This file is part of D.N.A. Aurora Project (dna-7.com)
 * Copyright (c) All Rights Reserved 2009-2010
 * DNA Team
 */

package org.dotdotnet.websockets;

import com.sun.grizzly.websockets.WebSocketEngine;
import java.io.IOException;
import java.util.logging.Logger;
import javax.servlet.ServletConfig;
import javax.servlet.ServletException;
import javax.servlet.http.HttpServlet;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

/**
 *
 * @author Ido
 */
public class ChatWebSocketServlet extends HttpServlet {
   
   static final Logger logger = Logger.getLogger(WebSocketEngine.WEBSOCKET);
    private final ChatApplication app = new ChatApplication();

    @Override
    public void init(ServletConfig config) throws ServletException {
        WebSocketEngine.getEngine().register(config.getServletContext().getContextPath() + "/chat", app);
    }

  @Override
  protected void doGet(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
    //super.doGet(req, resp);
    resp.getWriter().print("Welcome to us");
  }
    
    

}
