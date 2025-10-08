using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class NPC_HealthManager : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider easeHealthBar;
    [SerializeField] private float maxHealth = 100;
    [SerializeField] private float easeHealthSpeed = 0.5f;
    
    private float currentHealth;
    
    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    private void Update()
    {
        if(healthBar.value != easeHealthBar.value)
        {
            easeHealthBar.value = Mathf.Lerp(easeHealthBar.value, healthBar.value, easeHealthSpeed);
        }
        
        // For Testing Purposes Only
        // if (Input.GetKeyDown(KeyCode.H))
        // {
        //     TakeDamage(10);
        // }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();
    }
    
    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
    }
}
