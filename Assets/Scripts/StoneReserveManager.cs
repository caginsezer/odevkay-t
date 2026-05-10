using UnityEngine;

public class StoneReserveManager : MonoBehaviour
{
    public static StoneReserveManager Instance { get; private set; }

    private GameObject[] p1Stones;
    private GameObject[] p2Stones;
    private GameObject[] p3Stones;
    private GameObject[] p4Stones;

    private const float P1_Z = -5.2f; // Alt
    private const float P2_Z =  5.2f; // Üst
    private const float P3_X = -4.5f; // Sol
    private const float P4_X =  4.5f; // Sağ
    private const float STONE_Y = 0.15f;

    private const int COLS_HORIZONTAL = 6;
    private const int COLS_VERTICAL = 2;
    private const float COL_SPACING = 0.65f; 
    private const float ROW_SPACING = 0.72f; 

    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(int stonesPerPlayer, int playerCount)
    {
        // Deterministik seed: Her iki editörde de aynı taş pozisyonları oluşsun
        Random.InitState(42);
        
        p1Stones = new GameObject[stonesPerPlayer];
        p2Stones = new GameObject[stonesPerPlayer];

        CreateSideBackground(1, new Vector3(0f, -0.05f, P1_Z), new Vector3(4.8f, 0.05f, 2.6f), new Color(0.1f, 0.85f, 1f));
        CreateSideBackground(2, new Vector3(0f, -0.05f, P2_Z), new Vector3(4.8f, 0.05f, 2.6f), new Color(1f, 0.1f, 0.1f));

        CreateReserveStones(1, stonesPerPlayer, new Vector3(0, 0, P1_Z), p1Stones, true);
        CreateReserveStones(2, stonesPerPlayer, new Vector3(0, 0, P2_Z), p2Stones, true);

        if (playerCount >= 3)
        {
            p3Stones = new GameObject[stonesPerPlayer];
            CreateSideBackground(3, new Vector3(P3_X, -0.05f, 0f), new Vector3(1.6f, 0.05f, 6.0f), new Color(0.2f, 1f, 0.2f));
            CreateReserveStones(3, stonesPerPlayer, new Vector3(P3_X, 0, 0), p3Stones, false);
        }

        if (playerCount >= 4)
        {
            p4Stones = new GameObject[stonesPerPlayer];
            CreateSideBackground(4, new Vector3(P4_X, -0.05f, 0f), new Vector3(1.6f, 0.05f, 6.0f), new Color(1f, 0.8f, 0.1f));
            CreateReserveStones(4, stonesPerPlayer, new Vector3(P4_X, 0, 0), p4Stones, false);
        }
    }

    private void CreateSideBackground(int player, Vector3 pos, Vector3 scale, Color outlineColor)
    {
        GameObject bgLayer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bgLayer.name = $"SidePanelBG_P{player}";
        bgLayer.transform.position = pos;
        bgLayer.transform.localScale = scale;
        Destroy(bgLayer.GetComponent<Collider>());

        MeshRenderer r = bgLayer.GetComponent<MeshRenderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material m = new Material(s);
        m.SetColor("_BaseColor", outlineColor * 0.15f);
        m.SetFloat("_Smoothness", 0.1f);
        r.material = m;

        GameObject glowLayer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glowLayer.name = $"SidePanelGlow_P{player}";
        glowLayer.transform.position = pos - new Vector3(0, 0.01f, 0);
        glowLayer.transform.localScale = scale + new Vector3(0.4f, 0f, 0.4f);
        Destroy(glowLayer.GetComponent<Collider>());
        
        Material glowMat = new Material(s);
        glowMat.SetColor("_BaseColor", outlineColor);
        glowMat.SetColor("_EmissionColor", outlineColor * 2.5f);
        glowMat.EnableKeyword("_EMISSION");
        glowLayer.GetComponent<MeshRenderer>().material = glowMat;
    }

    private void CreateReserveStones(int player, int count, Vector3 basePos, GameObject[] stoneArray, bool isHorizontal)
    {
        Color tintColor = GameManager.Instance.GetPlayerColor(player);

        for (int i = 0; i < count; i++)
        {
            Vector3 slotPos = GetSlotPosition(i, count, basePos, isHorizontal);
            slotPos.x += Random.Range(-0.05f, 0.05f);
            slotPos.z += Random.Range(-0.05f, 0.05f);

            GameObject stone = Create3DPebble(slotPos, tintColor);
            stone.name = $"ReserveStone_P{player}_S{i}";
            stoneArray[i] = stone;
        }
    }

    private Vector3 GetSlotPosition(int slotIdx, int totalSlots, Vector3 basePos, bool isHorizontal)
    {
        int cols = isHorizontal ? COLS_HORIZONTAL : COLS_VERTICAL;
        int totalRows = Mathf.CeilToInt(totalSlots / (float)cols);
        float totalWidth = (cols - 1) * COL_SPACING;
        float totalHeight = (totalRows - 1) * ROW_SPACING;

        int row = slotIdx / cols;
        int col = slotIdx % cols;

        float offsetX = (row % 2 == 0) ? 0f : (COL_SPACING * 0.45f);

        if (isHorizontal)
        {
            float x = basePos.x - totalWidth / 2f + col * COL_SPACING + offsetX;
            float z = basePos.z + totalHeight / 2f - row * ROW_SPACING;
            return new Vector3(x, STONE_Y, z);
        }
        else
        {
            // Dikey dizilim: X ekseninde dar (cols), Z ekseninde uzun (rows)
            float z = basePos.z + totalHeight / 2f - row * ROW_SPACING;
            float x = basePos.x - totalWidth / 2f + col * COL_SPACING + offsetX;
            return new Vector3(x, STONE_Y, z);
        }
    }

    private GameObject Create3DPebble(Vector3 pos, Color glowColor)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = pos;
        float sx = Random.Range(0.38f, 0.46f);
        float sy = Random.Range(0.16f, 0.22f);
        float sz = Random.Range(0.34f, 0.42f);
        sphere.transform.localScale = new Vector3(sx, sy, sz);
        sphere.transform.rotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));

        SphereCollider sc = sphere.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.85f; 

        MeshRenderer r = sphere.GetComponent<MeshRenderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(s);
        mat.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.14f));
        mat.SetFloat("_Metallic", 0.2f);
        mat.SetFloat("_Smoothness", 0.85f);
        r.material = mat;

        return sphere;
    }

    private GameObject[] GetArrayForPlayer(int player)
    {
        if (player == 1) return p1Stones;
        if (player == 2) return p2Stones;
        if (player == 3) return p3Stones;
        if (player == 4) return p4Stones;
        return null;
    }

    public bool TryConsumeSpecificStone(int player, GameObject stoneObj)
    {
        var array = GetArrayForPlayer(player);
        if (array == null) return false;
        
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == stoneObj)
            {
                array[i] = null;
                if (stoneObj != null) Destroy(stoneObj);
                return true;
            }
        }
        return false;
    }

    public bool TryConsumeStone(int player)
    {
        var array = GetArrayForPlayer(player);
        if (array == null) return false;

        for (int i = array.Length - 1; i >= 0; i--)
        {
            if (array[i] != null)
            {
                GameObject top = array[i];
                array[i] = null;
                Destroy(top);
                return true;
            }
        }
        return false;
    }

    public void ReturnStone(int player)
    {
        var array = GetArrayForPlayer(player);
        if (array == null) return;

        bool isHorizontal = (player == 1 || player == 2);
        Vector3 basePos = Vector3.zero;
        if (player == 1) basePos = new Vector3(0, 0, P1_Z);
        else if (player == 2) basePos = new Vector3(0, 0, P2_Z);
        else if (player == 3) basePos = new Vector3(P3_X, 0, 0);
        else if (player == 4) basePos = new Vector3(P4_X, 0, 0);

        Color tint = GameManager.Instance.GetPlayerColor(player);

        int emptySlot = -1;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == null)
            {
                emptySlot = i;
                break;
            }
        }

        if (emptySlot == -1) return; 

        Vector3 slotPos = GetSlotPosition(emptySlot, array.Length, basePos, isHorizontal);
        slotPos.x += Random.Range(-0.02f, 0.02f);
        slotPos.z += Random.Range(-0.02f, 0.02f);

        GameObject stone = Create3DPebble(slotPos, tint);
        stone.name = $"ReserveStone_P{player}_ReturnedS{emptySlot}";
        array[emptySlot] = stone;
    }

    public bool HasStones(int player)
    {
        var array = GetArrayForPlayer(player);
        if (array == null) return false;
        foreach (var s in array) if (s != null) return true;
        return false;
    }

    public void ClearAll()
    {
        if (p1Stones != null) foreach (var s in p1Stones) if (s) Destroy(s);
        if (p2Stones != null) foreach (var s in p2Stones) if (s) Destroy(s);
        if (p3Stones != null) foreach (var s in p3Stones) if (s) Destroy(s);
        if (p4Stones != null) foreach (var s in p4Stones) if (s) Destroy(s);
        
        p1Stones = null; p2Stones = null; p3Stones = null; p4Stones = null;

        // Arka plan silindirlerini de temizle
        for (int i = 1; i <= 4; i++)
        {
            GameObject bg = GameObject.Find($"SidePanelBG_P{i}");
            if (bg != null) Destroy(bg);

            GameObject glow = GameObject.Find($"SidePanelGlow_P{i}");
            if (glow != null) Destroy(glow);
        }
    }
}
