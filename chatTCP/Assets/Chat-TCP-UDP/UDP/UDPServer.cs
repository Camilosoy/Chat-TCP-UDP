using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using UnityEngine;

public class UDPServer : MonoBehaviour
{
    private UdpClient udpServer;
    private IPEndPoint remoteEndPoint;

    public bool isServerRunning = false;

    // >>> NUEVO
    public event Action<string> OnMessageReceived;
    private readonly ConcurrentQueue<Action> mainThread = new ConcurrentQueue<Action>();

    void Update()
    {
        while (mainThread.TryDequeue(out var a)) a?.Invoke();
    }

    public void StartUDPServer(int port)
    {
        udpServer = new UdpClient(port);
        remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
        Debug.Log("Server started. Waiting for messages...");
        udpServer.BeginReceive(ReceiveData, null);
        isServerRunning = true;

    
        mainThread.Enqueue(() => OnMessageReceived?.Invoke("Servidor UDP listo en puerto " + port));
    }

    private void ReceiveData(IAsyncResult result)
    {
        byte[] receivedBytes = udpServer.EndReceive(result, ref remoteEndPoint);
        string receivedMessage = Encoding.UTF8.GetString(receivedBytes);
        Debug.Log("Received from client: " + receivedMessage);

    
        mainThread.Enqueue(() => OnMessageReceived?.Invoke("Cliente: " + receivedMessage));

        udpServer.BeginReceive(ReceiveData, null);
    }

    public void SendData(string message)
    {
        byte[] sendBytes = Encoding.UTF8.GetBytes(message);
        udpServer.Send(sendBytes, sendBytes.Length, remoteEndPoint);
        Debug.Log("Sent to client: " + message);

    
        mainThread.Enqueue(() => OnMessageReceived?.Invoke("Yo: " + message));
    }
}
