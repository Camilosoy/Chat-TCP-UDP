using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TCPServerUI : MonoBehaviour
{
    public int serverPort = 5555;
    [SerializeField] private TCPServer _server;
    [SerializeField] private TMP_InputField messageInput;

    [Header("Chat UI")]
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject textBubblePrefab;
    [SerializeField] private GameObject imageBubblePrefab;

    [Header("Imagen a enviar")]
    public Texture2D imageToSend;

    void Start()
    {
        _server.OnTextReceived += (msg) => AddMessage(msg, false);
        _server.OnImageReceived += (tex) => AddImage(tex, false);
    }

    public void StartServer()
    {
        _server.StartServer(serverPort);
    }

    public void SendServerMessage()
    {
        if (!_server.isRunning) return;
        if (string.IsNullOrEmpty(messageInput.text)) return;

        string message = messageInput.text;
        _server.SendText(message);
        AddMessage(message, true);
        messageInput.text = "";
    }

    public void SendServerImage()
    {
        if (!_server.isRunning) return;
        if (imageToSend == null) return;

        _server.SendImage(imageToSend);
        AddImage(imageToSend, true);
    }

    private void AddMessage(string text, bool isLocal)
    {
        var bubble = Instantiate(textBubblePrefab, messageContainer);
        bubble.GetComponentInChildren<TMP_Text>().text = text;
        bubble.GetComponent<Image>().color = isLocal ? Color.cyan : Color.gray;
    }

    private void AddImage(Texture2D tex, bool isLocal)
    {
        var bubble = Instantiate(imageBubblePrefab, messageContainer);
        var raw = bubble.GetComponentInChildren<RawImage>();
        raw.texture = tex;
        bubble.GetComponent<Image>().color = isLocal ? Color.cyan : Color.gray;
    }
}
