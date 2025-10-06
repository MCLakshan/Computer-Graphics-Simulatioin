using UnityEngine;

public class Main : MonoBehaviour
{
    [SerializeField, Range(30, 240)] private int frameCap = 60;
    
    void Start()
    {
        Application.targetFrameRate = frameCap;
    }
}
