using UnityEngine;

public class BoardSetup : MonoBehaviour
{
    public static BoardSetup Instance { get; private set; }

    [Header("Saha Ayarları")]
    public float boardRadiusX = 2.5f;
    public float boardRadiusZ = 3.2f;
    public int ropeSegments = 64;
    public float ropeHeight = 0.05f;

    private void Awake()
    {
        Instance = this;
        boardRadiusX = 2.5f;
        boardRadiusZ = 3.2f;
    }

    private Material CreateURPMaterial(Color color, float smoothness = 0.5f, float metallic = 0f)
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(urpShader);
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        return mat;
    }

    private Material CreateUnlitMaterial()
    {
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        return new Material(unlitShader);
    }

    public void CreateBoard(int playerCount = 2)
    {
        // Temizle
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }

        CreateTrackRing();
        CreatePlayingField();
        CreateRopeBoundary(playerCount);
        CreateFloor();
        CreatePhysicalBoundary();
        CreateParticles();
    }

    private void CreateTrackRing()
    {
        GameObject trackObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trackObj.name = "TrackRing";
        trackObj.transform.SetParent(transform, false);
        trackObj.transform.position = new Vector3(0, -0.01f, 0); 
        trackObj.transform.localScale = new Vector3((boardRadiusX + 0.3f) * 2, 0.04f, (boardRadiusZ + 0.3f) * 2);

        MeshRenderer renderer = trackObj.GetComponent<MeshRenderer>();
        renderer.material = CreateURPMaterial(new Color(0.1f, 0.1f, 0.12f), 0.2f, 0f);
        Destroy(trackObj.GetComponent<Collider>());
    }

    private void CreatePlayingField()
    {
        GameObject boardObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        boardObject.name = "GameBoard";
        boardObject.transform.SetParent(transform, false);
        boardObject.transform.position = Vector3.zero;
        boardObject.transform.localScale = new Vector3(boardRadiusX * 2, 0.05f, boardRadiusZ * 2);

        MeshRenderer renderer = boardObject.GetComponent<MeshRenderer>();
        renderer.material = CreateURPMaterial(new Color(0.95f, 0.78f, 0.05f), 0.4f, 0.1f);

        boardObject.GetComponent<Collider>().isTrigger = false;
        Rigidbody boardRb = boardObject.AddComponent<Rigidbody>();
        boardRb.isKinematic = true;
    }

    private void CreateRopeBoundary(int playerCount)
    {
        if (playerCount <= 2)
        {
            CreateNeonArc("NeonGlow_P1_Bottom", new Color(0.1f, 0.85f, 1f), Mathf.PI, 2f * Mathf.PI);
            CreateNeonArc("NeonGlow_P2_Top", new Color(1f, 0.1f, 0.1f), 0f, Mathf.PI);
            CreateDivideLine(true);
        }
        else if (playerCount == 3)
        {
            CreateNeonArc("NeonGlow_P1_Bottom", new Color(0.1f, 0.85f, 1f), 1.33f * Mathf.PI, 1.66f * Mathf.PI);
            CreateNeonArc("NeonGlow_P2_Top", new Color(1f, 0.1f, 0.1f), 0.33f * Mathf.PI, 0.66f * Mathf.PI);
            CreateNeonArc("NeonGlow_P3_Left", new Color(0.2f, 1f, 0.2f), 0.66f * Mathf.PI, 1.33f * Mathf.PI);
            // Çizgiler karmaşık olabilir, bu yüzden şimdilik merkezde + çizelim
            CreateDivideLine(true);
            CreateDivideLine(false);
        }
        else
        {
            CreateNeonArc("NeonGlow_P1_Bottom", new Color(0.1f, 0.85f, 1f), 1.25f * Mathf.PI, 1.75f * Mathf.PI);
            CreateNeonArc("NeonGlow_P2_Top", new Color(1f, 0.1f, 0.1f), 0.25f * Mathf.PI, 0.75f * Mathf.PI);
            CreateNeonArc("NeonGlow_P3_Left", new Color(0.2f, 1f, 0.2f), 0.75f * Mathf.PI, 1.25f * Mathf.PI);
            CreateNeonArc("NeonGlow_P4_Right", new Color(1f, 0.8f, 0.1f), -0.25f * Mathf.PI, 0.25f * Mathf.PI);
            CreateDivideLine(true);
            CreateDivideLine(false);
        }
    }

    private void CreateNeonArc(string name, Color emitColor, float startAngle, float endAngle)
    {
        GameObject arcObj = new GameObject(name);
        arcObj.transform.SetParent(transform, false);
        arcObj.transform.position = new Vector3(0, ropeHeight + 0.05f, 0);

        LineRenderer lr = arcObj.AddComponent<LineRenderer>();
        int segments = ropeSegments / 2;
        lr.positionCount = segments + 1;
        lr.startWidth = 0.20f;
        lr.endWidth = 0.20f;
        lr.loop = false;
        lr.useWorldSpace = true;

        Material mat = CreateUnlitMaterial();
        mat.SetColor("_BaseColor", emitColor);
        mat.SetColor("_EmissionColor", emitColor * 3.0f);
        mat.EnableKeyword("_EMISSION");
        lr.material = mat;

        float adjX = boardRadiusX + 0.15f;
        float adjZ = boardRadiusZ + 0.15f;
        float angleStep = (endAngle - startAngle) / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + (i * angleStep);
            float x = Mathf.Cos(angle) * adjX;
            float z = Mathf.Sin(angle) * adjZ;
            lr.SetPosition(i, new Vector3(x, ropeHeight + 0.06f, z));
        }
    }

    private void CreateDivideLine(bool horizontal)
    {
        GameObject lineObj = new GameObject("DivideLine");
        lineObj.transform.SetParent(transform, false);
        lineObj.transform.position = new Vector3(0, ropeHeight + 0.05f, 0);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.useWorldSpace = true;

        Material mat = CreateUnlitMaterial();
        mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.3f));
        mat.SetFloat("_Surface", 1);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType", "Transparent");
        lr.material = mat;

        if (horizontal)
        {
            lr.SetPosition(0, new Vector3(-boardRadiusX, ropeHeight + 0.06f, 0));
            lr.SetPosition(1, new Vector3(boardRadiusX, ropeHeight + 0.06f, 0));
        }
        else
        {
            lr.SetPosition(0, new Vector3(0, ropeHeight + 0.06f, -boardRadiusZ));
            lr.SetPosition(1, new Vector3(0, ropeHeight + 0.06f, boardRadiusZ));
        }
    }

    private void CreateFloor()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "TableFloor";
        floor.transform.SetParent(transform, false);
        floor.transform.position = new Vector3(0, -0.4f, 0);
        floor.transform.localScale = new Vector3(4, 1, 4);

        MeshRenderer renderer = floor.GetComponent<MeshRenderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(s);
        
        try {
            string texPath = System.IO.Path.Combine(Application.dataPath, "Textures", "wood_bg.png");
            if (System.IO.File.Exists(texPath))
            {
                byte[] fd = System.IO.File.ReadAllBytes(texPath);
                Texture2D woodTex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                woodTex.LoadImage(fd);
                mat.mainTexture = woodTex;
                mat.mainTextureScale = new Vector2(4f, 4f);
                mat.SetFloat("_Smoothness", 0.4f);
            }
            else
            {
                mat.SetColor("_BaseColor", new Color(0.28f, 0.16f, 0.07f));
                mat.SetFloat("_Smoothness", 0.6f);
            }
        } catch {
            mat.SetColor("_BaseColor", new Color(0.28f, 0.16f, 0.07f));
            mat.SetFloat("_Smoothness", 0.6f);
        }
        
        renderer.material = mat;
    }

    private void CreatePhysicalBoundary()
    {
        GameObject boundaryParent = new GameObject("PhysicalBoundary");
        boundaryParent.transform.SetParent(transform, false);
        boundaryParent.transform.position = Vector3.zero;

        int colliderCount = 32;
        float adjX = boardRadiusX - 0.05f;
        float adjZ = boardRadiusZ - 0.05f;
        float angleStep = 360f / colliderCount;

        for (int i = 0; i < colliderCount; i++)
        {
            float angle = i * angleStep;
            float angleRad = angle * Mathf.Deg2Rad;
            
            Vector3 pos = new Vector3(Mathf.Cos(angleRad) * adjX, 0.2f, Mathf.Sin(angleRad) * adjZ);
            float nextAngleRad = (angle + 1f) * Mathf.Deg2Rad;
            Vector3 nextPos = new Vector3(Mathf.Cos(nextAngleRad) * adjX, 0.2f, Mathf.Sin(nextAngleRad) * adjZ);
            Vector3 tangent = (nextPos - pos).normalized;
            float lookAngle = Mathf.Atan2(tangent.x, tangent.z) * Mathf.Rad2Deg;

            GameObject wall = new GameObject($"Wall_{i}");
            wall.transform.position = pos;
            wall.transform.rotation = Quaternion.Euler(0, lookAngle, 0);
            wall.transform.parent = boundaryParent.transform;

            BoxCollider col = wall.AddComponent<BoxCollider>();
            col.size = new Vector3(0.1f, 0.5f, 0.8f);
        }
    }

    private void CreateParticles()
    {
        GameObject particles = new GameObject("BackgroundParticles");
        particles.transform.SetParent(transform, false);
        particles.transform.position = new Vector3(0, -0.2f, 0);

        ParticleSystem ps = particles.AddComponent<ParticleSystem>();
        
        // ParticleSystem'i önce durdur, ayarla, sonra başlat
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = 4f;
        main.startSpeed = 0.5f;
        main.startSize = 0.08f;
        main.startColor = new Color(0.2f, 0.8f, 1f, 0.3f);
        main.maxParticles = 80;

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(10f, 0.2f, 14f);

        // Tüm velocity eksenlerini aynı modda (TwoConstants) ayarla
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        vel.y = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
        if (s != null)
        {
            Material m = new Material(s);
            m.SetColor("_BaseColor", Color.white);
            renderer.material = m;
        }
        
        // Ayarlar bittikten sonra tekrar başlat
        ps.Play();
    }

    public bool IsWithinBounds(Vector3 position)
    {
        float localX = position.x;
        float localZ = position.z;
        float a = boardRadiusX - 0.2f;
        float b = boardRadiusZ - 0.2f;
        return (localX * localX) / (a * a) + (localZ * localZ) / (b * b) < 1.0f;
    }
}
