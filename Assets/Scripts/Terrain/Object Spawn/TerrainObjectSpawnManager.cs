using UnityEngine;

public class TerrainObjectSpawnManager : MonoBehaviour
{
    [Header("<b><color=#FFE066>Terrain Object Spawn Manager</color></b>\n<i><color=#FFF2B2>Manages the spawning of objects on the terrain</color></i>")]
    
    [Header("References")]
    [SerializeField] private TerrainObjectSpawner terrainObjectSpawner_largePineTrees;
    
    public void SpawnObjects()
    {
        if (terrainObjectSpawner_largePineTrees == null)
        {
            Debug.LogError("TerrainObjectSpawnManager: Missing terrain object spawner for large pine trees!");
            return;
        }

        // Trigger the spawn process
        terrainObjectSpawner_largePineTrees.SpawnObjects();
    }
}
