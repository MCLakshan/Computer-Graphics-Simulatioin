using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
using TMPro;
using System.Text;
using System.Collections.Generic;

public class Mng_PerformanceMonitor : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private TMP_Text performanceText;
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private bool showPerformanceStats = true;
    [SerializeField] private bool showMemoryStats = true;
    [SerializeField] private bool showRenderingStats = true;
    [SerializeField] private bool showSystemInfo = false;
    
    [Header("Color Settings")]
    [SerializeField] private Color goodColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private Color normalColor = Color.white;
    
    [Header("Warning Thresholds")]
    [SerializeField] private long memoryWarningMB = 1024; // 1GB
    [SerializeField] private long memoryCriticalMB = 2048; // 2GB
    [SerializeField] private int drawCallWarning = 1000;
    [SerializeField] private int drawCallCritical = 2000;
    
    // Profiler Recorders
    private ProfilerRecorder mainThreadTimeRecorder;
    private ProfilerRecorder renderThreadTimeRecorder;
    private ProfilerRecorder totalAllocatedMemoryRecorder;
    private ProfilerRecorder gcAllocatedInFrameRecorder;
    private ProfilerRecorder systemUsedMemoryRecorder;
    private ProfilerRecorder drawCallsRecorder;
    private ProfilerRecorder verticesRecorder;
    private ProfilerRecorder trianglesRecorder;
    private ProfilerRecorder batchesRecorder;
    private ProfilerRecorder textureMemoryRecorder;
    private ProfilerRecorder meshMemoryRecorder;
    private ProfilerRecorder audioMemoryRecorder;
    
    // Performance tracking
    private float timer = 0.0f;
    private StringBuilder stringBuilder;
    private bool systemInfoCached = false;
    private string cachedSystemInfo = "";
    
    // FPS tracking
    private int frameCount = 0;
    private float fpsTimer = 0.0f;
    private int currentFPS = 0;
    
    void Start()
    {
        if (performanceText == null)
        {
            performanceText = GetComponent<TMP_Text>();
            if (performanceText == null)
            {
                performanceText = GetComponentInChildren<TMP_Text>();
            }
        }
        
        if (performanceText == null)
        {
            Debug.LogWarning("Mng_PerformanceMonitor: No TMP_Text component found!");
            enabled = false;
            return;
        }
        
        stringBuilder = new StringBuilder(1024);
        
        // Initialize Profiler Recorders
        InitializeRecorders();
        
        // Cache system info once
        if (showSystemInfo)
        {
            CacheSystemInfo();
        }
    }
    
    void InitializeRecorders()
    {
        mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
        renderThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Render Thread", 15);
        totalAllocatedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Allocated Memory", 15);
        gcAllocatedInFrameRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 15);
        systemUsedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory", 15);
        drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 15);
        verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count", 15);
        trianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 15);
        batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 15);
        textureMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Texture Memory", 15);
        meshMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Mesh Memory", 15);
        audioMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Audio, "Audio Memory", 15);
    }
    
    void Update()
    {
        if (!showPerformanceStats && !showMemoryStats && !showRenderingStats) return;
        
        // Update FPS
        frameCount++;
        fpsTimer += Time.unscaledDeltaTime;
        
        // Update display
        timer += Time.unscaledDeltaTime;
        if (timer >= updateInterval)
        {
            currentFPS = Mathf.RoundToInt(frameCount / fpsTimer);
            UpdateDisplay();
            timer = 0.0f;
            frameCount = 0;
            fpsTimer = 0.0f;
        }
    }
    
    void UpdateDisplay()
    {
        stringBuilder.Clear();
        
        if (showPerformanceStats)
        {
            AddPerformanceStats();
        }
        
        if (showMemoryStats)
        {
            AddMemoryStats();
        }
        
        if (showRenderingStats)
        {
            AddRenderingStats();
        }
        
        if (showSystemInfo && systemInfoCached)
        {
            stringBuilder.AppendLine();
            stringBuilder.Append(cachedSystemInfo);
        }
        
        performanceText.text = stringBuilder.ToString();
    }
    
    void AddPerformanceStats()
    {
        stringBuilder.AppendLine("<color=#FFD700><b>PERFORMANCE</b></color>");
        
        // FPS with color coding
        Color fpsColor = GetFPSColor(currentFPS);
        stringBuilder.AppendLine($"FPS: <color=#{ColorUtility.ToHtmlStringRGB(fpsColor)}>{currentFPS}</color>");
        
        // Frame times
        if (mainThreadTimeRecorder.Valid)
        {
            double mainThreadTime = GetRecorderFrameAverage(mainThreadTimeRecorder) * 1e-6; // Convert to ms
            Color frameTimeColor = mainThreadTime > 16.67 ? criticalColor : (mainThreadTime > 8.33 ? warningColor : goodColor);
            stringBuilder.AppendLine($"Main Thread: <color=#{ColorUtility.ToHtmlStringRGB(frameTimeColor)}>{mainThreadTime:F2}ms</color>");
        }
        
        if (renderThreadTimeRecorder.Valid)
        {
            double renderThreadTime = GetRecorderFrameAverage(renderThreadTimeRecorder) * 1e-6; // Convert to ms
            stringBuilder.AppendLine($"Render Thread: {renderThreadTime:F2}ms");
        }
        
        stringBuilder.AppendLine();
    }
    
    void AddMemoryStats()
    {
        stringBuilder.AppendLine("<color=#00FFFF><b>MEMORY</b></color>");
        
        // Total allocated memory
        if (totalAllocatedMemoryRecorder.Valid)
        {
            long totalMemory = totalAllocatedMemoryRecorder.LastValue;
            long totalMemoryMB = totalMemory / (1024 * 1024);
            Color memoryColor = GetMemoryColor(totalMemoryMB);
            stringBuilder.AppendLine($"Total Allocated: <color=#{ColorUtility.ToHtmlStringRGB(memoryColor)}>{totalMemoryMB}MB</color>");
        }
        
        // System used memory
        if (systemUsedMemoryRecorder.Valid)
        {
            long systemMemory = systemUsedMemoryRecorder.LastValue;
            long systemMemoryMB = systemMemory / (1024 * 1024);
            stringBuilder.AppendLine($"System Used: {systemMemoryMB}MB");
        }
        
        // GC allocations per frame
        if (gcAllocatedInFrameRecorder.Valid)
        {
            long gcAllocated = gcAllocatedInFrameRecorder.LastValue;
            Color gcColor = gcAllocated > 1024 ? criticalColor : (gcAllocated > 512 ? warningColor : goodColor);
            stringBuilder.AppendLine($"GC/Frame: <color=#{ColorUtility.ToHtmlStringRGB(gcColor)}>{gcAllocated}B</color>");
        }
        
        // Texture memory
        if (textureMemoryRecorder.Valid)
        {
            long textureMemory = textureMemoryRecorder.LastValue / (1024 * 1024);
            stringBuilder.AppendLine($"Texture Memory: {textureMemory}MB");
        }
        
        // Mesh memory
        if (meshMemoryRecorder.Valid)
        {
            long meshMemory = meshMemoryRecorder.LastValue / (1024 * 1024);
            stringBuilder.AppendLine($"Mesh Memory: {meshMemory}MB");
        }
        
        // Audio memory
        if (audioMemoryRecorder.Valid)
        {
            long audioMemory = audioMemoryRecorder.LastValue / (1024 * 1024);
            stringBuilder.AppendLine($"Audio Memory: {audioMemory}MB");
        }
        
        stringBuilder.AppendLine();
    }
    
    void AddRenderingStats()
    {
        stringBuilder.AppendLine("<color=#FF69B4><b>RENDERING</b></color>");
        
        // Draw calls
        if (drawCallsRecorder.Valid)
        {
            int drawCalls = (int)drawCallsRecorder.LastValue;
            Color drawCallColor = GetDrawCallColor(drawCalls);
            stringBuilder.AppendLine($"Draw Calls: <color=#{ColorUtility.ToHtmlStringRGB(drawCallColor)}>{drawCalls}</color>");
        }
        
        // Batches
        if (batchesRecorder.Valid)
        {
            int batches = (int)batchesRecorder.LastValue;
            stringBuilder.AppendLine($"Batches: {batches}");
        }
        
        // Vertices
        if (verticesRecorder.Valid)
        {
            int vertices = (int)verticesRecorder.LastValue;
            string vertexString = vertices > 1000000 ? $"{vertices / 1000000f:F1}M" : $"{vertices / 1000f:F0}K";
            stringBuilder.AppendLine($"Vertices: {vertexString}");
        }
        
        // Triangles
        if (trianglesRecorder.Valid)
        {
            int triangles = (int)trianglesRecorder.LastValue;
            string triangleString = triangles > 1000000 ? $"{triangles / 1000000f:F1}M" : $"{triangles / 1000f:F0}K";
            stringBuilder.AppendLine($"Triangles: {triangleString}");
        }
        
        stringBuilder.AppendLine();
    }
    
    void CacheSystemInfo()
    {
        StringBuilder sysInfo = new StringBuilder();
        sysInfo.AppendLine("<color=#90EE90><b>SYSTEM INFO</b></color>");
        sysInfo.AppendLine($"GPU: {SystemInfo.graphicsDeviceName}");
        sysInfo.AppendLine($"VRAM: {SystemInfo.graphicsMemorySize}MB");
        sysInfo.AppendLine($"CPU: {SystemInfo.processorType}");
        sysInfo.AppendLine($"Cores: {SystemInfo.processorCount}");
        sysInfo.AppendLine($"RAM: {SystemInfo.systemMemorySize}MB");
        sysInfo.AppendLine($"Platform: {Application.platform}");
        
        cachedSystemInfo = sysInfo.ToString();
        systemInfoCached = true;
    }
    
    // Helper methods - FIXED VERSION
    static double GetRecorderFrameAverage(ProfilerRecorder recorder)
    {
        var samplesCount = recorder.Capacity;
        if (samplesCount == 0)
            return 0;
        
        double r = 0;
        // Create a List instead of array for the new CopyTo method
        var samples = new List<ProfilerRecorderSample>(samplesCount);
        
        // Use the correct CopyTo signature: CopyTo(List<ProfilerRecorderSample>, bool)
        // The second parameter is 'includeInactive' - set to false for active samples only
        recorder.CopyTo(samples, false);
        
        // Calculate average from the copied samples
        for (var i = 0; i < samples.Count; ++i)
            r += samples[i].Value;
        
        if (samples.Count > 0)
            r /= samples.Count;
        
        return r;
    }
    
    Color GetFPSColor(int fps)
    {
        if (fps >= 60) return goodColor;
        if (fps >= 30) return warningColor;
        return criticalColor;
    }
    
    Color GetMemoryColor(long memoryMB)
    {
        if (memoryMB >= memoryCriticalMB) return criticalColor;
        if (memoryMB >= memoryWarningMB) return warningColor;
        return goodColor;
    }
    
    Color GetDrawCallColor(int drawCalls)
    {
        if (drawCalls >= drawCallCritical) return criticalColor;
        if (drawCalls >= drawCallWarning) return warningColor;
        return goodColor;
    }
    
    // Public methods
    public void TogglePerformanceStats() => showPerformanceStats = !showPerformanceStats;
    public void ToggleMemoryStats() => showMemoryStats = !showMemoryStats;
    public void ToggleRenderingStats() => showRenderingStats = !showRenderingStats;
    public void ToggleSystemInfo() => showSystemInfo = !showSystemInfo;
    
    public void SetUpdateInterval(float interval)
    {
        updateInterval = Mathf.Clamp(interval, 0.1f, 5.0f);
    }
    
    void OnDisable()
    {
        // Dispose of recorders
        mainThreadTimeRecorder.Dispose();
        renderThreadTimeRecorder.Dispose();
        totalAllocatedMemoryRecorder.Dispose();
        gcAllocatedInFrameRecorder.Dispose();
        systemUsedMemoryRecorder.Dispose();
        drawCallsRecorder.Dispose();
        verticesRecorder.Dispose();
        trianglesRecorder.Dispose();
        batchesRecorder.Dispose();
        textureMemoryRecorder.Dispose();
        meshMemoryRecorder.Dispose();
        audioMemoryRecorder.Dispose();
        
        if (performanceText != null)
        {
            performanceText.gameObject.SetActive(false);
        }
    }
}