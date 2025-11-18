using Fusion;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField roomNameInput;
    public Button hostButton;
    public Button joinButton;
    public Button startButton;

    [Header("Fusion")]
    public NetworkRunner networkRunner;
    public string gameSceneName = "Mobilga Placeholder"; // Your game scene name

    private void Awake()
    {
        // Auto-assign if not set
        if (networkRunner == null) networkRunner = FindObjectOfType<NetworkRunner>();
        if (roomNameInput == null) roomNameInput = FindObjectOfType<TMP_InputField>();
        if (hostButton == null) hostButton = GameObject.Find("HostButton")?.GetComponent<Button>();
        if (joinButton == null) joinButton = GameObject.Find("JoinButton")?.GetComponent<Button>();
        if (startButton == null) startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
    }

    private async void Start()
    {
        hostButton.onClick.AddListener(OnHostButton);
        joinButton.onClick.AddListener(OnJoinButton);
        startButton.onClick.AddListener(OnStartButton);

        startButton.gameObject.SetActive(false);
    }

    private async void OnHostButton()
    {
        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName)) roomName = "TestRoom";

        var args = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = roomName,
            // Poprawka: usuniêto przypisanie Scene, bo NetworkSceneInfo nie przyjmuje int
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        ChangeButtonState(false);
        await networkRunner.StartGame(args);
        Debug.Log($"Hosting room: {roomName}");

        // Show Start button for host
        startButton.gameObject.SetActive(true);
    }

    private async void OnJoinButton()
    {
        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName)) { Debug.LogError("Enter room name!"); return; }

        var args = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = roomName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        ChangeButtonState(false);
        await networkRunner.StartGame(args);
        Debug.Log($"Joining room: {roomName}");
    }

    private async void OnStartButton()
    {
        if (networkRunner.IsServer)
        {
            // Poprawka: u¿yj NetworkRunner.LoadScene zamiast nieistniej¹cej SetActiveScene
            await networkRunner.LoadScene(gameSceneName);
        }
    }

    private void ChangeButtonState(bool enabled)
    {
        hostButton.interactable = enabled;
        joinButton.interactable = enabled;
    }
}