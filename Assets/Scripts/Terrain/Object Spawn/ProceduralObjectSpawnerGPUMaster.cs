using System;
using UnityEngine;

public class ProceduralObjectSpawnerGPUMaster : MonoBehaviour
{
    // Script Description:
    [Header("Procedural Object Spawner GPU Master")]
    
    [Header("References")]
    [SerializeField] private Transform player;
    
    [Header("Settings")]
    [SerializeField] private float chunkSize = 32f;

    public event Action onPlayerMovedToNewChunk;
        
    private Vector2Int _lastPlayerChunk;
    private float _nextRenderTime;
    
    void Update()
    {
        // OPTIMIZATION: Only update chunks if player has moved to a new chunk
        var currentChunk = WorldToChunkCoord(player.position);
        if (_lastPlayerChunk != currentChunk)
        {
            // Trigger event for player moving to a new chunk
            onPlayerMovedToNewChunk?.Invoke();
            _lastPlayerChunk = currentChunk;
        }
    }
    
    Vector2Int WorldToChunkCoord(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.z / chunkSize)
        );
    }
}
