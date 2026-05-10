using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

[InitializeOnLoad]
public class MultiplayerAutoSetup
{
    static MultiplayerAutoSetup()
    {
        EditorApplication.delayCall += RunSetup;
    }

    private static void RunSetup()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        bool changed = false;

        GameObject gmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/GameManagerPrefab.prefab");
        if (gmPrefab == null)
        {
            GameObject obj = new GameObject("GameManagerPrefab");
            obj.AddComponent<NetworkObject>();
            obj.AddComponent<GameManager>();
            PrefabUtility.SaveAsPrefabAsset(obj, "Assets/Resources/GameManagerPrefab.prefab");
            Object.DestroyImmediate(obj);
            changed = true;
        }

        GameObject magnetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/MagnetPiecePrefab.prefab");
        if (magnetPrefab == null)
        {
            GameObject obj = new GameObject("MagnetPiecePrefab");
            obj.AddComponent<MagnetPiece>();
            obj.AddComponent<NetworkObject>();
            obj.AddComponent<Unity.Netcode.Components.NetworkTransform>();
            
            SphereCollider col = obj.AddComponent<SphereCollider>();
            col.radius = 0.26f;

            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.mass = 1.0f;
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;

            PrefabUtility.SaveAsPrefabAsset(obj, "Assets/Resources/MagnetPiecePrefab.prefab");
            Object.DestroyImmediate(obj);
            changed = true;
        }

        GameObject nmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/NetworkManagerPrefab.prefab");
        if (nmPrefab == null)
        {
            GameObject obj = new GameObject("NetworkManagerPrefab");
            NetworkManager nm = obj.AddComponent<NetworkManager>();
            UnityTransport ut = obj.AddComponent<UnityTransport>();
            
            // To ensure compatibility and clean setup, we set the transport directly.
            // The NetworkConfig automatically handles standard setup, we just need to assign the transport if needed.
            // In newer NGO versions, assigning NetworkConfig fields via code is tricky due to internal lists,
            // but nm.NetworkConfig.NetworkTransport is accessible.
            nm.NetworkConfig.NetworkTransport = ut;
            
            GameObject loadedMagnet = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/MagnetPiecePrefab.prefab");
            if (loadedMagnet != null)
            {
                nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = loadedMagnet });
            }

            GameObject loadedGm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/GameManagerPrefab.prefab");
            if (loadedGm != null)
            {
                nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = loadedGm });
            }

            PrefabUtility.SaveAsPrefabAsset(obj, "Assets/Resources/NetworkManagerPrefab.prefab");
            Object.DestroyImmediate(obj);
            changed = true;
        }
        else
        {
            NetworkManager nm = nmPrefab.GetComponent<NetworkManager>();
            bool hasMagnet = false;
            foreach (var p in nm.NetworkConfig.Prefabs.Prefabs)
            {
                if (p.Prefab != null && p.Prefab.name == "MagnetPiecePrefab") hasMagnet = true;
            }
            if (!hasMagnet)
            {
                GameObject loadedMagnet = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/MagnetPiecePrefab.prefab");
                if (loadedMagnet != null)
                {
                    nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = loadedMagnet });
                    changed = true;
                }
            }

            bool hasGm = false;
            foreach (var p in nm.NetworkConfig.Prefabs.Prefabs)
            {
                if (p.Prefab != null && p.Prefab.name == "GameManagerPrefab") hasGm = true;
            }
            if (!hasGm)
            {
                GameObject loadedGm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/GameManagerPrefab.prefab");
                if (loadedGm != null)
                {
                    nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = loadedGm });
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(nmPrefab);
                AssetDatabase.SaveAssets();
            }
        }

        if (changed)
        {
            Debug.Log("[MultiplayerAutoSetup] NetworkManager and Prefabs configured successfully.");
        }
    }
}
