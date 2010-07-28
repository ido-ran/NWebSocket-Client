var count = 0;
var loop = 0;
var websocket = null;
var name  = null;

var app = {
    url: 'ws://localhost:8080/YASO/chat',
    initialize: function() {
        if ("WebSocket" in window) {
            $('login-name').focus();
            app.listen();
        } else {
            $('missing-sockets').style.display = 'inherit';
            $('login-name').style.display = 'none';
            $('login-button').style.display = 'none';
            $('display').style.display = 'none';
        }
    },
    listen: function() {
        $('websockets-frame').src = app.url + '?' + count;
        count ++;
    },
    login: function() {
        name = $F('login-name');
        if (! name.length > 0) {
            $('system-message').style.color = 'red';
            $('login-name').focus();
            return;
        }
        $('system-message').style.color = '#2d2b3d';
        $('system-message').innerHTML = name + ':';

        $('login-button').disabled = true;
        $('login-form').style.display = 'none';
        $('message-form').style.display = '';

            var p = document.createElement('p');
            p.innerHTML = new Date() + 'connecting to ' + app.url;
            $('display').appendChild(p);

        app.connectToServer();
    },
    connectToServer: function() {
        websocket = new WebSocket(app.url);
        websocket.name = name;
        websocket.onopen = function() {
            // Web Socket is connected. You can send data by send() method
            websocket.send('login:' + name);
        };
        websocket.onmessage = function (evt) {
            eval(evt.data);
            $('message').disabled = false;
            $('post-button').disabled = false;
            $('message').focus();
            $('message').value = '';
        };
        websocket.onclose = function() {
            var p = document.createElement('p');
            p.innerHTML = new Date() + " - The socket has been closed";
            $('display').appendChild(p);

            new Fx.Scroll('display').down();
            
            connectToServer();
        };
    },
    post: function() {
        var message = $F('message');
        if (!message > 0) {
            return;
        }
        $('message').disabled = true;
        $('post-button').disabled = true;

        websocket.send(message);
    },
    update: function(data) {
        if (data) {
            var p = document.createElement('p');
            p.innerHTML = new Date() + "<b>" + data.name + "</b>:" + data.message;

            $('display').appendChild(p);

            new Fx.Scroll('display').down();
        }
    }
};

var rules = {
    '#login-name': function(elem) {
        Event.observe(elem, 'keydown', function(e) {
            if (e.keyCode == 13) {
                $('login-button').focus();
            }
        });
    },
    '#login-button': function(elem) {
        elem.onclick = app.login;
    },
    '#message': function(elem) {
        Event.observe(elem, 'keydown', function(e) {
            if (e.shiftKey && e.keyCode == 13) {
                $('post-button').focus();
            }
        });
    },
    '#post-button': function(elem) {
        elem.onclick = app.post;
    }
};
Behaviour.addLoadEvent(app.initialize);
Behaviour.register(rules);
