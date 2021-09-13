using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;


public class NetworkManager : MonoBehaviour
{
    WebSocket ws;

    private void Start() {
        ws = new WebSocket("ws://localhost:5000");
        ws.OnMessage += (sender, e) =>
        {
            Debug.Log("Message received from " + ((WebSocket)sender).Url + ", Data: " + e.Data);
        };
        ws.Connect();
    }

    private void Update() {
        if(ws == null) return;
        if(Input.GetKeyDown(KeyCode.Space))
        {
            ws.Send("Hello");
        }
    }
}
