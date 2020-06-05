using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

class UDPManager
{
    private static UDPManager instance = null;
    private static object padlock = new object();

    public delegate void OnReplyDelegate(byte[] _reply, string _address, int _port);
    public delegate void OnExceptionDelegate(System.Exception _ex);

    public OnReplyDelegate onReply = null;
    public OnExceptionDelegate onException = null;

    private string remoteHost;
    private int remotePort;
    private int localPort;

    private bool running = false;
    private UdpClient udpClient = null;
    private IPEndPoint remotEP = null;
    private Queue<byte[]> outPool = new Queue<byte[]>();

    public static UDPManager Instance
    {
        get
        {
            lock (padlock)
            {
                if (instance == null)
                    instance = new UDPManager();
            }

            return instance;
        }
    }

    public void SetUp(string _remoteHost, int _remotePort, int _localPort = 0)
    {
        remoteHost = _remoteHost;
        remotePort = _remotePort;
        localPort = _localPort;

        remotEP = new IPEndPoint(IPAddress.Parse(remoteHost), remotePort);
        //0 代表自动分配端口
        udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, localPort));
    }

    public void Run()
    {
        if (!running)
        {
            running = true;

            Thread receiverThread = new Thread(runReceiver);
            receiverThread.Start();
            Debug.Log("UDPManager::receiver - thread running");
            Thread senderThread = new Thread(runSender);
            senderThread.Start();
            Debug.Log("UDPManager::sender - thread running");
        }
    }

    public void Stop()
    {
        if (running)
        {
            running = false;
            udpClient.Close();
            udpClient = null;
        }
    }

    private void runSender()
    {
        while (running)
        {
            if (null == udpClient)
                break;
            while (outPool.Count != 0)
            {
                try
                {
                    byte[] buf = outPool.Dequeue();
                    if (null == buf)
                        continue;
                    udpClient.Send(buf, buf.Length, remotEP);
                }
                catch (System.Exception _ex)
                {
                    if (null != onException)
                        onException(_ex);
                }
            }
            Thread.Sleep(50);
        }
        Debug.Log("UDPManager::sender - thread exit");
    }

    private void runReceiver()
    {
        IPEndPoint senderEP = new IPEndPoint(IPAddress.Any, 0);
        while (running)
        {
            if (null == udpClient)
                break;
            try
            {
                if (udpClient.Available <= 0)
                    continue;
                byte[] buf = udpClient.Receive(ref senderEP);
                if (null == buf)
                    continue;
                if (null != onReply)
                    onReply(buf, senderEP.Address.ToString(), senderEP.Port);
            }
            catch (System.Exception _ex)
            {
                if (null != onException)
                    onException(_ex);
            }
        }
        Debug.Log("UDPManager::receiver - thread exit");
    }

    public void Report(string _data)
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes(_data);
        outPool.Enqueue(data);
    }

    public void Report(byte[] _data)
    {
        outPool.Enqueue(_data);
    }
}