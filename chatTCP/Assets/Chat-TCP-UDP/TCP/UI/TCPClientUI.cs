using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TCPClientUI : MonoBehaviour
{
    public int serverPort = 5555;
    public string serverAddress = "127.0.0.1";
    [SerializeField] private TCPClient _client;
    [SerializeField] private TMP_InputField messageInput;

    [Header("Chat UI")]
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject textBubblePrefab;
    [SerializeField] private GameObject imageBubblePrefab;

    [Header("Imagen a enviar")]
    public Texture2D imageToSend;

    void Start()
    {
        _client.OnTextReceived += (msg) => AddMessage(msg, false);
        _client.OnImageReceived += (tex) => AddImage(tex, false);
    }

    public void ConnectClient()
    {
        _client.ConnectToServer(serverAddress, serverPort);
    }

    public void SendClientMessage()
    {
        if (!_client.isConnected) return;
        if (string.IsNullOrEmpty(messageInput.text)) return;

        string message = messageInput.text;
        _client.SendText(message);
        AddMessage(message, true);
        messageInput.text = "";
    }

    public void SendClientImage()
    {
        if (!_client.isConnected) return;
        if (imageToSend == null) return;

        _client.SendImage(imageToSend);
        AddImage(imageToSend, true);
    }

    private void AddMessage(string text, bool isLocal)
    {
        var bubble = Instantiate(textBubblePrefab, messageContainer);
        bubble.GetComponentInChildren<TMP_Text>().text = text;
        bubble.GetComponent<Image>().color = isLocal ? Color.green : Color.gray;
    }

    private void AddImage(Texture2D tex, bool isLocal)
    {
        var bubble = Instantiate(imageBubblePrefab, messageContainer);
        var raw = bubble.GetComponentInChildren<RawImage>();
        raw.texture = tex;
        bubble.GetComponent<Image>().color = isLocal ? Color.green : Color.gray;
    }
}
