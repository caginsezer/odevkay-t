using UnityEngine;

/// <summary>
/// Sahne başlatıcı - Tüm sistemleri sırayla ayağa kaldırır.
/// Dikey (Portrait) mod ve güvenli shader yüklemeleri içerir.
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnAfterSceneLoad()
    {
        if (GameManager.Instance != null) return;

        Debug.Log("=== Miknatıs Oyunu Baslatiliyor ===");

        // 1. EventSystem (UI tıklamaları için en başta olmalı)
        try {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                // Yeni Input System için gerekli modül
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Debug.Log("📱 Olay sistemi (New Input System) aktif edildi.");
            }
        } catch (System.Exception e) { Debug.LogError("EventSystem Init Error: " + e); }

        // 2. Temel Sistemler (Managerlar)
        try {
            if (Object.FindAnyObjectByType<AudioManager>() == null)
            {
                new GameObject("AudioManager").AddComponent<AudioManager>();
            }
        } catch (System.Exception e) { Debug.LogError("AudioManager Init Error: " + e); }

        try {
            if (Object.FindAnyObjectByType<IntroController>() == null)
            {
                new GameObject("IntroController").AddComponent<IntroController>();
            }
        } catch (System.Exception e) { Debug.LogError("IntroController Init Error: " + e); }

        try {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        } catch (System.Exception e) { Debug.LogError("GameManager Init Error: " + e); }

        // 3. Kamera Kurulumu
        try {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("MainCamera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.28f, 0.16f, 0.07f); // Koyu ahşap
            mainCam.nearClipPlane = 0.1f;
            mainCam.farClipPlane = 100f;
            mainCam.orthographic = true;
            mainCam.orthographicSize = 8.5f; // Dikey ekrana tam sığması için
            mainCam.transform.position = new Vector3(0, 15f, 0);
            mainCam.transform.rotation = Quaternion.Euler(90f, 0, 0);

            // 4. Kamera Kontrolcüsü
            CameraController camController = mainCam.gameObject.GetComponent<CameraController>();
            if (camController == null) camController = mainCam.gameObject.AddComponent<CameraController>();
            camController.SetupExistingCamera(mainCam);
        } catch (System.Exception e) { Debug.LogError("Camera Init Error: " + e); }

        // Global Volume/Post-process temizliği (Bazen URP projelerinde pembe ekrana sebep olabilir)
        try {
            var volumes = Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None);
            foreach (var vol in volumes) Object.DestroyImmediate(vol.gameObject);
        } catch (System.Exception e) { Debug.LogError("Volume Cleanup Error: " + e); }

        // 5. Işıklandırma
        try {
            SetupLighting();
        } catch (System.Exception e) { Debug.LogError("Lighting Setup Error: " + e); }

        // 6. Oyun Dünyası Bileşenleri
        GameObject boardObj = null;
        BoardSetup boardSetup = null;
        try {
            boardObj = new GameObject("BoardController");
            boardSetup = boardObj.AddComponent<BoardSetup>();
            boardSetup.CreateBoard();
        } catch (System.Exception e) { Debug.LogError("Board Setup Error: " + e); }

        try {
            GameObject reserveObj = new GameObject("StoneReserveManager");
            reserveObj.AddComponent<StoneReserveManager>();
        } catch (System.Exception e) { Debug.LogError("StoneReserveManager Setup Error: " + e); }

        try {
            GameObject placementObj = new GameObject("PlacementController");
            PlacementController placement = placementObj.AddComponent<PlacementController>();
            if (boardSetup != null) placement.Initialize(boardSetup, Camera.main);
        } catch (System.Exception e) { Debug.LogError("PlacementController Setup Error: " + e); }

        // 7. Arayüzler (UI)
        try {
            GameObject uiObj = new GameObject("UIManager");
            UIManager ui = uiObj.AddComponent<UIManager>();
            ui.Initialize();
        } catch (System.Exception e) { Debug.LogError("UIManager Init Error: " + e); }

        try {
            GameObject menuObj = new GameObject("MainMenuManager");
            MainMenuManager menu = menuObj.AddComponent<MainMenuManager>();
            menu.Initialize();
        } catch (System.Exception e) { Debug.LogError("MainMenuManager Init Error: " + e); }

        // 8. Mobil ve Uygulama Ayarları
        try {
            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        } catch (System.Exception e) { Debug.LogError("App Settings Error: " + e); }

        Debug.Log("=== Sahne Kurulumu Tamamlandı (Dikey Mod) ===");
    }

    static void SetupLighting()
    {
        Light existingLight = Object.FindAnyObjectByType<Light>();
        if (existingLight == null)
        {
            existingLight = new GameObject("DirectionalLight").AddComponent<Light>();
        }

        existingLight.type = LightType.Directional;
        existingLight.color = new Color(1f, 0.95f, 0.85f);
        existingLight.intensity = 1.6f;
        existingLight.shadows = LightShadows.Soft;
        existingLight.shadowStrength = 1.0f;
        existingLight.transform.rotation = Quaternion.Euler(35f, -45f, 0f);

        GameObject fillLight = new GameObject("FillLight");
        Light fill = fillLight.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.6f, 0.7f, 1f);
        fill.intensity = 0.5f;
        fillLight.transform.rotation = Quaternion.Euler(30, 150, 0);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);
    }
}
