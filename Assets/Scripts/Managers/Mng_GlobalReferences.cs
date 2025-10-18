using UnityEngine;

public class Mng_GlobalReferences : MonoBehaviour
{
    // Global references
    [Header("Global References")]
    public Terrain terrain;
    public Transform player;
    public Transform playerCamera;
    
    // Getters for easy access
    public Terrain GetTerrain() => terrain;
    public Transform GetPlayer() => player;
    public Transform GetPlayerCamera() => playerCamera;
}
