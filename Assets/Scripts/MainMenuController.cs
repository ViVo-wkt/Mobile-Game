using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic; // Required for List<> and Dictionary<>
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Panel References")]
    public GameObject mainPanel;
    public GameObject coopPanel;
    public GameObject settingsPanel;
    public GameObject loadingPanel;

    [Header("Co-op UI")]
    public TMP_InputField roomNameInput;

    [Header("Game Settings")]
    // The scene to load when the game starts. 
    // IMPORTANT: "Main Level" must be in Build Settings!
    public string gameSceneName = "Main Level"; 

    private NetworkRunner _runner;

    private void Start()
    {
        // 1. Ensure clean state on startup
        ShowPanel(mainPanel);
        if (loadingPanel) loadingPanel.SetActive(false);

        // 2. Setup Input Field validation
        if (roomNameInput)
        {
            roomNameInput.characterLimit = 15;
            roomNameInput.onValidateInput += (string text, int charIndex, char addedChar) => 
            {
                return char.IsLetterOrDigit(addedChar) ? addedChar : '\0'; 
            };
        }
    }

    // ================== BUTTON CLICK EVENTS ==================

    public void OnSoloClicked()
    {
        StartGame(GameMode.Single, "SoloSession");
    }

    public void OnCoopMenuClicked()
    {
        ShowPanel(coopPanel);
    }

    public void OnConnectClicked()
    {
        string roomName = roomNameInput.text;
        
        if (string.IsNullOrEmpty(roomName)) 
            roomName = "Room_" + UnityEngine.Random.Range(1000, 9999);

        StartGame(GameMode.Shared, roomName);
    }

    public void OnSettingsClicked()
    {
        ShowPanel(settingsPanel);
    }

    public void OnBackClicked()
    {
        ShowPanel(mainPanel);
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ================== FUSION STARTUP LOGIC ==================

    private async void StartGame(GameMode mode, string sessionName)
    {
        ShowPanel(null); 
        if (loadingPanel) loadingPanel.SetActive(true);

        if (_runner == null) 
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
        }
        
        _runner.ProvideInput = true;

        if (!_runner.TryGetComponent(out NetworkInputManager inputMgr))
        {
            _runner.gameObject.AddComponent<NetworkInputManager>();
        }
        
        if (!_runner.TryGetComponent(out NetworkSceneManagerDefault sceneMgr))
        {
            sceneMgr = _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        // Get the build index from the name string
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{gameSceneName}.unity");
        
        // Safety check: if scene isn't found, default to index 1 (usually the first game scene)
        if (sceneIndex < 0) 
        {
            Debug.LogWarning($"Scene '{gameSceneName}' not found in Build Settings. Defaulting to index 1.");
            sceneIndex = 1; 
        }

        try 
        {
            // FIX: Manually create NetworkSceneInfo struct.
            // This approach avoids 'FromBuildIndex' if it's missing in your specific Fusion version.
            var sceneInfo = new NetworkSceneInfo();
            // We cast sceneIndex to prevent ambiguous calls if any exist, though mostly standard int works.
            sceneInfo.AddSceneRef(SceneRef.FromIndex(sceneIndex), LoadSceneMode.Single);

            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = sessionName,
                SceneManager = sceneMgr,
                Scene = sceneInfo
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start game: {e.Message}");
            if (loadingPanel) loadingPanel.SetActive(false);
            ShowPanel(mainPanel);
        }
    }

    private void ShowPanel(GameObject panelToShow)
    {
        if(mainPanel) mainPanel.SetActive(false);
        if(coopPanel) coopPanel.SetActive(false);
        if(settingsPanel) settingsPanel.SetActive(false);
        
        if (panelToShow != null) panelToShow.SetActive(true);
    }

    // ================== REQUIRED FUSION CALLBACKS ==================
    
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}