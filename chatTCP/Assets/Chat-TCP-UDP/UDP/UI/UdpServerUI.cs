using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UdpServerUI : MonoBehaviour
{
    public int serverPort = 5555;

    [SerializeField] private UDPServer _server;
    [SerializeField] private TMP_InputField messageInput;


    [SerializeField] private TMP_Text chatOutput;
    [SerializeField] private ScrollRect scroll;

    void OnEnable()
    {
        if (_server != null) _server.OnMessageReceived += Append;
    }
    void OnDisable()
    {
        if (_server != null) _server.OnMessageReceived -= Append;
    }

    private void Append(string line)
    {
        if (chatOutput == null) return;
        chatOutput.text += (chatOutput.text.Length > 0 ? "\n" : "") + line;
        if (scroll != null)
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0;
        }
    }

    public void SendServerMessage()
    {
        if (!_server.isServerRunning) { Debug.Log("The server is not running"); return; }
        if (string.IsNullOrEmpty(messageInput.text)) { Debug.Log("The chat entry is empty"); return; }

        _server.SendData(messageInput.text);
        messageInput.text = "";
    }

    public void StartServer()
    {
        _server.StartUDPServer(serverPort);
        Append($"Servidor escuchando en 0.0.0.0:{serverPort}");
    }
}
