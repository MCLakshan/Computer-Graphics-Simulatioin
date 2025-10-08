using UnityEngine;

public class MathUtils
{
    /*
    Line Segment–Sphere Intersection Derivation

    Given:
        Line segment:
            A = (x1, y1, z1)
            B = (x2, y2, z2)

        Sphere:
            C = (x3, y3, z3)
            radius = r

    Goal:
        Determine whether the line segment AB intersects the sphere centered at C.

    ---------------------------------------------------
    Step 1: Line segment in parametric form
        P(t) = A + t(B - A)
        where t ∈ [0, 1]
        - t = 0 → point A
        - t = 1 → point B

    Step 2: Sphere equation
        Any point P lies on the sphere if:
            ||P - C||² = r²

    Step 3: Substitute P(t) into the sphere equation
        ||(A + t(B - A)) - C||² = r²
        → ||(A - C) + t(B - A)||² = r²

        Let:
            d = B - A
            f = A - C

        Then:
            ||f + t*d||² = r²

    Step 4: Expand the dot product
        (f + t*d) • (f + t*d) = r²
        (d•d)t² + 2(f•d)t + (f•f - r²) = 0

        Let:
            a = d•d
            b = 2(f•d)
            c = f•f - r²

        Then:
            a*t² + b*t + c = 0

    Step 5: Solve for t
        Discriminant:
            Δ = b² - 4ac

        - If Δ < 0 → no intersection
        - If Δ = 0 → tangent (touches sphere)
        - If Δ > 0 → two intersection points

        t₁ = (-b - √Δ) / (2a)
        t₂ = (-b + √Δ) / (2a)

    Step 6: Check if intersection occurs on the segment
        If either t₁ or t₂ ∈ [0, 1],
        → the segment AB intersects the sphere.
    */
    
    public static bool CheckSpereCollisionWithLine(Vector3 linePointA, Vector3 linePointB, Vector3 sphereCenter, float sphereRadius)
    {
        Vector3 d = linePointB - linePointA; 
        Vector3 f = linePointA - sphereCenter; 
        
        float a = Vector3.Dot(d, d);
        float b = 2 * Vector3.Dot(f, d);
        float c = Vector3.Dot(f, f) - sphereRadius * sphereRadius;
        
        float discriminant = b * b - 4 * a * c; // Δ

        if (discriminant < 0)
        {
            // No intersection
            return false;
        }
        else
        {
            discriminant = Mathf.Sqrt(discriminant);
            float t1 = (-b - discriminant) / (2 * a);
            float t2 = (-b + discriminant) / (2 * a);
            
            // Check if either t1 or t2 is within the segment
            if ((t1 >= 0 && t1 <= 1) || (t2 >= 0 && t2 <= 1))
            {
                return true; // Intersection occurs within the segment
            }
            
            return false; // Intersection does not occur within the segment
        }
    }
    
}