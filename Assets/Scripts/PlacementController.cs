using UnityEngine;
using Unity.Netcode;

public class PlacementController : MonoBehaviour
{
    private BoardSetup boardSetup;
    private Camera mainCamera;

    private Texture2D handCursorTex;
    private GameObject handCursorObj;

    private MagnetPiece draggedStone = null;
    private bool isDragging = false;
    private int draggingPlayerID = 0;

    private const float PANEL_THRESHOLD = 3.6f; // Dikey eksen sınırı
    private const float PANEL_THRESHOLD_X = 2.8f; // Yatay eksen sınırı

    public void Initialize(BoardSetup board, Camera camera)
    {
        boardSetup = board;
        mainCamera = camera;

        try {
            string texPath = System.IO.Path.Combine(Application.dataPath, "Textures", "hand_cursor.png");
            if (System.IO.File.Exists(texPath))
            {
                byte[] fd = System.IO.File.ReadAllBytes(texPath);
                handCursorTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                handCursorTex.LoadImage(fd);
            }
        } catch (System.Exception e) {
            Debug.LogWarning("[PlacementController] Hand cursor load failed: " + e.Message);
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        
        // NetworkManager kontrolü - bağlı değilse (henüz host/client başlamamışsa) çık
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        int localPlayerID = 1;
        if (NetworkManager.Singleton.IsServer)
        {
            // Eğer bağlı başka bir istemci yoksa (tek cihazda test veya Hotseat oynanıyorsa),
            // o anki aktif oyuncu kimse onun taşlarını kontrol etmeye izin ver.
            if (NetworkManager.Singleton.ConnectedClientsList.Count <= 1)
            {
                localPlayerID = GameManager.Instance.currentPlayer.Value;
            }
            else
            {
                localPlayerID = 1; // Online oyunda Host her zaman 1. oyuncudur
            }
        }
        else
        {
            // İstemciler sadece kendi ID'lerine ait taşları oynatabilir
            localPlayerID = (int)NetworkManager.Singleton.LocalClientId + 1;
        }

        if (GameManager.Instance.currentState.Value != GameManager.GameState.WaitingForPlacement || 
            GameManager.Instance.currentPlayer.Value != localPlayerID)
        {
            if (isDragging) CancelDrag();
            return;
        }

        HandleUnifiedInput(localPlayerID);
    }

    private void HandleUnifiedInput(int localPlayerID)
    {
        if (UnityEngine.InputSystem.Pointer.current == null) return;

        var pointer = UnityEngine.InputSystem.Pointer.current;
        Vector2 screenPos = pointer.position.ReadValue();

        if (pointer.press.wasPressedThisFrame)
        {
            TryStartDrag(screenPos, localPlayerID);
        }
        else if (pointer.press.isPressed && isDragging)
        {
            MoveDraggedStone(screenPos);
        }
        else if (pointer.press.wasReleasedThisFrame && isDragging)
        {
            TryDropStone(screenPos, localPlayerID);
        }
    }

    private void TryStartDrag(Vector2 screenPos, int localPlayerID)
    {
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        if (StoneReserveManager.Instance == null || !StoneReserveManager.Instance.HasStones(localPlayerID))
            return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        GameObject hitStone = null;
        RaycastHit[] hits = Physics.RaycastAll(ray, 30f, Physics.AllLayers, QueryTriggerInteraction.Collide);
        float closestDist = float.MaxValue;

        string prefix = $"ReserveStone_P{localPlayerID}_";
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject.name.StartsWith(prefix) && hit.distance < closestDist)
            {
                closestDist = hit.distance;
                hitStone = hit.collider.gameObject;
            }
        }

        Vector3 spawnPos = Vector3.zero;

        if (hitStone != null)
        {
            if (!StoneReserveManager.Instance.TryConsumeSpecificStone(localPlayerID, hitStone))
                return;
            spawnPos = new Vector3(hitStone.transform.position.x, 0.25f, hitStone.transform.position.z);
        }
        else
        {
            Vector3 worldPos = GetWorldPoint(screenPos);
            if (worldPos == Vector3.positiveInfinity) return;

            bool inPanel = false;
            if (localPlayerID == 1 && worldPos.z < -PANEL_THRESHOLD) inPanel = true;
            else if (localPlayerID == 2 && worldPos.z > PANEL_THRESHOLD) inPanel = true;
            else if (localPlayerID == 3 && worldPos.x < -PANEL_THRESHOLD_X) inPanel = true;
            else if (localPlayerID == 4 && worldPos.x > PANEL_THRESHOLD_X) inPanel = true;

            if (!inPanel) return;

            StoneReserveManager.Instance.TryConsumeStone(localPlayerID);
            spawnPos = new Vector3(worldPos.x, 0.25f, worldPos.z);
        }

        draggedStone = MagnetPiece.CreatePreviewMagnet(spawnPos, localPlayerID);

        if (handCursorTex != null)
        {
            handCursorObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            handCursorObj.name = "DragHandCursor";
            Destroy(handCursorObj.GetComponent<Collider>());
            handCursorObj.transform.SetParent(draggedStone.transform, false);
            handCursorObj.transform.localPosition = new Vector3(0.35f, 0.4f, -0.35f);
            handCursorObj.transform.localRotation = Quaternion.Euler(90f, 0f, 45f);
            handCursorObj.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Standard");
            Material mat = new Material(unlitShader);
            mat.mainTexture = handCursorTex;
            mat.SetFloat("_Surface", 1);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3500;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            handCursorObj.GetComponent<MeshRenderer>().material = mat;
        }

        isDragging = true;
        draggingPlayerID = localPlayerID;

        AudioManager.Instance?.Play(AudioManager.SoundType.StonePick);
    }

    private void MoveDraggedStone(Vector2 screenPos)
    {
        if (draggedStone == null) { isDragging = false; return; }

        Vector3 worldPos = GetWorldPoint(screenPos);
        if (worldPos == Vector3.positiveInfinity) return;

        draggedStone.transform.position = new Vector3(worldPos.x, 0.25f, worldPos.z);

        bool inside = boardSetup.IsWithinBounds(worldPos);
        if (draggedStone.meshRenderer != null)
        {
            Color gl = inside ? GameManager.Instance.GetPlayerColor(draggingPlayerID) * 1.5f : Color.red * 0.5f;
            draggedStone.meshRenderer.material.SetColor("_EmissionColor", gl);
        }

        if (handCursorObj != null)
        {
            float bounce = Mathf.Sin(Time.time * 6f) * 0.05f;
            handCursorObj.transform.localPosition = new Vector3(0.35f, 0.4f, -0.35f + bounce);
        }
    }

    private void TryDropStone(Vector2 screenPos, int localPlayerID)
    {
        if (draggedStone == null) { isDragging = false; return; }

        Vector3 worldPos = GetWorldPoint(screenPos);
        Vector3 dropPos = new Vector3(worldPos.x, 0.2f, worldPos.z);

        if (worldPos != Vector3.positiveInfinity && boardSetup.IsWithinBounds(dropPos))
        {
            // Başarılı. Görsel önizlemeyi sil, ServerRpc yolla!
            Destroy(draggedStone.gameObject);
            GameManager.Instance.TryPlaceStoneServerRpc(dropPos, localPlayerID);

            AudioManager.Instance?.Play(AudioManager.SoundType.StonePlaced);
        }
        else
        {
            // Başarısız, iade et
            Destroy(draggedStone.gameObject);
            StoneReserveManager.Instance?.ReturnStone(localPlayerID);
        }

        if (handCursorObj != null) Destroy(handCursorObj);
        draggedStone = null;
        isDragging = false;
    }

    private Vector3 GetWorldPoint(Vector2 screenPos)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return Vector3.positiveInfinity;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        Plane plane = new Plane(Vector3.up, new Vector3(0, 0.2f, 0));
        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return Vector3.positiveInfinity;
    }

    private void CancelDrag()
    {
        if (handCursorObj != null) Destroy(handCursorObj);
        if (draggedStone != null)
        {
            Destroy(draggedStone.gameObject);
            if (draggingPlayerID > 0)
                StoneReserveManager.Instance?.ReturnStone(draggingPlayerID);
            draggedStone = null;
        }
        isDragging = false;
    }

    public void ResetPreview()
    {
        CancelDrag();
    }
}
