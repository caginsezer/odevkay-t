using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using Unity.Netcode.Components;

public class SyncFixer
{
    [InitializeOnLoadMethod]
    static void FixPrefab()
    {
        string path = "Assets/Resources/MagnetPiecePrefab.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            if (prefab.GetComponent<NetworkRigidbody>() == null)
            {
                prefab.AddComponent<NetworkRigidbody>();
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=cyan>✨ SİHİRLİ DOKUNUŞ: NetworkRigidbody eklendi (Fizik senkronizasyonu mükemmelleştirildi).</color>");
            }
        }
    }
}
