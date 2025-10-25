using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement; // To get current scene info
using UnityEngine.SceneManagement;
using System.IO;

public class WorldSaver
{
    [MenuItem("Tools/Save World Assets")]
    static void SaveWorldAssets()
    {
        // Change this to your Prototype Game Scene name
        // string targetSceneName = "PCG - Terrain";
        string targetSceneName = "PCG - Terrain Generation (Cinamatics Rendering)";

        // Get active scene
        Scene currentScene = EditorSceneManager.GetActiveScene();

        if (currentScene.name != targetSceneName)
        {
            Debug.LogWarning("⚠️ You are not in the prototype game scene. Current scene: " + currentScene.name);
            return;
        }

        // Base save paths
        // string basePath = "Assets/Prototype Game ( With PCG Terrain )/TerrainSaves/";
        string basePath = "Assets/Scenes/Saved Assets For Cenematics Rendering/";
        string terrainPath = Path.Combine(basePath, "SavedTerrain.asset");
        string prefabFolder = Path.Combine(basePath, "Prefabs/");

        // Ensure directories exist
        if (!AssetDatabase.IsValidFolder(basePath))
            AssetDatabase.CreateFolder("Assets", "Prototype Game ( With PCG Terrain )/TerrainSaves");
        if (!AssetDatabase.IsValidFolder(prefabFolder))
            AssetDatabase.CreateFolder(basePath.TrimEnd('/'), "Prefabs");

        // --- Save Terrain Data ---
        Terrain terrain = GameObject.FindObjectOfType<Terrain>();
        if (terrain != null)
        {
            // Make sure terrain path is unique each save
            string uniqueTerrainPath = AssetDatabase.GenerateUniqueAssetPath(terrainPath);

            TerrainData newTerrainData = Object.Instantiate(terrain.terrainData);
            AssetDatabase.CreateAsset(newTerrainData, uniqueTerrainPath);
            AssetDatabase.SaveAssets();

            // Reassign to scene terrain
            terrain.terrainData = newTerrainData;

            Debug.Log("✅ TerrainData saved and reassigned: " + uniqueTerrainPath);
        }
        else
        {
            Debug.LogWarning("⚠️ No Terrain found to save.");
        }

        // --- Save Tree Parents ---
        GameObject[] treeParents = GameObject.FindGameObjectsWithTag("SaveableAsPrefab");
        if (treeParents.Length > 0)
        {
            foreach (GameObject root in treeParents)
            {
                string prefabPath = Path.Combine(prefabFolder, root.name + ".prefab");
                string uniquePrefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

                PrefabUtility.SaveAsPrefabAssetAndConnect(root, uniquePrefabPath, InteractionMode.UserAction);
                Debug.Log("🌲 Saved & connected prefab: " + uniquePrefabPath);
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No objects with tag 'SaveableAsPrefab' found.");
        }

        Debug.Log("🎉 World assets saved successfully!");
    }
}
