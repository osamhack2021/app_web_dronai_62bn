using UnityEngine;
using WebSocketSharp;

namespace Dronai.Network
{
    public class NetworkManager : MonoBehaviour
    {
        private WebSocket ws;

        private void Start()
        {
            ws = new WebSocket("ws://ds.linearjun.com");
            ws.OnMessage += (sender, e) =>
            {
                Debug.Log("Message received from " + ((WebSocket)sender).Url + ", Data: " + e.Data);
            };
            ws.Connect();
        }

        private void Update()
        {
            if (ws == null) return;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ws.Send("Hello");
            }
        }
    }
}
