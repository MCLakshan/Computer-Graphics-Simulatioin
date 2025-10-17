using System;
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
    
    // Attack Damage
    [Header("Attack Damage Settings")]
    [SerializeField] float attackDamageoOverTime = 1f;
    
    private Mng_PlayerHelthStaminaManager player_helthStaminaManager;
    private bool ckeckAttackCollision = false;
    
    private void Start()
    {
        // Find the Player Health Stamina Manager in the scene
        player_helthStaminaManager = FindObjectOfType<Mng_PlayerHelthStaminaManager>();
        if (player_helthStaminaManager == null)
        {
            Debug.LogWarning("Mng_PlayerHelthStaminaManager not found in the scene.");
        }
    }

    private void FixedUpdate()
    {
        CheckAttackCollision();
    }

    // Check Attack Collision
    public void CheckAttackCollision()
    {
        if (player_hitCollisionManager != null && swordPointA != null && swordPointB != null && ckeckAttackCollision)
        {
            bool collisionDetected = player_hitCollisionManager.CheckCollisionWithGivenLine(swordPointA.position, swordPointB.position);
            if (collisionDetected)
            {
                // Apply damage to the player
                if (player_helthStaminaManager != null)
                {
                    player_helthStaminaManager.TakeDamage(attackDamageoOverTime);
                }
            }
            
        }
    }
    
    // Animation Event to Start Checking Attack Collision
    public void StartCheckAttackCollisionCheck()
    {
        ckeckAttackCollision = true;
    }
    
    // Animation Event to Stop Checking Attack Collision
    public void StopCheckAttackCollisionCheck()
    {
        ckeckAttackCollision = false;
    }
}
