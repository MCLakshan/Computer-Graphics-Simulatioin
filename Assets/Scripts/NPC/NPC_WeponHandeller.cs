using UnityEngine;

public class NPC_WeponHandeller : MonoBehaviour
{
    // Player Hit Collision Manager Reference
    [Header("Player Hit Collision Manager Reference")]
    [SerializeField] Player_HitCollisionManager player_hitCollisionManager;
    
    // Sword Points
    [Header("Sword Points")]
    [SerializeField] Transform swordPointA;
    [SerializeField] Transform swordPointB;
    
    // Check Attack Collision
    public void CheckAttackCollision()
    {
        if (player_hitCollisionManager != null && swordPointA != null && swordPointB != null)
        {
            bool collisionDetected = player_hitCollisionManager.CheckCollisionWithGivenLine(swordPointA.position, swordPointB.position);
            if (collisionDetected)
            {
                Debug.Log("Attack Hit Detected!");
                // Handle hit logic here (e.g., apply damage, play effects, etc.)
            }
            else
            {
                Debug.Log("No Hit Detected.");
            }
        }
        else
        {
            Debug.LogWarning("Player_HitCollisionManager or Sword Points are not assigned.");
        }
    }
}
