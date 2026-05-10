using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private Canvas mainCanvas;
    private GameObject canvasObj;

    private Text[] playerMagnetsText = new Text[4];
    private Text[] playerScoreText = new Text[4];
    private GameObject[] playerPanelObjs = new GameObject[4];

    private Text turnText;
    private Image timerBarFill;
    private GameObject winPanel;
    private Text winText;

    private float pulseTime = 0f;
    private int activePlayer = 1;

    // Oyuncu renkleri
    private readonly Color[] playerColors = new Color[] {
        new Color(0.1f, 0.85f, 1f),   // P1 Mavi
        new Color(1f, 0.25f, 0.25f),   // P2 Kırmızı
        new Color(0.2f, 1f, 0.3f),     // P3 Yeşil
        new Color(1f, 0.75f, 0.1f)     // P4 Sarı
    };

    public void Initialize()
    {
        CreateUI();
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged += OnTurnChanged;
            GameManager.Instance.OnMagnetsUpdated += OnMagnetsUpdated;
            GameManager.Instance.OnScoresUpdated += OnScoresUpdated;
            GameManager.Instance.OnStatusMessage += OnStatusMessage;
            GameManager.Instance.OnGameOver += OnGameOver;
            GameManager.Instance.OnTimerUpdated += OnTimerUpdated;
        }
    }

    private Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    private void CreateUI()
    {
        canvasObj = new GameObject("UICanvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // ========== ALT PANEL (Player 1) ==========
        CreateCompactPanel(0, 
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 0), new Vector2(0, 75));

        // ========== ÜST PANEL (Player 2) ==========
        CreateCompactPanel(1, 
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -75), new Vector2(0, 0));

        // ========== SOL PANEL (Player 3) ==========
        CreateCompactSidePanel(2,
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(0, 80), new Vector2(55, -80));

        // ========== SAĞ PANEL (Player 4) ==========
        CreateCompactSidePanel(3,
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
            new Vector2(-55, 80), new Vector2(0, -80));

        // ========== ZAMANLAYICI (Timer Bar - Alt Ortada) ==========
        CreateTimerBar();

        // ========== KAZANMA EKRANI ==========
        CreateWinScreen();

        canvasObj.SetActive(false);
    }

    private void CreateCompactPanel(int idx, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
    {
        string[] names = { "P1 ▼", "P2 ▲", "P3 ◄", "P4 ►" };
        Color c = playerColors[idx];

        GameObject panel = new GameObject($"Player{idx+1}Panel");
        panel.transform.SetParent(canvasObj.transform, false);

        // Yarı saydam koyu arka plan
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.04f, 0.80f);

        // Raycasting kapalı - taşlara tıklamayı engellemesin!
        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        playerPanelObjs[idx] = panel;

        // Renkli üst/alt şerit (accent line)
        GameObject accent = new GameObject("Accent");
        accent.transform.SetParent(panel.transform, false);
        Image accentImg = accent.AddComponent<Image>();
        accentImg.color = c;
        RectTransform acRt = accent.GetComponent<RectTransform>();
        if (idx == 0) { // Alt panel -> accent çizgisi üstte
            acRt.anchorMin = new Vector2(0, 1); acRt.anchorMax = Vector2.one;
            acRt.offsetMin = new Vector2(0, -3); acRt.offsetMax = Vector2.zero;
        } else { // Üst panel -> accent çizgisi altta
            acRt.anchorMin = Vector2.zero; acRt.anchorMax = new Vector2(1, 0);
            acRt.offsetMin = Vector2.zero; acRt.offsetMax = new Vector2(0, 3);
        }

        // İsim
        GameObject nameTxt = CreateSimpleText(panel.transform, names[idx], 24, FontStyle.Bold, c);
        RectTransform nRt = nameTxt.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0, 0); nRt.anchorMax = new Vector2(0.3f, 1);
        nRt.offsetMin = new Vector2(15, 0); nRt.offsetMax = Vector2.zero;
        nameTxt.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

        // Taş Sayısı
        GameObject magTxt = CreateSimpleText(panel.transform, "⬢ 18", 26, FontStyle.Bold, Color.white);
        RectTransform mRt = magTxt.GetComponent<RectTransform>();
        mRt.anchorMin = new Vector2(0.3f, 0); mRt.anchorMax = new Vector2(0.6f, 1);
        mRt.offsetMin = Vector2.zero; mRt.offsetMax = Vector2.zero;
        playerMagnetsText[idx] = magTxt.GetComponent<Text>();

        // Skor
        GameObject scoreTxt = CreateSimpleText(panel.transform, "★ 0", 26, FontStyle.Bold, new Color(1f, 0.85f, 0.3f));
        RectTransform sRt = scoreTxt.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.6f, 0); sRt.anchorMax = new Vector2(1f, 1);
        sRt.offsetMin = Vector2.zero; sRt.offsetMax = new Vector2(-15, 0);
        scoreTxt.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
        playerScoreText[idx] = scoreTxt.GetComponent<Text>();
    }

    private void CreateCompactSidePanel(int idx, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
    {
        string[] names = { "P1", "P2", "P3", "P4" };
        Color c = playerColors[idx];

        GameObject panel = new GameObject($"Player{idx+1}Panel");
        panel.transform.SetParent(canvasObj.transform, false);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.04f, 0.80f);

        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        playerPanelObjs[idx] = panel;

        // Renkli yan şerit
        GameObject accent = new GameObject("Accent");
        accent.transform.SetParent(panel.transform, false);
        Image accentImg = accent.AddComponent<Image>();
        accentImg.color = c;
        RectTransform acRt = accent.GetComponent<RectTransform>();
        if (idx == 2) { // Sol -> accent sağda
            acRt.anchorMin = new Vector2(1, 0); acRt.anchorMax = Vector2.one;
            acRt.offsetMin = new Vector2(-3, 0); acRt.offsetMax = Vector2.zero;
        } else { // Sağ -> accent solda
            acRt.anchorMin = Vector2.zero; acRt.anchorMax = new Vector2(0, 1);
            acRt.offsetMin = Vector2.zero; acRt.offsetMax = new Vector2(3, 0);
        }

        // İsim (üst kısım)
        GameObject nameTxt = CreateSimpleText(panel.transform, names[idx], 20, FontStyle.Bold, c);
        RectTransform nRt = nameTxt.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0, 0.7f); nRt.anchorMax = Vector2.one;
        nRt.offsetMin = Vector2.zero; nRt.offsetMax = Vector2.zero;

        // Taş (orta)
        GameObject magTxt = CreateSimpleText(panel.transform, "⬢18", 18, FontStyle.Bold, Color.white);
        RectTransform mRt = magTxt.GetComponent<RectTransform>();
        mRt.anchorMin = new Vector2(0, 0.35f); mRt.anchorMax = new Vector2(1, 0.7f);
        mRt.offsetMin = Vector2.zero; mRt.offsetMax = Vector2.zero;
        playerMagnetsText[idx] = magTxt.GetComponent<Text>();

        // Skor (alt)
        GameObject scoreTxt = CreateSimpleText(panel.transform, "★0", 18, FontStyle.Bold, new Color(1f, 0.85f, 0.3f));
        RectTransform sRt = scoreTxt.GetComponent<RectTransform>();
        sRt.anchorMin = Vector2.zero; sRt.anchorMax = new Vector2(1, 0.35f);
        sRt.offsetMin = Vector2.zero; sRt.offsetMax = Vector2.zero;
        playerScoreText[idx] = scoreTxt.GetComponent<Text>();
    }

    private void CreateTimerBar()
    {
        // Timer container
        GameObject timerObj = new GameObject("TimerBar");
        timerObj.transform.SetParent(canvasObj.transform, false);
        Image tbBg = timerObj.AddComponent<Image>();
        tbBg.color = new Color(0.05f, 0.05f, 0.08f, 0.90f);

        CanvasGroup cg = timerObj.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        RectTransform tbRt = timerObj.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0.15f, 0f);
        tbRt.anchorMax = new Vector2(0.85f, 0f);
        tbRt.pivot = new Vector2(0.5f, 0f);
        tbRt.offsetMin = new Vector2(0, 78);
        tbRt.offsetMax = new Vector2(0, 115);

        // Fill bar
        GameObject fillObj = new GameObject("TimerFill");
        fillObj.transform.SetParent(timerObj.transform, false);
        timerBarFill = fillObj.AddComponent<Image>();
        timerBarFill.color = new Color(0.1f, 0.85f, 1f);
        timerBarFill.type = Image.Type.Filled;
        timerBarFill.fillMethod = Image.FillMethod.Horizontal;
        RectTransform fRt = fillObj.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
        fRt.offsetMin = new Vector2(2, 2); fRt.offsetMax = new Vector2(-2, -2);

        // Rounded corner effect with outline
        Outline ol = timerObj.AddComponent<Outline>();
        ol.effectColor = new Color(0.2f, 0.6f, 1f, 0.4f);
        ol.effectDistance = new Vector2(1, -1);

        // Turn text overlay
        GameObject turnTxtObj = CreateSimpleText(timerObj.transform, "SIRA BEKLENİYOR", 18, FontStyle.Bold, Color.white);
        RectTransform tRt = turnTxtObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        turnText = turnTxtObj.GetComponent<Text>();
    }

    private GameObject CreateSimpleText(Transform parent, string text, int size, FontStyle style, Color color)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        Text t = obj.AddComponent<Text>();
        t.font = GetDefaultFont();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow sh = obj.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.7f);
        sh.effectDistance = new Vector2(1, -1);

        return obj;
    }

    private void CreateWinScreen()
    {
        winPanel = new GameObject("WinPanel");
        winPanel.transform.SetParent(canvasObj.transform, false);
        Image bgImg = winPanel.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.02f, 0.05f, 0.92f);
        RectTransform rt = winPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        // Kazanan yazısı
        GameObject winTxtObj = CreateSimpleText(winPanel.transform, "KAZANAN!", 65, FontStyle.Bold, new Color(1f, 0.85f, 0.2f));
        winText = winTxtObj.GetComponent<Text>();
        RectTransform wRt = winTxtObj.GetComponent<RectTransform>();
        wRt.anchorMin = new Vector2(0, 0.5f); wRt.anchorMax = new Vector2(1, 0.75f);
        wRt.offsetMin = Vector2.zero; wRt.offsetMax = Vector2.zero;
        Outline wol = winTxtObj.AddComponent<Outline>();
        wol.effectColor = new Color(1f, 0.5f, 0f, 0.6f);
        wol.effectDistance = new Vector2(3, -3);

        // Yeniden başla butonu
        GameObject rBtn = new GameObject("RestartBtn");
        rBtn.transform.SetParent(winPanel.transform, false);
        Image bImg = rBtn.AddComponent<Image>();
        bImg.color = new Color(0.15f, 0.7f, 0.3f, 0.95f);
        RectTransform bRt = rBtn.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.5f, 0.25f); bRt.anchorMax = new Vector2(0.5f, 0.25f);
        bRt.pivot = new Vector2(0.5f, 0.5f);
        bRt.sizeDelta = new Vector2(450, 100);

        Button btn = rBtn.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            if (Unity.Netcode.NetworkManager.Singleton != null) Unity.Netcode.NetworkManager.Singleton.Shutdown();
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        });

        Outline bol = rBtn.AddComponent<Outline>();
        bol.effectColor = new Color(0.2f, 1f, 0.4f, 0.5f);
        bol.effectDistance = new Vector2(2, -2);

        CreateSimpleText(rBtn.transform, "ANA MENÜYE DÖN", 30, FontStyle.Bold, Color.white);
        RectTransform btnTxtRt = rBtn.transform.GetChild(0).GetComponent<RectTransform>();
        btnTxtRt.anchorMin = Vector2.zero; btnTxtRt.anchorMax = Vector2.one;
        btnTxtRt.offsetMin = Vector2.zero; btnTxtRt.offsetMax = Vector2.zero;

        winPanel.SetActive(false);
    }

    public void ShowGameUI()
    {
        if (canvasObj != null) canvasObj.SetActive(true);

        // GameManager'a geç bağlan (eğer daha önce bağlanamadıysa)
        if (GameManager.Instance != null && GameManager.Instance.OnTurnChanged == null)
        {
            SubscribeToEvents();
        }
    }

    private void Update()
    {
        if (canvasObj == null || !canvasObj.activeSelf) return;

        // Aktif oyuncunun panelini nabız efektiyle vurgula
        pulseTime += Time.deltaTime * 4f;
        float pulse = (Mathf.Sin(pulseTime) + 1f) / 2f;

        for (int i = 0; i < 4; i++)
        {
            if (playerPanelObjs[i] == null) continue;
            Image bg = playerPanelObjs[i].GetComponent<Image>();
            if (bg == null) continue;

            if (i + 1 == activePlayer)
            {
                // Aktif oyuncu: parlayan arka plan
                Color ac = playerColors[i];
                bg.color = new Color(ac.r * 0.15f, ac.g * 0.15f, ac.b * 0.15f, 0.7f + pulse * 0.25f);
            }
            else
            {
                bg.color = new Color(0.02f, 0.02f, 0.04f, 0.70f);
            }
        }

        // SubscribeToEvents tekrar dene (GameManager geç oluşabilir)
        if (GameManager.Instance != null && GameManager.Instance.OnTurnChanged == null)
        {
            SubscribeToEvents();
        }
    }

    private void OnTurnChanged(int player)
    {
        activePlayer = player;
        if (turnText != null)
        {
            turnText.text = $"OYUNCU {player} SIRASI";
            Color pc = playerColors[Mathf.Clamp(player - 1, 0, 3)];
            turnText.color = pc;
            if (timerBarFill != null) timerBarFill.color = pc;
        }
    }

    private void OnMagnetsUpdated(int p1, int p2, int p3, int p4)
    {
        int[] vals = { p1, p2, p3, p4 };
        for (int i = 0; i < 4; i++)
        {
            if (playerMagnetsText[i] != null)
            {
                // Yan paneller daha kısa
                if (i < 2)
                    playerMagnetsText[i].text = $"⬢ {vals[i]}";
                else
                    playerMagnetsText[i].text = $"⬢{vals[i]}";
            }
        }

        int total = 0;
        if (GameManager.Instance != null) total = GameManager.Instance.totalPlayers.Value;
        for (int i = 0; i < 4; i++)
        {
            if (playerPanelObjs[i] != null)
                playerPanelObjs[i].SetActive(i < total);
        }
    }

    private void OnScoresUpdated(int p1, int p2, int p3, int p4)
    {
        int[] vals = { p1, p2, p3, p4 };
        for (int i = 0; i < 4; i++)
        {
            if (playerScoreText[i] != null)
            {
                if (i < 2)
                    playerScoreText[i].text = $"★ {vals[i]}";
                else
                    playerScoreText[i].text = $"★{vals[i]}";
            }
        }
    }

    private void OnTimerUpdated(float normalizedTime)
    {
        if (timerBarFill != null) timerBarFill.fillAmount = normalizedTime;
    }

    private void OnStatusMessage(string msg) { }

    private void OnGameOver(int winner)
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            if (winText != null)
            {
                winText.text = $"OYUNCU {winner} KAZANDI!";
                winText.color = playerColors[Mathf.Clamp(winner - 1, 0, 3)];
            }
        }
    }
}
