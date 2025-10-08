using JetBrains.Annotations;
using UnityEngine;

public enum CollisionType
{
    Collition_With_Line,
}

public class Player_HitCollisionManager : MonoBehaviour
{
    // Implement collision detection and handling logic here
    
    // Layer 1 Collision Points
    [Header("Layer 1 Collision Points")]
    [SerializeField] [NotNull] private Transform[] Layer_1_CollitionPoints;
    [SerializeField] private float Layer_1_CollitionRadius = 1;
    
    // Layer 2 Collision Points
    [Header("Layer 2 Collision Points")]
    [SerializeField] [CanBeNull] private Transform[] Layer_2_CollitionPoints;
    [SerializeField] private float Layer_2_CollitionRadius = 1;
    
    // Layer 3 Collision Points
    [Header("Layer 3 Collision Points")]
    [SerializeField] [CanBeNull] private Transform[] Layer_3_CollitionPoints;
    [SerializeField] private float Layer_3_CollitionRadius = 1;
    
    [Header("Visualization Settings")]
    [SerializeField] private bool showLayer1 = true;
    [SerializeField] private bool showLayer2 = true;
    [SerializeField] private bool showLayer3 = true;

    public bool CheckCollisionWithGivenLine(Vector3 linePointA, Vector3 linePointB)
    {
        // Check collision with Layer 1
        foreach (var point1 in Layer_1_CollitionPoints)
        {
            if (point1 != null && MathUtils.CheckSpereCollisionWithLine(linePointA, linePointB, point1.position, Layer_1_CollitionRadius))
            {
                
                // Collision Detected with Layer 1
                // Now check Layer 2 if it exists
                
                foreach(var point2 in Layer_2_CollitionPoints)
                {
                    if (point2 != null && MathUtils.CheckSpereCollisionWithLine(linePointA, linePointB, point2.position, Layer_2_CollitionRadius))
                    {
                        // Collision Detected with Layer 2
                        // Now check Layer 3 if it exists
                        
                        foreach(var point3 in Layer_3_CollitionPoints)
                        {
                            if (point3 != null && MathUtils.CheckSpereCollisionWithLine(linePointA, linePointB, point3.position, Layer_3_CollitionRadius))
                            {
                                // Collision Detected with Layer 3
                                return true; // Full collision through all layers
                            }
                        }
                        return true; // Collision Detected with Layer 2 only
                    }
                }
            }
        }
        return false;
    }
    
    // Draw Gizmos to visualize collision points in the editor
    private void OnDrawGizmos()
    {
        // --- Layer 1 ---
        if (showLayer1 && Layer_1_CollitionPoints != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
            foreach (var point in Layer_1_CollitionPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, Layer_1_CollitionRadius * 0.1f);
                    Gizmos.DrawWireSphere(point.position, Layer_1_CollitionRadius);
                }
            }
        }

        // --- Layer 2 ---
        if (showLayer2 && Layer_2_CollitionPoints != null)
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.5f);
            foreach (var point in Layer_2_CollitionPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, Layer_2_CollitionRadius * 0.1f);
                    Gizmos.DrawWireSphere(point.position, Layer_2_CollitionRadius);
                }
            }
        }

        // --- Layer 3 ---
        if (showLayer3 && Layer_3_CollitionPoints != null)
        {
            Gizmos.color = new Color(0.7f, 0.2f, 1f, 0.5f);
            foreach (var point in Layer_3_CollitionPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, Layer_3_CollitionRadius * 0.1f);
                    Gizmos.DrawWireSphere(point.position, Layer_3_CollitionRadius);
                }
            }
        }

        // --- Connection Lines ---
        Gizmos.color = new Color(1f, 1f, 1f, 0.1f);
        if (showLayer1) DrawConnectionLines(Layer_1_CollitionPoints);
        if (showLayer2) DrawConnectionLines(Layer_2_CollitionPoints);
        if (showLayer3) DrawConnectionLines(Layer_3_CollitionPoints);
    }
    
    private void DrawConnectionLines(Transform[] points)
    {
        if (points == null || points.Length < 2) return;
        for (int i = 0; i < points.Length - 1; i++)
        {
            if (points[i] != null && points[i + 1] != null)
            {
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
            }
        }
    }
}

