using System;
using UnityEngine;

public class Mng_DayAndNightCycle : MonoBehaviour
{
    [Header("Current Time")]
    [SerializeField] private string currentTimeString = "";
    
    [Header("Time Settings")]
    [Range(0f, 24f)]
    [SerializeField] private float currentTime = 0f;
    [SerializeField] private float timeSpeed = 1f;
    
    [Header("Light Settings")]
    [SerializeField] private Light sunLight;
    [SerializeField] private float sunPosition = 1f;
    [SerializeField] private float sunIntensity = 1f;
    
    
    private void Update()
    {
        currentTime += Time.deltaTime * timeSpeed;
        
        if (currentTime >= 24f)
        {
            currentTime -= 24f; // Reset to 0 after 24 hours
        }
        
        UpdateTimeText();
    }
    
    private void OnValidate()
    {
        UpdateLight();
    }
    
    private void UpdateTimeText()
    {
        currentTimeString = Mathf.Floor(currentTime).ToString("00") + ":" + ((currentTime % 1) * 60).ToString("00");
    }

    private void UpdateLight()
    {
        float sunRotation = (currentTime / 24f) * 360f;
        sunLight.transform.rotation = Quaternion.Euler(sunRotation - 90f, sunPosition, 0f);
    }
}
