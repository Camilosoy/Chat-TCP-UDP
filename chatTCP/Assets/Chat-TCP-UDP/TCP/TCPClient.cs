using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Text;

public class TCPClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;

    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    public bool isConnected;

    public event Action<string> OnTextReceived;
    public event Action<Texture2D> OnImageReceived;

    private enum MsgType : byte { Text = 0, Image = 1 }

    void Update()
    {
        while (mainThreadActions.TryDequeue(out var action))
            action?.Invoke();
    }

    public void ConnectToServer(string ip, int port)
    {
        client = new TcpClient();
        client.Connect(ip, port);
        stream = client.GetStream();
        isConnected = true;

        receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        receiveThread.Start();

        Debug.Log("[CLIENT] Connected to " + ip + ":" + port);
    }

    private void ReceiveLoop()
    {
        try
        {
            while (client != null && client.Connected)
            {
                int type = stream.ReadByte();
                if (type < 0) break;

                byte[] lenBytes = new byte[4];
                stream.Read(lenBytes, 0, 4);
                int length = BitConverter.ToInt32(lenBytes, 0);

                byte[] payload = new byte[length];
                int read = 0;
                while (read < length)
                    read += stream.Read(payload, read, length - read);

                if ((MsgType)type == MsgType.Text)
                {
                    string msg = Encoding.UTF8.GetString(payload);
                    mainThreadActions.Enqueue(() => OnTextReceived?.Invoke(msg));
                }
                else if ((MsgType)type == MsgType.Image)
                {
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(payload);
                    mainThreadActions.Enqueue(() => OnImageReceived?.Invoke(tex));
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[CLIENT] Receive error: " + e.Message);
        }
    }

    // --- ENVIAR ---
    public void SendText(string msg)
    {
        if (stream == null) return;
        byte[] data = Encoding.UTF8.GetBytes(msg);
        SendFrame(MsgType.Text, data);
    }

    public void SendImage(Texture2D tex)
    {
        if (stream == null) return;
        byte[] data = tex.EncodeToPNG();
        SendFrame(MsgType.Image, data);
    }

    private void SendFrame(MsgType type, byte[] data)
    {
        byte[] lenBytes = BitConverter.GetBytes(data.Length);
        byte[] header = new byte[1 + 4];
        header[0] = (byte)type;
        Array.Copy(lenBytes, 0, header, 1, 4);

        stream.Write(header, 0, header.Length);
        stream.Write(data, 0, data.Length);
        stream.Flush();
    }
}
