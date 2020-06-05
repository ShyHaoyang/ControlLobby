using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    public Dictionary<string, string> Clients = new Dictionary<string, string>();
    private Queue<string> replyQueue = new Queue<string>();

    void Start()
    {
        UDPManager.Instance.SetUp("127.0.0.1", 9090, 8080);
        UDPManager.Instance.Run();
        UDPManager.Instance.onReply = OnReply;
    }

    void Update()
    {
        
    }

    private void OnDestroy()
    {
        UDPManager.Instance.Stop();
    }

    void OnReply(byte[] _reply, string _address, int _port)
    {
        string reply = System.Text.Encoding.UTF8.GetString(_reply);
        replyQueue.Enqueue(reply);
    }
}
