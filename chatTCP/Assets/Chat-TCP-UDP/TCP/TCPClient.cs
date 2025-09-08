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

    private const int MaxFrameSize = 32 * 1024 * 1024; // 32MB por seguridad

    void Update()
    {
        while (mainThreadActions.TryDequeue(out var a))
        {
            try { a?.Invoke(); } catch (Exception e) { Debug.LogWarning("[CLIENT] MainThread action err: " + e.Message); }
        }
    }

    public void ConnectToServer(string ip, int port)
    {
        client = new TcpClient();
        client.NoDelay = true;
        client.Connect(ip, port);

        stream = client.GetStream();
        stream.ReadTimeout = 30000;
        stream.WriteTimeout = 30000;

        isConnected = true;

        receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        receiveThread.Start();

        Debug.Log("[CLIENT] Connected to " + ip + ":" + port);
    }

    private static bool ReadExact(NetworkStream s, byte[] buffer, int offset, int count)
    {
        int readTotal = 0;
        while (readTotal < count)
        {
            int r = s.Read(buffer, offset + readTotal, count - readTotal);
            if (r <= 0) return false; // desconectado
            readTotal += r;
        }
        return true;
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
                if (!ReadExact(stream, lenBytes, 0, 4)) break;
                int length = BitConverter.ToInt32(lenBytes, 0);

                if (length < 0 || length > MaxFrameSize)
                {
                    Debug.LogWarning("[CLIENT] Invalid frame length: " + length);
                    break;
                }

                byte[] payload = new byte[length];
                if (!ReadExact(stream, payload, 0, length)) break;

                if ((MsgType)type == MsgType.Text)
                {
                    string msg = Encoding.UTF8.GetString(payload);
                    mainThreadActions.Enqueue(() => OnTextReceived?.Invoke(msg));
                }
                else // Image
                {
                    // ¡Nada de Unity en este hilo! Creamos la textura en el hilo principal.
                    byte[] img = payload;
                    mainThreadActions.Enqueue(() =>
                    {
                        try
                        {
                            var tex = new Texture2D(2, 2);
                            tex.LoadImage(img);  // seguro en hilo principal
                            OnImageReceived?.Invoke(tex);
                        }
                        catch (Exception e) { Debug.LogWarning("[CLIENT] LoadImage error: " + e.Message); }
                    });
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[CLIENT] Receive error: " + e.Message);
        }
        finally
        {
            isConnected = false;
            try { stream?.Close(); } catch { }
            try { client?.Close(); } catch { }
            Debug.Log("[CLIENT] Disconnected.");
        }
    }

    // --- ENVIAR ---
    public void SendText(string msg)
    {
        if (stream == null) return;
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            SendFrame(MsgType.Text, data);
        }
        catch (Exception e) { Debug.LogWarning("[CLIENT] SendText error: " + e.Message); }
    }

    public void SendImage(Texture2D tex)
    {
        if (stream == null) return;
        try
        {
            byte[] data = tex.EncodeToPNG(); // JPG si quieres menos tamaño
            if (data.Length > MaxFrameSize)
            {
                Debug.LogWarning($"[CLIENT] Image too large ({data.Length} bytes).");
                return;
            }
            SendFrame(MsgType.Image, data);
        }
        catch (Exception e) { Debug.LogWarning("[CLIENT] SendImage error: " + e.Message); }
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

    private void OnDestroy()
    {
        try { stream?.Close(); } catch { }
        try { client?.Close(); } catch { }
        isConnected = false;
    }
}
