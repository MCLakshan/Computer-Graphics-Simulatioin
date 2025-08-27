using StarterAssets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Mng_SurvivalStatsManager : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float _currentHealth = 0f;
    private float _healthPercent => _currentHealth / maxHealth;
    
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDecreaseRate = 5f; // per second when
    [SerializeField] private float staminaRecoveryRate = 10f; // per second when not
    [SerializeField] private float staminaRecoveryDelay = 1f; // seconds after stopping
    private float _currentStamina = 0f;
    private float _currentStaminaRecoveryDelayCounter = 0f; 
    private float _staminaPercent => _currentStamina / maxStamina;
    
    [Header("Hunger Settings")]
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float hungerDecreaseRate = 1f; // per minute
    [SerializeField] private float loseHealthWhenStarvingRate = 5f; // per second when
    private float _currentHunger = 0f;
    private float _hungerPercent => _currentHunger / maxHunger;
    
    [Header("Thirst Settings")]
    [SerializeField] private float maxThirst = 100f;
    [SerializeField] private float thirstDecreaseRate = 1.5f; // per minute
    [SerializeField] private float loseHealthWhenDehydratedRate = 5f; // per second when
    private float _currentThirst = 0f;
    private float _thirstPercent => _currentThirst / maxThirst;
    
    [Header("UI Settings")]
    [SerializeField] private Image healthBar;
    [SerializeField] private Image staminaBar;
    [SerializeField] private Image hungerBar;
    [SerializeField] private Image thirstBar;
    [SerializeField, Range(0f, 1f)] private float lowStatThreshold = 0.3f;
    [SerializeField, Range(0f, 1f)] private float midStatThreshold = 0.6f;
    [SerializeField] private Color lowStatColor  = new Color(0.75f, 0.25f, 0.25f); // smoky brick red
    [SerializeField] private Color midStatColor  = new Color(0.85f, 0.75f, 0.35f); // dusty golden yellow
    [SerializeField] private Color highStatColor = new Color(0.35f, 0.7f, 0.35f);  // mossy green
    
    [Header("Player References")]
    [SerializeField] private StarterAssetsInputs _playerInputs;
    
    [Header("Events")]
    public UnityEvent OnPlayerDeath;
    
    private void Start()
    {
        _currentHealth = maxHealth;
        _currentStamina = maxStamina;
        _currentHunger = maxHunger;
        _currentThirst = maxThirst;
    }

    #region - GETTERS -

    public bool HasStamina => _currentStamina > 0f;

    #endregion
    
    private void FixedUpdate()
    {
        HandleHealth();
        HandleStamina();
        HandleHungerDecrese();
        HandleThirstDecrese();
        UpdateUIBars();
    }

    private void HandleHealth()
    {
        if (_currentHealth <= 0f)
        {
            _currentHealth = 0f;
            OnPlayerDeath?.Invoke();
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
    }
    
    private void HandleHungerDecrese()
    {
        _currentHunger -= hungerDecreaseRate * Time.deltaTime;
        if (_currentHunger < 0)
        {
            _currentHunger = 0;
            _currentHealth -= loseHealthWhenStarvingRate * Time.deltaTime; // Lose health when starving
        }
    }
    
    private void HandleThirstDecrese()
    {
        _currentThirst -= thirstDecreaseRate * Time.deltaTime;
        if (_currentThirst < 0)
        {
            _currentThirst = 0;
            _currentHealth -= loseHealthWhenDehydratedRate * Time.deltaTime; // Lose health when dehydrated
        }
    }
    
    public void IncreaseHungerAndThirst(float hungerAmount, float thirstAmount)
    {
        _currentHunger += hungerAmount;
        if (_currentHunger > maxHunger)
            _currentHunger = maxHunger;
        
        _currentThirst += thirstAmount;
        if (_currentThirst > maxThirst)
            _currentThirst = maxThirst;
    }

    private void UpdateUIBars()
    {
        // Health Bar
        if (healthBar != null)
        {
            healthBar.fillAmount = _healthPercent;
            if (_healthPercent <= lowStatThreshold)
                healthBar.color = lowStatColor;
            else if (_healthPercent <= midStatThreshold)
                healthBar.color = midStatColor;
            else
                healthBar.color = highStatColor;
        }
        
        // Stamina Bar
        if (staminaBar != null)
        {
            staminaBar.fillAmount = _staminaPercent;
            if (_staminaPercent <= lowStatThreshold)
                staminaBar.color = lowStatColor;
            else if (_staminaPercent <= midStatThreshold)
                staminaBar.color = midStatColor;
            else
                staminaBar.color = highStatColor;
        }
        
        // Hunger Bar
        if (hungerBar != null)
        {
            hungerBar.fillAmount = _hungerPercent;
            if (_hungerPercent <= lowStatThreshold)
                hungerBar.color = lowStatColor;
            else if (_hungerPercent <= midStatThreshold)
                hungerBar.color = midStatColor;
            else
                hungerBar.color = highStatColor;
        }
        
        // Thirst Bar
        if (thirstBar != null)
        {
            thirstBar.fillAmount = _thirstPercent;
            if (_thirstPercent <= lowStatThreshold)
                thirstBar.color = lowStatColor;
            else if (_thirstPercent <= midStatThreshold)
                thirstBar.color = midStatColor;
            else
                thirstBar.color = highStatColor;
        }
    }

}
