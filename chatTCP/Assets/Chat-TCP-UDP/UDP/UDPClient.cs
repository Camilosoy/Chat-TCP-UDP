using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using UnityEngine;

public class UDPClient : MonoBehaviour
{
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    public bool isServerConnected = false;

    
    public event Action<string> OnMessageReceived;

    
    private readonly ConcurrentQueue<Action> mainThread = new ConcurrentQueue<Action>();

    void Update()
    {
        while (mainThread.TryDequeue(out var a)) a?.Invoke();
    }

    public void StartUDPClient(string ipAddress, int port)
    {
        udpClient = new UdpClient();
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        udpClient.BeginReceive(ReceiveData, null);

    
        SendData("Hello, server!");

        isServerConnected = true;
    }

    private void ReceiveData(IAsyncResult result)
    {
        byte[] receivedBytes = udpClient.EndReceive(result, ref remoteEndPoint);
        string receivedMessage = Encoding.UTF8.GetString(receivedBytes);
        Debug.Log("Received from server: " + receivedMessage);

    
        mainThread.Enqueue(() => OnMessageReceived?.Invoke("Servidor: " + receivedMessage));

        udpClient.BeginReceive(ReceiveData, null);
    }

    public void SendData(string message)
    {
        byte[] sendBytes = Encoding.UTF8.GetBytes(message);
        udpClient.Send(sendBytes, sendBytes.Length, remoteEndPoint);
        Debug.Log("Sent to server: " + message);

    
        mainThread.Enqueue(() => OnMessageReceived?.Invoke("Yo: " + message));
    }
}
