import 'dotenv/config';

import http from 'http';
import WebSocket from 'ws';
import server from './server';

const { PORT } = process.env;


let exs = http.createServer({}, server).listen(PORT, () => {
  console.log(`Server is listening on port ${PORT}`);
});

const wss = new WebSocket.Server({ server: exs });

wss.on('connection', function connections(ws) {
  console.log("A new client connected");
  ws.send("Welcome to dronai server");
  
  ws.on('message', function incoming(message){
    console.log('received: %s', message);
    ws.send("수신 옝호~");
  });
});