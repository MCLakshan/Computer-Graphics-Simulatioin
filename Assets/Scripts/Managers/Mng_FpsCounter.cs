using UnityEngine;
using TMPro;

public class Mng_FpsCounter : MonoBehaviour
{
    [Header("FPS Display Settings")]
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private bool showFPS = true;
    
    [Header("Color Settings")]
    [SerializeField] private Color goodFpsColor = Color.green;
    [SerializeField] private Color averageFpsColor = Color.yellow;
    [SerializeField] private Color badFpsColor = Color.red;
    
    [Header("FPS Thresholds")]
    [SerializeField] private int goodFpsThreshold = 60;
    [SerializeField] private int averageFpsThreshold = 30;
    
    private float timer = 0.0f;
    private int frameCount = 0;
    private int currentFps = 0;
    private int lastDisplayedFps = -1;
    private Color lastColor;
    
    // Pre-allocated strings to avoid garbage collection
    private static readonly string[] fpsStrings = new string[200];
    
    static Mng_FpsCounter()
    {
        // Pre-generate FPS strings to avoid runtime string allocation
        for (int i = 0; i < fpsStrings.Length; i++)
        {
            fpsStrings[i] = $"FPS: {i}";
        }
    }
    
    void Start()
    {
        if (fpsText == null)
        {
            fpsText = GetComponent<TMP_Text>();
            if (fpsText == null)
            {
                fpsText = GetComponentInChildren<TMP_Text>();
            }
        }
        
        if (fpsText == null)
        {
            Debug.LogWarning("Mng_FpsCounter: No TMP_Text component found!");
            enabled = false; // Disable the component to save performance
            return;
        }
        
        lastColor = fpsText.color;
    }
    
    void Update()
    {
        if (!showFPS) return;
        
        frameCount++;
        timer += Time.unscaledDeltaTime;
        
        // Only update display at specified intervals
        if (timer >= updateInterval)
        {
            currentFps = Mathf.RoundToInt(frameCount / timer);
            
            // Only update UI if FPS actually changed
            if (currentFps != lastDisplayedFps)
            {
                UpdateFpsDisplay();
                lastDisplayedFps = currentFps;
            }
            
            // Reset counters
            timer = 0.0f;
            frameCount = 0;
        }
    }
    
    private void UpdateFpsDisplay()
    {
        // Use pre-allocated strings to avoid garbage collection
        int fpsIndex = Mathf.Clamp(currentFps, 0, fpsStrings.Length - 1);
        fpsText.text = fpsStrings[fpsIndex];
        
        // Only change color if necessary
        Color newColor = GetFpsColor();
        if (newColor != lastColor)
        {
            fpsText.color = newColor;
            lastColor = newColor;
        }
    }
    
    private Color GetFpsColor()
    {
        if (currentFps >= goodFpsThreshold)
            return goodFpsColor;
        else if (currentFps >= averageFpsThreshold)
            return averageFpsColor;
        else
            return badFpsColor;
    }
    
    // Public methods
    public void ToggleFpsDisplay()
    {
        showFPS = !showFPS;
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(showFPS);
        }
    }
    
    public int GetCurrentFps() => currentFps;
    
    public void SetUpdateInterval(float interval)
    {
        updateInterval = Mathf.Clamp(interval, 0.1f, 5.0f);
    }
    
    void OnDisable()
    {
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(false);
        }
    }
}