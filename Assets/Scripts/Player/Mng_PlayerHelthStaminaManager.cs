using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

public class Mng_PlayerHelthStaminaManager : MonoBehaviour, IPlayerStatsManager
{
    [Header("Health Settings")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider easeHealthBar;
    [SerializeField] private float maxHealth = 100;
    [SerializeField] private float easeHealthSpeed = 0.5f;
    
    [Header("Stamina Settings")]
    [SerializeField] private Slider staminaBar;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDecreaseRate = 5f; // per second when
    [SerializeField] private float staminaRecoveryRate = 10f; // per second when not
    [SerializeField] private float staminaRecoveryDelay = 1f; // seconds after stopping

    [Header("Player References")]
    [SerializeField] private StarterAssetsInputs _playerInputs;
    
    private float _currentStamina = 0f;
    private float _currentStaminaRecoveryDelayCounter = 0f; 
    private float currentHealth;
    
    #region - GETTERS -

    public bool HasStamina => _currentStamina > 0f;

    #endregion
    
    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
        
        _currentStamina = maxStamina;
    }

    private void FixedUpdate()
    {
        if(healthBar.value != easeHealthBar.value)
        {
            easeHealthBar.value = Mathf.Lerp(easeHealthBar.value, healthBar.value, easeHealthSpeed);
        }
        
        HandleStamina();
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
    
    private void HandleStamina()
    {
        // Decrease stamina when sprinting
        if (_playerInputs.sprint)
        {
            _currentStamina -= staminaDecreaseRate * Time.deltaTime;
            _currentStaminaRecoveryDelayCounter = 0;
        }
        
        // Recover stamina when not sprinting
        if(!_playerInputs.sprint && _currentStamina < maxStamina)
        {
            if (_currentStaminaRecoveryDelayCounter < staminaRecoveryDelay)
            {
                // Increase the delay counter
                _currentStaminaRecoveryDelayCounter += Time.deltaTime;
            }
            else
            {
                // After delay, recover stamina
                _currentStamina += staminaRecoveryRate * Time.deltaTime;
                if (_currentStamina > maxStamina)
                {
                    _currentStamina = maxStamina;
                }
            }
        }
        
        // Clamp stamina to valid range
        _currentStamina = Mathf.Clamp(_currentStamina, 0, maxStamina);
        
        UpdateStaminaBar();
    }
    
    private void UpdateStaminaBar()
    {
        if (staminaBar != null)
        {
            staminaBar.value = _currentStamina / maxStamina;
        }
    }
}
