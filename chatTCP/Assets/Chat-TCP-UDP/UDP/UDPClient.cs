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

    // >>> NUEVO: evento para que la UI muestre mensajes
    public event Action<string> OnMessageReceived;

    // >>> NUEVO: cola para ejecutar en el hilo principal (UI)
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

        // Primer “hola” para que el server conozca nuestro endpoint
        SendData("Hello, server!");

        isServerConnected = true;
    }

    private void ReceiveData(IAsyncResult result)
    {
        byte[] receivedBytes = udpClient.EndReceive(result, ref remoteEndPoint);
        string receivedMessage = Encoding.UTF8.GetString(receivedBytes);
        Debug.Log("Received from server: " + receivedMessage);

        // >>> NUEVO: notificar a la UI *en el hilo principal*
        mainThread.Enqueue(() => OnMessageReceived?.Invoke("Servidor: " + receivedMessage));

        udpClient.BeginReceive(ReceiveData, null);
    }

    public void SendData(string message)
    {
        byte[] sendBytes = Encoding.UTF8.GetBytes(message);
        udpClient.Send(sendBytes, sendBytes.Length, remoteEndPoint);
        Debug.Log("Sent to server: " + message);

        // >>> Mostrar inmediatamente mi propio mensaje en la UI
        mainThread.Enqueue(() => OnMessageReceived?.Invoke("Yo: " + message));
    }
}
