using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using RelayServerData = Unity.Networking.Transport.Relay.RelayServerData;

public class MainMenuManager : MonoBehaviour
{
    private GameObject menuCanvasObj;
    private Canvas menuCanvas;
    
    private List<RectTransform> animLetters = new List<RectTransform>();
    private List<Vector2> originalPositions = new List<Vector2>();
    private float timeElapsed = 0f;

    private GameObject rulesPopup;
    private GameObject lobbyPopup;
    private InputField ipInput;

    public void Initialize()
    {
        BuildMenuCanvas();
        ShowMenu(); 
    }

    public void ShowMenu()
    {
        if (menuCanvasObj != null)
            menuCanvasObj.SetActive(true);
        else
            BuildMenuCanvas();

        AudioManager.Instance?.Play(AudioManager.SoundType.MenuAmbiance);
    }

    private void BuildMenuCanvas()
    {
        menuCanvasObj = new GameObject("MainMenuCanvas");
        menuCanvas = menuCanvasObj.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 50;

        CanvasScaler scaler = menuCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); 
        scaler.matchWidthOrHeight = 0.5f;
        menuCanvasObj.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Modern Arka Plan
        GameObject bgObj = new GameObject("MenuOverlay");
        bgObj.transform.SetParent(menuCanvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.02f, 0.05f, 0.95f); // Koyu Lacivert/Siyah
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

        CreateAnimatedTitle("MAGNETIC", new Vector2(0, 450));
        
        GameObject subTitleObj = CreateTextObj("MAYHEM", 55, FontStyle.Bold, new Color(1f, 0.8f, 0.1f));
        SetAnchorsAndOffset(subTitleObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 320));
        
        GameObject lineInfo = new GameObject("SubLine");
        lineInfo.transform.SetParent(menuCanvasObj.transform, false);
        Image lineImg = lineInfo.AddComponent<Image>();
        lineImg.color = new Color(0.2f, 0.8f, 1f, 0.8f); // Neon Mavi Çizgi
        SetAnchorsAndOffset(lineInfo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 280), new Vector2(400, 4));

        GameObject playBtnObj = CreateButton("PlayButton", "OYUNA BAŞLA", 45, new Color(0.1f, 0.6f, 1f, 0.8f), Color.white);
        SetAnchorsAndOffset(playBtnObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -100), new Vector2(600, 120));
        AddButtonHoverEffect(playBtnObj);
        playBtnObj.GetComponent<Button>().onClick.AddListener(OnPlayClicked);

        GameObject rulesBtnObj = CreateButton("RulesButton", "KURALLAR", 35, new Color(0.1f, 0.1f, 0.2f, 0.8f), new Color(0.8f, 0.8f, 0.8f));
        SetAnchorsAndOffset(rulesBtnObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -280), new Vector2(400, 90));
        AddButtonHoverEffect(rulesBtnObj);
        rulesBtnObj.GetComponent<Button>().onClick.AddListener(OnRulesClicked);

        CreateRulesPopup();
        CreateLobbyPopup();
    }

    private void AddButtonHoverEffect(GameObject btnObj)
    {
        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.8f, 1f, 0.5f);
        outline.effectDistance = new Vector2(4, -4);
        
        // Basit bir scale animasyonu için EventTrigger eklenebilir, şimdilik statik premium glow ekledik
    }

    private void CreateAnimatedTitle(string word, Vector2 centerOffset)
    {
        float letterSpacing = 95f;
        float totalWidth = (word.Length - 1) * letterSpacing;
        float startX = -totalWidth / 2f;

        animLetters.Clear();
        originalPositions.Clear();

        for (int i = 0; i < word.Length; i++)
        {
            float posX = startX + i * letterSpacing;
            string letter = word[i].ToString();

            GameObject letterObj = CreateTextObj(letter, 120, FontStyle.Bold, Color.white);
            RectTransform rect = letterObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            
            Vector2 finalPos = new Vector2(posX, centerOffset.y);
            rect.anchoredPosition = finalPos;
            rect.sizeDelta = new Vector2(100, 150);

            float t = (float)i / (word.Length - 1);
            Color col = Color.Lerp(new Color(0.2f, 0.8f, 1f), new Color(0.8f, 0.2f, 1f), t); // Mavi-Mor Gradient
            
            letterObj.GetComponent<Text>().color = col;
            Shadow shadow = letterObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(4, -4);

            Outline ol = letterObj.AddComponent<Outline>();
            ol.effectColor = col * 0.5f;
            ol.effectDistance = new Vector2(2, -2);

            animLetters.Add(rect);
            originalPositions.Add(finalPos);
        }
    }

    private void Update()
    {
        if (animLetters.Count == 0 || menuCanvasObj == null || !menuCanvasObj.activeSelf) return;

        timeElapsed += Time.deltaTime * 2.5f;

        for (int i = 0; i < animLetters.Count; i++)
        {
            float wave = Mathf.Sin(timeElapsed + (i * 0.5f)) * 20f;
            animLetters[i].anchoredPosition = originalPositions[i] + new Vector2(0, wave);
        }
    }

    private Font GetMenuFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    private GameObject CreateTextObj(string content, int fontSize, FontStyle style, Color color)
    {
        GameObject txtObj = new GameObject("TextObj");
        txtObj.transform.SetParent(menuCanvasObj.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.font = GetMenuFont();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.raycastTarget = false; // Metinler tıklamayı engellemesin
        return txtObj;
    }

    private void SetAnchorsAndOffset(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size = default)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        if (size != default)
        {
            rect.sizeDelta = size;
        }
        else
        {
            // Eğer stretch (0 to 1) ise sizeDelta (0,0) olmalı, aksi takdirde taşma yapar
            if (anchorMin == Vector2.zero && anchorMax == Vector2.one)
                rect.sizeDelta = Vector2.zero;
            else
                rect.sizeDelta = new Vector2(800, 200);
        }
    }

    private GameObject CreateButton(string objName, string textContent, int fontSize, Color bgColor, Color textColor)
    {
        GameObject btnObj = new GameObject(objName);
        btnObj.transform.SetParent(menuCanvasObj.transform, false);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        Button btn = btnObj.AddComponent<Button>();
        
        GameObject txtObj = CreateTextObj(textContent, fontSize, FontStyle.Bold, textColor);
        txtObj.transform.SetParent(btnObj.transform, false);
        SetAnchorsAndOffset(txtObj, Vector2.zero, Vector2.one, Vector2.zero); 

        return btnObj;
    }

    private void ApplyGlassmorphism(GameObject panel)
    {
        Image img = panel.GetComponent<Image>();
        if (img == null) img = panel.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.08f, 0.85f); // Koyu saydam

        Outline ol = panel.AddComponent<Outline>();
        ol.effectColor = new Color(0.3f, 0.5f, 1f, 0.4f);
        ol.effectDistance = new Vector2(3, -3);
    }

    private void CreateRulesPopup()
    {
        rulesPopup = new GameObject("RulesPopup");
        rulesPopup.transform.SetParent(menuCanvasObj.transform, false);
        ApplyGlassmorphism(rulesPopup);
        SetAnchorsAndOffset(rulesPopup, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(850, 1100));

        GameObject title = CreateTextObj("KURALLAR", 55, FontStyle.Bold, new Color(0.2f, 0.8f, 1f));
        title.transform.SetParent(rulesPopup.transform, false);
        SetAnchorsAndOffset(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(600, 100));

        string rulesBody = 
        "1. Her oyuncunun kendine ait taşları vardır.\n\n" +
        "2. Sırası gelen oyuncu, kendi rezervinden bir taşı ortadaki masaya bırakır.\n\n" +
        "3. Süre sınırı vardır, süre dolarsa sıranı kaybedersin.\n\n" +
        "4. Mıknatısları birbirine değdirmeden yerleştir! Eğer taşları yapıştırırsan, hepsi geri döner ve eksi puan yersin.\n\n" +
        "5. Elindeki taşları ilk bitiren kazanır!";

        GameObject body = CreateTextObj(rulesBody, 36, FontStyle.Normal, new Color(0.9f, 0.9f, 0.95f));
        body.transform.SetParent(rulesPopup.transform, false);
        Text bodyTxt = body.GetComponent<Text>();
        bodyTxt.alignment = TextAnchor.UpperLeft;
        SetAnchorsAndOffset(body, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0, -60), new Vector2(0, 0)); 
        RectTransform rect = body.GetComponent<RectTransform>();
        rect.offsetMin = new Vector2(60, 150); 
        rect.offsetMax = new Vector2(-60, -200); 

        GameObject closeBtn = CreateButton("CloseRules", "KAPAT", 40, new Color(0.8f, 0.2f, 0.3f, 0.9f), Color.white);
        closeBtn.transform.SetParent(rulesPopup.transform, false);
        SetAnchorsAndOffset(closeBtn, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(350, 90));
        AddButtonHoverEffect(closeBtn);
        closeBtn.GetComponent<Button>().onClick.AddListener(() => { rulesPopup.SetActive(false); });

        rulesPopup.SetActive(false); 
    }

    private Text joinCodeDisplay;
    private InputField joinCodeInput;
    private GameObject statusText;

    private void CreateLobbyPopup()
    {
        lobbyPopup = new GameObject("LobbyPopup");
        lobbyPopup.transform.SetParent(menuCanvasObj.transform, false);
        ApplyGlassmorphism(lobbyPopup);
        SetAnchorsAndOffset(lobbyPopup, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(850, 1300));

        GameObject title = CreateTextObj("LOBİ BAĞLANTISI", 55, FontStyle.Bold, new Color(1f, 0.8f, 0.1f));
        title.transform.SetParent(lobbyPopup.transform, false);
        SetAnchorsAndOffset(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(600, 100));

        // --- Host Alanı ---
        GameObject hostTitle = CreateTextObj("OYUN KUR (HOST)", 40, FontStyle.Bold, new Color(0.3f, 0.8f, 1f));
        hostTitle.transform.SetParent(lobbyPopup.transform, false);
        SetAnchorsAndOffset(hostTitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(600, 80));

        GameObject h2 = CreateButton("H2", "2 Oyuncu", 35, new Color(0.2f, 0.6f, 1f, 0.8f), Color.white);
        h2.transform.SetParent(lobbyPopup.transform, false);
        SetAnchorsAndOffset(h2, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -280), new Vector2(400, 80));
        AddButtonHoverEffect(h2);
        h2.GetComponent<Button>().onClick.AddListener(() => StartHostGame(2));

        GameObject h3 = CreateButton("H3", "3 Oyuncu", 35, new Color(0.2f, 0.8f, 0.2f, 0.8f), Color.white);
        h3.transform.SetParent(lobbyPopup.transform, false);
        SetAnchorsAndOffset(h3, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -370), new Vector2(400, 80));
        AddButtonHoverEffect(h3);
        h3.GetComponent<Button>().onClick.AddListener(() => StartHostGame(3));

        GameObject h4 = CreateButton("H4", "4 Oyuncu", 35, new Color(1f, 0.6f, 0.1f, 0.8f), Color.white);
        h4.transform.SetParent(lobbyPopup.transform, false);
        SetAnchorsAndOffset(h4, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -460), new Vector2(400, 80));
        AddButtonHoverEffect(h4);
        h4.GetComponent<Button>().onClick.AddListener(() => StartHostGame(4));

        // Oda Kodu Göstergesi (Host olunca görünür)
        GameObject codeLabel = CreateTextObj("ODA KODU:", 32, FontStyle.Bold, new Color(0.5f, 1f, 0.5f));
        codeLabel.transform.SetParent(lobbyPopup.transform, false);
        SetAnchorsAndOffset(codeLabel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -560), new Vector2(400, 60));
        codeLabel.SetActive(false);

        GameObject codeObj = CreateTextObj("------", 60, FontStyle.Bold, new Color(1f, 1f, 0.3f));
        codeObj.transform.SetParent(lobbyPopup.transform, false);
        SetAnchorsAndOffset(codeObj, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -620), new Vector2(500, 80));
        joinCodeDisplay = codeObj.GetComponent<Text>();
        codeObj.SetActive(false);

        // --- Join Alanı ---
        GameObject joinTitle = CreateTextObj("ODAYA KATIL (CLIENT)", 40, FontStyle.Bold, new Color(1f, 0.6f, 0.8f));
        joinTitle.transform.SetParent(lobbyPopup.transform, false);
        SetAnchorsAndOffset(joinTitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -740), new Vector2(600, 80));

        GameObject inputObj = new GameObject("JoinCodeInput");
        inputObj.transform.SetParent(lobbyPopup.transform, false);
        Image inputBg = inputObj.AddComponent<Image>();
        inputBg.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        SetAnchorsAndOffset(inputObj, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -840), new Vector2(500, 80));
        
        GameObject textObj = CreateTextObj("Oda Kodunu Gir", 35, FontStyle.Normal, Color.black);
        textObj.transform.SetParent(inputObj.transform, false);
        SetAnchorsAndOffset(textObj, Vector2.zero, Vector2.one, Vector2.zero);
        
        joinCodeInput = inputObj.AddComponent<InputField>();
        joinCodeInput.textComponent = textObj.GetComponent<Text>();
        joinCodeInput.text = "";
        joinCodeInput.characterLimit = 6;

        // Eski ipInput referansını da koruyalım (uyumluluk)
        ipInput = joinCodeInput;

        GameObject jBtn = CreateButton("Join", "BAĞLAN", 40, new Color(0.8f, 0.2f, 0.8f, 0.8f), Color.white);
        jBtn.transform.SetParent(lobbyPopup.transform, false);
        SetAnchorsAndOffset(jBtn, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -940), new Vector2(400, 80));
        AddButtonHoverEffect(jBtn);
        jBtn.GetComponent<Button>().onClick.AddListener(JoinGame);

        // Durum Mesajı
        statusText = CreateTextObj("", 28, FontStyle.Italic, new Color(0.8f, 0.8f, 0.8f));
        statusText.transform.SetParent(lobbyPopup.transform, false);
        SetAnchorsAndOffset(statusText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -1040), new Vector2(700, 60));

        GameObject closeBtn = CreateButton("CloseLobby", "İPTAL", 35, new Color(0.8f, 0.2f, 0.3f, 0.8f), Color.white);
        closeBtn.transform.SetParent(lobbyPopup.transform, false);
        SetAnchorsAndOffset(closeBtn, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(350, 80));
        AddButtonHoverEffect(closeBtn);
        closeBtn.GetComponent<Button>().onClick.AddListener(() => { StartCoroutine(SlidePopup(lobbyPopup, false)); });

        lobbyPopup.SetActive(false);
    }

    private void OnPlayClicked()
    {
        AudioManager.Instance?.Play(AudioManager.SoundType.ButtonClick);
        lobbyPopup.SetActive(true);
        StartCoroutine(SlidePopup(lobbyPopup, true));
    }

    private void OnRulesClicked()
    {
        AudioManager.Instance?.Play(AudioManager.SoundType.ButtonClick);
        rulesPopup.SetActive(true);
        StartCoroutine(SlidePopup(rulesPopup, true));
    }

    private IEnumerator SlidePopup(GameObject popup, bool show)
    {
        RectTransform rt = popup.GetComponent<RectTransform>();
        Vector2 hiddenPos = new Vector2(0, -2000);
        Vector2 visiblePos = Vector2.zero;

        if (show)
        {
            rt.anchoredPosition = hiddenPos;
            popup.SetActive(true);
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 3f;
                rt.anchoredPosition = Vector2.Lerp(hiddenPos, visiblePos, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
        }
        else
        {
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 3f;
                rt.anchoredPosition = Vector2.Lerp(visiblePos, hiddenPos, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            popup.SetActive(false);
        }
    }
    private ushort FindAvailablePort(ushort startPort = 7777)
    {
        for (ushort port = startPort; port < startPort + 100; port++)
        {
            try
            {
                using (var socket = new System.Net.Sockets.Socket(
                    System.Net.Sockets.AddressFamily.InterNetwork,
                    System.Net.Sockets.SocketType.Dgram,
                    System.Net.Sockets.ProtocolType.Udp))
                {
                    socket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));
                    return port;
                }
            }
            catch { }
        }
        return startPort;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.GetComponent<Text>().text = msg;
        Debug.Log($"[Relay] {msg}");
    }

    private async Task InitializeUnityServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        Debug.Log("[Relay] Unity Services hazır, anonim giriş yapıldı.");
    }

    private void ConfigureNetworkManager()
    {
        NetworkManager.Singleton.NetworkConfig.ForceSamePrefabs = false;
        NetworkManager.Singleton.NetworkConfig.EnableSceneManagement = false;
        
        GameObject magnetPrefab = Resources.Load<GameObject>("MagnetPiecePrefab");
        GameObject netGmPrefab = Resources.Load<GameObject>("GameManagerPrefab");
        
        if (magnetPrefab != null && !NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(magnetPrefab))
            NetworkManager.Singleton.AddNetworkPrefab(magnetPrefab);
        if (netGmPrefab != null && !NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(netGmPrefab))
            NetworkManager.Singleton.AddNetworkPrefab(netGmPrefab);
    }

    private void StartHostGame(int playerCount)
    {
        StartHostWithRelay(playerCount);
    }

    private async void StartHostWithRelay(int playerCount)
    {
        SetStatus("Sunucu hazırlanıyor...");
        
        try
        {
            await InitializeUnityServices();
            SetStatus("Relay sunucusuna bağlanılıyor...");

            // Relay allocation oluştur (maxConnections = playerCount - 1, host kendisi sayılmıyor)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(playerCount - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            Debug.Log($"[Relay] Oda kodu: {joinCode}");

            // Oda kodunu ekranda göster
            if (joinCodeDisplay != null)
            {
                joinCodeDisplay.text = joinCode;
                joinCodeDisplay.gameObject.SetActive(true);
                // ODA KODU label'ını da göster
                foreach (Transform child in lobbyPopup.transform)
                {
                    Text t = child.GetComponent<Text>();
                    if (t != null && t.text == "ODA KODU:")
                        child.gameObject.SetActive(true);
                }
            }

            // Transport'u Relay ile konfigüre et
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

            ConfigureNetworkManager();
            NetworkManager.Singleton.StartHost();

            SetStatus($"Oda kuruldu! Kod: {joinCode}");

            // Oyunu başlat
            await Task.Delay(500); // NetworkManager'ın başlamasını bekle
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGameFromMenu(playerCount);
            }
            else
            {
                GameObject gmPrefab = Resources.Load<GameObject>("GameManagerPrefab");
                if (gmPrefab != null)
                {
                    GameObject gmObj = Instantiate(gmPrefab);
                    gmObj.GetComponent<NetworkObject>().Spawn();
                    gmObj.GetComponent<GameManager>().StartGameFromMenu(playerCount);
                }
            }

            // Menüyü gizle
            menuCanvasObj.SetActive(false);
            AudioManager.Instance?.StopAmbiance();
        }
        catch (System.Exception e)
        {
            SetStatus($"HATA: {e.Message}");
            Debug.LogError($"[Relay] Host başlatma hatası: {e}");
        }
    }

    private void JoinGame()
    {
        string code = joinCodeInput != null ? joinCodeInput.text.Trim().ToUpper() : "";
        if (string.IsNullOrEmpty(code) || code.Length < 4)
        {
            SetStatus("Lütfen geçerli bir oda kodu girin!");
            return;
        }
        JoinWithRelay(code);
    }

    private async void JoinWithRelay(string joinCode)
    {
        SetStatus("Odaya bağlanılıyor...");
        
        try
        {
            await InitializeUnityServices();
            SetStatus($"Kod: {joinCode} ile bağlanılıyor...");

            // Relay'e katıl
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // Transport'u Relay ile konfigüre et
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            ConfigureNetworkManager();
            NetworkManager.Singleton.StartClient();

            SetStatus("Bağlandı! Oyun yükleniyor...");

            // Menüyü gizle
            menuCanvasObj.SetActive(false);
            AudioManager.Instance?.StopAmbiance();
        }
        catch (System.Exception e)
        {
            SetStatus($"HATA: {e.Message}");
            Debug.LogError($"[Relay] Bağlantı hatası: {e}");
        }
    }
}
