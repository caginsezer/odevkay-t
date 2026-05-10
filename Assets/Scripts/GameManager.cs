using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Oyun Ayarları")]
    public int magnetsPerPlayer = 18;
    public float attractionCheckDelay = 0.5f;
    public float clusterDistance = 1.1f;
    public float maxTurnTime = 10f;

    public enum GameState { MainMenu, WaitingForPlacement, CheckingAttraction, GameOver }
    
    public NetworkVariable<GameState> currentState = new NetworkVariable<GameState>(GameState.MainMenu);
    public NetworkVariable<int> currentPlayer = new NetworkVariable<int>(1);
    public NetworkVariable<int> totalPlayers = new NetworkVariable<int>(2);
    public NetworkVariable<float> currentTurnTime = new NetworkVariable<float>(10f);

    public NetworkList<int> playerRemainingMagnets;
    public NetworkList<int> playerScores;

    public List<MagnetPiece> placedMagnets = new List<MagnetPiece>();
    
    public System.Action<int> OnTurnChanged;
    public System.Action<int, int, int, int> OnMagnetsUpdated; 
    public System.Action<int, int, int, int> OnScoresUpdated;  
    public System.Action<string> OnStatusMessage;
    public System.Action<int> OnGameOver; 
    public System.Action<float> OnTimerUpdated; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            magnetsPerPlayer = 18; 
            maxTurnTime = 10f;
            playerRemainingMagnets = new NetworkList<int>();
            playerScores = new NetworkList<int>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            playerRemainingMagnets.Clear();
            playerScores.Clear();
            for (int i = 0; i < 4; i++) {
                playerRemainingMagnets.Add(0);
                playerScores.Add(0);
            }
            
            // Host: Client bağlandığında ona oyun durumunu gönder
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        
        // Client: Eğer oyun zaten başlamışsa (geç katılım), taşları ve board'u kur
        if (!IsServer && currentState.Value != GameState.MainMenu)
        {
            Debug.Log($"[GameManager] Geç katılım algılandı! Oyun durumu: {currentState.Value}, Oyuncu sayısı: {totalPlayers.Value}");
            SetupLocalGame(totalPlayers.Value);
        }
        
        playerRemainingMagnets.OnListChanged += (e) => TriggerMagnetsUpdated();
        playerScores.OnListChanged += (e) => TriggerScoresUpdated();
        currentPlayer.OnValueChanged += (prev, current) => {
            OnTurnChanged?.Invoke(current);
            OnStatusMessage?.Invoke($"Oyuncu {current}'in sirasi.");
        };
    }
    
    private void OnClientConnected(ulong clientId)
    {
        // Sunucu kendisi olduğunda atlat (host)
        if (clientId == NetworkManager.Singleton.LocalClientId) return;
        
        // Oyun zaten başlamışsa, yeni client'a başlatma komutu gönder
        if (currentState.Value != GameState.MainMenu)
        {
            Debug.Log($"[GameManager] Yeni client bağlandı (ID: {clientId}), ona oyun durumunu gönderiyorum.");
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
            LateJoinSetupClientRpc(totalPlayers.Value, clientRpcParams);
        }
    }
    
    [ClientRpc]
    private void LateJoinSetupClientRpc(int playerCount, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[GameManager] LateJoinSetup: Client tarafında {playerCount} oyunculu oyun kuruluyor!");
        SetupLocalGame(playerCount);
    }
    
    private void SetupLocalGame(int playerCount)
    {
        var reserveMgr = FindAnyObjectByType<StoneReserveManager>();
        if (reserveMgr != null)
        {
            reserveMgr.ClearAll();
            reserveMgr.Initialize(magnetsPerPlayer, playerCount);
        }

        if (BoardSetup.Instance != null)
        {
            BoardSetup.Instance.CreateBoard(playerCount);
        }

        FindAnyObjectByType<UIManager>()?.ShowGameUI();
        AudioManager.Instance?.Play(AudioManager.SoundType.GameAmbiance);
        
        TriggerMagnetsUpdated();
        TriggerScoresUpdated();
        OnStatusMessage?.Invoke($"Oyuncu {currentPlayer.Value}'in sirasi.");
    }

    private void TriggerMagnetsUpdated()
    {
        int p1 = playerRemainingMagnets.Count > 0 ? playerRemainingMagnets[0] : 0;
        int p2 = playerRemainingMagnets.Count > 1 ? playerRemainingMagnets[1] : 0;
        int p3 = playerRemainingMagnets.Count > 2 ? playerRemainingMagnets[2] : 0;
        int p4 = playerRemainingMagnets.Count > 3 ? playerRemainingMagnets[3] : 0;
        OnMagnetsUpdated?.Invoke(p1, p2, p3, p4);
    }

    private void TriggerScoresUpdated()
    {
        int p1 = playerScores.Count > 0 ? playerScores[0] : 0;
        int p2 = playerScores.Count > 1 ? playerScores[1] : 0;
        int p3 = playerScores.Count > 2 ? playerScores[2] : 0;
        int p4 = playerScores.Count > 3 ? playerScores[3] : 0;
        OnScoresUpdated?.Invoke(p1, p2, p3, p4);
    }

    public void StartGameFromMenu(int playerCount)
    {
        if (!IsServer) return;
        
        totalPlayers.Value = playerCount;
        currentState.Value = GameState.WaitingForPlacement;

        StartNewGameClientRpc(playerCount);
    }

    [ClientRpc]
    private void StartNewGameClientRpc(int playerCount)
    {
        var reserveMgr = FindAnyObjectByType<StoneReserveManager>();
        if (reserveMgr != null)
        {
            reserveMgr.ClearAll();
            reserveMgr.Initialize(magnetsPerPlayer, playerCount);
        }

        if (BoardSetup.Instance != null)
        {
            BoardSetup.Instance.CreateBoard(playerCount);
        }

        FindAnyObjectByType<UIManager>()?.ShowGameUI();
        AudioManager.Instance?.Play(AudioManager.SoundType.GameAmbiance);
        
        // Sadece server silsin
        if (IsServer)
        {
            foreach (var magnet in placedMagnets)
            {
                if (magnet != null && magnet.gameObject != null)
                    magnet.GetComponent<NetworkObject>()?.Despawn();
            }
            placedMagnets.Clear();

            for (int i = 0; i < 4; i++)
            {
                playerRemainingMagnets[i] = i < playerCount ? magnetsPerPlayer : 0;
                playerScores[i] = 0;
            }

            currentPlayer.Value = 1;
            currentTurnTime.Value = maxTurnTime;
            currentState.Value = GameState.WaitingForPlacement;
        }

        TriggerMagnetsUpdated();
        TriggerScoresUpdated();
        OnStatusMessage?.Invoke("Oyun Başladı! Oyuncu 1'in sırası.");
    }

    private void Update()
    {
        if (!IsServer) return;

        if (currentState.Value == GameState.WaitingForPlacement)
        {
            currentTurnTime.Value -= Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(currentTurnTime.Value / maxTurnTime);
            UpdateTimerClientRpc(normalizedTime);

            if (currentTurnTime.Value <= 0)
            {
                currentTurnTime.Value = 0;
                UpdateTimerClientRpc(0);
                SwitchTurn();
            }
        }
    }

    [ClientRpc]
    private void UpdateTimerClientRpc(float normalizedTime)
    {
        OnTimerUpdated?.Invoke(normalizedTime);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryPlaceStoneServerRpc(Vector3 dropPos, int playerID)
    {
        if (currentState.Value != GameState.WaitingForPlacement) return;
        if (currentPlayer.Value != playerID) return;

        GameObject prefab = Resources.Load<GameObject>("MagnetPiecePrefab");
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, dropPos, Quaternion.identity);
            MagnetPiece magnet = obj.GetComponent<MagnetPiece>();
            
            // SPAWN FIRST!
            obj.GetComponent<NetworkObject>().Spawn();
            
            // THEN ASSIGN NETWORK VARIABLES!
            magnet.ownerPlayer.Value = playerID;

            OnMagnetPlaced(magnet);
        }
    }

    public void OnMagnetPlaced(MagnetPiece magnet)
    {
        if (!IsServer) return;
        if (currentState.Value != GameState.WaitingForPlacement) return;

        magnet.ownerPlayer.Value = currentPlayer.Value;
        magnet.isPlaced.Value = true;
        placedMagnets.Add(magnet);

        int pIndex = currentPlayer.Value - 1;
        if (pIndex >= 0 && pIndex < playerRemainingMagnets.Count)
        {
            playerRemainingMagnets[pIndex]--;
        }

        currentState.Value = GameState.CheckingAttraction;
        StartCoroutine(CheckAttractionsAfterDelay());
    }

    private IEnumerator CheckAttractionsAfterDelay()
    {
        yield return new WaitForSeconds(attractionCheckDelay);

        List<MagnetPiece> clusteredMagnets = FindClusteredMagnets();
        int pIndex = currentPlayer.Value - 1;

        if (clusteredMagnets.Count > 0)
        {
            int penaltyCount = clusteredMagnets.Count;
            int penaltyScore = penaltyCount * 50;
            
            playerScores[pIndex] = Mathf.Max(0, playerScores[pIndex] - penaltyScore);

            PlaySoundClientRpc(AudioManager.SoundType.Penalty);

            foreach (var magnet in clusteredMagnets)
            {
                placedMagnets.Remove(magnet);
                magnet.GetComponent<NetworkObject>()?.Despawn();
                ReturnStoneClientRpc(currentPlayer.Value);
            }

            playerRemainingMagnets[pIndex] += penaltyCount;
        }
        else
        {
            int baseScore = 100;
            int timeBonus = Mathf.FloorToInt((currentTurnTime.Value / maxTurnTime) * 50);
            int earnedScore = baseScore + timeBonus;

            playerScores[pIndex] += earnedScore;
        }

        if (CheckWinCondition())
        {
            yield break;
        }

        SwitchTurn();
    }

    [ClientRpc]
    private void ReturnStoneClientRpc(int player)
    {
        StoneReserveManager.Instance?.ReturnStone(player);
    }

    [ClientRpc]
    private void PlaySoundClientRpc(AudioManager.SoundType sound)
    {
        AudioManager.Instance?.Play(sound);
    }

    private List<MagnetPiece> FindClusteredMagnets()
    {
        HashSet<MagnetPiece> clustered = new HashSet<MagnetPiece>();
        for (int i = 0; i < placedMagnets.Count; i++)
        {
            for (int j = i + 1; j < placedMagnets.Count; j++)
            {
                if (placedMagnets[i] == null || placedMagnets[j] == null) continue;

                float distance = Vector3.Distance(
                    placedMagnets[i].transform.position,
                    placedMagnets[j].transform.position
                );

                if (distance < clusterDistance)
                {
                    clustered.Add(placedMagnets[i]);
                    clustered.Add(placedMagnets[j]);
                }
            }
        }
        return new List<MagnetPiece>(clustered);
    }

    private bool CheckWinCondition()
    {
        for (int i = 0; i < totalPlayers.Value; i++)
        {
            if (playerRemainingMagnets[i] <= 0)
            {
                currentState.Value = GameState.GameOver;
                playerScores[i] += 1000;
                TriggerScoresUpdated();
                GameOverClientRpc(i + 1);
                return true;
            }
        }
        return false;
    }

    [ClientRpc]
    private void GameOverClientRpc(int winnerPlayer)
    {
        OnStatusMessage?.Invoke($"Oyuncu {winnerPlayer} Kazandi!");
        OnGameOver?.Invoke(winnerPlayer);
        AudioManager.Instance?.Play(AudioManager.SoundType.Win);
    }

    private void SwitchTurn()
    {
        if (!IsServer) return;
        
        int nextPlayer = currentPlayer.Value + 1;
        if (nextPlayer > totalPlayers.Value) nextPlayer = 1;
        
        currentPlayer.Value = nextPlayer;
        currentState.Value = GameState.WaitingForPlacement;
        currentTurnTime.Value = maxTurnTime;

        PlaySoundClientRpc(AudioManager.SoundType.TurnChange);
    }

    public void UndoLastMove() { } // Devre Dışı

    public Color GetPlayerColor(int player)
    {
        switch(player)
        {
            case 1: return new Color(0.1f, 0.85f, 1f, 1f); // Mavi (Alt)
            case 2: return new Color(1f, 0.1f, 0.1f, 1f); // Kırmızı (Üst)
            case 3: return new Color(0.2f, 1f, 0.2f, 1f); // Yeşil (Sol)
            case 4: return new Color(1f, 0.8f, 0.1f, 1f); // Sarı (Sağ)
            default: return Color.white;
        }
    }
}
