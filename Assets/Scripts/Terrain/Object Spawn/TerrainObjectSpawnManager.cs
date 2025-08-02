using UnityEngine;

public class TerrainObjectSpawnManager : MonoBehaviour
{
    [Header(
        "<b><color=#FFE066>Terrain Object Spawn Manager</color></b>\n<i><color=#FFF2B2>Manages the spawning of objects on the terrain</color></i>")]
    [Header("References")]
    [SerializeField] private TerrainObjectSpawner largePineTrees_Grass;
    [SerializeField] private TerrainObjectSpawner largePineTrees_Forest;
    
    public void SpawnObjects()
    {
        // Trigger the spawn process
        largePineTrees_Grass.SpawnObjects();
        largePineTrees_Forest.SpawnObjects();
    }
}
