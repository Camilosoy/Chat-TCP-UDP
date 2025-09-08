using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UdpClientUI : MonoBehaviour
{
    public int serverPort = 5555;
    public string serverAddress = "127.0.0.1";

    [SerializeField] private UDPClient _client;
    [SerializeField] private TMP_InputField messageInput;

    // >>> NUEVO: a dónde dibujar el chat (asigna en el Inspector)
    [SerializeField] private TMP_Text chatOutput;
    [SerializeField] private ScrollRect scroll;

    void OnEnable()
    {
        if (_client != null) _client.OnMessageReceived += Append;
    }
    void OnDisable()
    {
        if (_client != null) _client.OnMessageReceived -= Append;
    }

    private void Append(string line)
    {
        if (chatOutput == null) return;
        chatOutput.text += (chatOutput.text.Length > 0 ? "\n" : "") + line;
        if (scroll != null)
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0; // auto-scroll al final
        }
    }

    public void SendClientMessage()
    {
        if (!_client.isServerConnected) { Debug.Log("The client is not connected"); return; }
        if (string.IsNullOrEmpty(messageInput.text)) { Debug.Log("The chat entry is empty"); return; }

        _client.SendData(messageInput.text);
        messageInput.text = "";
    }

    public void ConnectClient()
    {
        _client.StartUDPClient(serverAddress, serverPort);
        Append($"Conectando a {serverAddress}:{serverPort} …");
    }
}
