using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Mıknatıs parçası - Manyetik çekim fiziği (URP uyumlu)
/// </summary>
public class MagnetPiece : NetworkBehaviour
{
    [Header("Manyetik Özellikler")]
    public float attractionRadius = 2.0f;
    public float magneticForce = 15f;
    public float maxMagneticForce = 40f;

    [Header("Durum")]
    public NetworkVariable<int> ownerPlayer = new NetworkVariable<int>();
    public NetworkVariable<bool> isPlaced = new NetworkVariable<bool>();
    public bool isPreview = false;

    private Rigidbody rb;
    [HideInInspector] public MeshRenderer meshRenderer;
    public static List<MagnetPiece> allMagnets = new List<MagnetPiece>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Dinamik görsel katmanları oluştur (sadece ilk seferde)
        if (transform.childCount == 0)
        {
            BuildVisuals();
        }
        else
        {
            // Zaten varsa meshRenderer'ı bul
            Transform p = transform.Find("StonePebble");
            if (p != null) meshRenderer = p.GetComponent<MeshRenderer>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!isPreview)
        {
            if (!allMagnets.Contains(this)) allMagnets.Add(this);
        }

        ownerPlayer.OnValueChanged += (prev, current) => UpdateColor();
        UpdateColor();
    }

    public override void OnNetworkDespawn()
    {
        allMagnets.Remove(this);
    }

    public static void ClearAllMagnets()
    {
        allMagnets.Clear();
    }

    private float slideSpeed = 10.0f;
    private bool isAnimatingSlide = false;
    private Vector3 targetPosition;

    private void Update()
    {
        if (isPlaced == null || !isPlaced.Value || isPreview) return;

        // YANLARDAN KAYMA ANIMASYONU (havadan düşme yok)
        if (isAnimatingSlide)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, slideSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isAnimatingSlide = false;

                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                }
            }
        }
    }

    /// <summary>
    /// Taşı yan kenardan (oyuncunun tarafından) kaydırarak yerleştir
    /// </summary>
    public void StartSlideAnimation(Vector3 target, int playerNumber)
    {
        targetPosition = target;

        // Oyuncuya göre başlangıç noktası (ovalın kenarında, aynı X veya Z'de)
        float startX = target.x;
        float startZ = target.z;

        if (playerNumber == 1) startZ = -5.5f;
        else if (playerNumber == 2) startZ = 5.5f;
        else if (playerNumber == 3) startX = -4.5f;
        else if (playerNumber == 4) startX = 4.5f;

        transform.position = new Vector3(startX, target.y, startZ);
        isAnimatingSlide = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isPlaced == null || !isPlaced.Value || isPreview) return;

        MagnetPiece otherMagnet = collision.gameObject.GetComponent<MagnetPiece>();
        if (otherMagnet != null && otherMagnet.isPlaced != null && otherMagnet.isPlaced.Value && !otherMagnet.isPreview)
        {
            // Efekti tetikle
            CollisionBlastEffect.Spawn(collision.contacts[0].point);
        }
    }

    private void BuildVisuals()
    {
        float sx = Random.Range(0.48f, 0.58f);
        float sy = Random.Range(0.20f, 0.28f);
        float sz = Random.Range(0.42f, 0.52f);

        GameObject pebbleObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pebbleObj.name = "StonePebble";
        pebbleObj.transform.SetParent(transform, false);
        pebbleObj.transform.localPosition = Vector3.zero;
        pebbleObj.transform.localScale = new Vector3(sx, sy, sz);
        pebbleObj.transform.localRotation = Quaternion.Euler(
            Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
        Destroy(pebbleObj.GetComponent<Collider>());
        meshRenderer = pebbleObj.GetComponent<MeshRenderer>();

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mainMat = new Material(litShader);
        mainMat.SetColor("_BaseColor", new Color(0.10f, 0.10f, 0.12f));
        mainMat.SetFloat("_Metallic", 0.15f);
        mainMat.SetFloat("_Smoothness", 0.82f);
        meshRenderer.material = mainMat;

        GameObject highlightObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        highlightObj.name = "StoneHighlight";
        highlightObj.transform.SetParent(transform, false);
        highlightObj.transform.localPosition = new Vector3(-0.05f, 0.06f, -0.05f);
        highlightObj.transform.localScale = new Vector3(sx * 0.55f, sy * 0.55f, sz * 0.55f);
        Destroy(highlightObj.GetComponent<Collider>());
        Material hlMat = new Material(litShader);
        hlMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.0f));
        hlMat.SetFloat("_Metallic", 0f);
        hlMat.SetFloat("_Smoothness", 1.0f);
        hlMat.SetColor("_EmissionColor", new Color(0.7f, 0.75f, 0.85f) * 0.25f);
        hlMat.EnableKeyword("_EMISSION");
        hlMat.SetFloat("_Surface", 1);
        hlMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        hlMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        hlMat.SetInt("_ZWrite", 0);
        hlMat.renderQueue = 3000;
        hlMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        hlMat.SetOverrideTag("RenderType", "Transparent");
        highlightObj.GetComponent<MeshRenderer>().material = hlMat;

        GameObject glowRingObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowRingObj.name = "StoneGlow";
        glowRingObj.transform.SetParent(transform, false);
        glowRingObj.transform.localPosition = new Vector3(0, -0.04f, 0);
        glowRingObj.transform.localScale = new Vector3(sx * 1.15f, sy * 0.3f, sz * 1.15f);
        Destroy(glowRingObj.GetComponent<Collider>());
        Material glowMat = new Material(litShader);
        glowMat.SetColor("_BaseColor", Color.black);
        glowRingObj.GetComponent<MeshRenderer>().material = glowMat;

        UpdateColor();
    }

    private void UpdateColor()
    {
        if (meshRenderer == null || GameManager.Instance == null) return;

        Color playerColor = Color.white;
        int p = ownerPlayer.Value;
        if (isPreview && p == 0 && GameManager.Instance != null) p = GameManager.Instance.currentPlayer.Value;
        
        if (p > 0)
        {
            playerColor = GameManager.Instance.GetPlayerColor(p);
        }

        Transform glowRing = transform.Find("StoneGlow");
        if (glowRing != null)
        {
            Material glowMat = glowRing.GetComponent<MeshRenderer>().material;
            Color gc = playerColor * 1.8f;
            gc.a = 0.35f;
            glowMat.SetColor("_BaseColor", gc);
            glowMat.SetColor("_EmissionColor", gc * 2f);
            glowMat.EnableKeyword("_EMISSION");
            glowMat.SetFloat("_Surface", 1);
            glowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            glowMat.SetInt("_ZWrite", 0);
            glowMat.renderQueue = 3000;
            glowMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glowMat.SetOverrideTag("RenderType", "Transparent");
        }

        if (isPreview)
        {
            Material mainMat = meshRenderer.material;
            mainMat.SetFloat("_Surface", 1);
            mainMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mainMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mainMat.SetInt("_ZWrite", 0);
            mainMat.renderQueue = 3000;
            mainMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mainMat.SetOverrideTag("RenderType", "Transparent");
            Color previewColor = new Color(0.12f, 0.12f, 0.14f, 0.55f);
            mainMat.SetColor("_BaseColor", previewColor);
            mainMat.SetColor("_EmissionColor", playerColor * 0.8f);
            mainMat.EnableKeyword("_EMISSION");
        }
    }

    public static MagnetPiece CreatePreviewMagnet(Vector3 position, int playerNumber)
    {
        // Preview taşları ağ nesnesi değildir - basit bir yerel nesne oluştur
        GameObject obj = new GameObject("MagnetPreview");
        MagnetPiece magnet = obj.AddComponent<MagnetPiece>();
        magnet.isPreview = true;
        
        SphereCollider sCol = obj.AddComponent<SphereCollider>();
        sCol.radius = 0.26f;
        sCol.enabled = false;
        
        obj.transform.position = position;

        return magnet;
    }

    public void SetHighlight(bool highlighted)
    {
        if (meshRenderer == null) return;
        int p = ownerPlayer.Value;
        if (p == 0) return;

        Color baseColor = GameManager.Instance.GetPlayerColor(p);
        if (highlighted)
        {
            meshRenderer.material.SetColor("_EmissionColor", baseColor * 1.2f);
            meshRenderer.material.EnableKeyword("_EMISSION");
            meshRenderer.transform.localScale = new Vector3(0.6f, 0.35f, 0.6f); 
        }
        else
        {
            meshRenderer.material.DisableKeyword("_EMISSION");
            meshRenderer.transform.localScale = new Vector3(0.53f, 0.24f, 0.47f); 
        }
    }
}
