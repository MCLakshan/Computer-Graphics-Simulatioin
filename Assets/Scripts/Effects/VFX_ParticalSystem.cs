using UnityEngine;
using System.Collections.Generic;

public class VFX_ParticalSystem : MonoBehaviour
{
    [Header("Particle Settings")]
    public int maxParticles = 100;
    public float emissionRate = 10f;
    public float particleLifetime = 3f;
    public Vector3 startVelocity = Vector3.up;
    public Vector3 gravity = new Vector3(0, -9.8f, 0);
    public float startSize = 0.1f;
    public float endSize = 0.05f;
    public Color startColor = Color.white;
    public Color endColor = Color.clear;
    [Range(0f, 2f)]
    public float velocityMultiplier = 0.5f;
    
    [Header("Spawn Area")]
    public Vector3 spawnAreaSize = Vector3.one;
    
    [Header("Material")]
    public Material particleMaterial;
    
    private List<Particle> particles = new List<Particle>();
    private float timeSinceLastEmission = 0f;
    private Mesh particleMesh;
    
    // Particle class to hold individual particle data
    [System.Serializable]
    public class Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float age;
        public float lifetime;
        public bool isActive;
        
        public Particle()
        {
            isActive = false;
        }
    }
    
    void Start()
    {
        // Create a simple quad mesh for particles
        CreateParticleMesh();
        
        // Initialize particle pool
        for (int i = 0; i < maxParticles; i++)
        {
            particles.Add(new Particle());
        }
        
        // Create default material if none assigned
        if (particleMaterial == null)
        {
            particleMaterial = CreateDefaultMaterial();
        }
    }
    
    void Update()
    {
        // Emit new particles
        EmitParticles();
        
        // Update existing particles
        UpdateParticles();
        
        // Render particles
        RenderParticles();
    }
    
    void CreateParticleMesh()
    {
        particleMesh = new Mesh();
        
        // Create a simple quad
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0)
        };
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
        
        particleMesh.vertices = vertices;
        particleMesh.uv = uv;
        particleMesh.triangles = triangles;
        particleMesh.RecalculateNormals();
    }
    
    Material CreateDefaultMaterial()
    {
        // Create a simple unlit material
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.white;
        return mat;
    }
    
    void EmitParticles()
    {
        timeSinceLastEmission += Time.deltaTime;
        
        if (timeSinceLastEmission >= 1f / emissionRate)
        {
            SpawnParticle();
            timeSinceLastEmission = 0f;
        }
    }
    
    void SpawnParticle()
    {
        // Find an inactive particle
        Particle particle = GetInactiveParticle();
        if (particle == null) return;
        
        // Initialize particle properties
        particle.isActive = true;
        particle.age = 0f;
        particle.lifetime = particleLifetime + Random.Range(-0.5f, 0.5f);
        
        // Random position within spawn area
        particle.position = transform.position + new Vector3(
            Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
            Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
            Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
        );
        
        // Random velocity based on start velocity
        particle.velocity = startVelocity + new Vector3(
            Random.Range(-1f, 1f) * velocityMultiplier,
            Random.Range(-1f, 1f) * velocityMultiplier,
            Random.Range(-1f, 1f) * velocityMultiplier
        );
    }
    
    Particle GetInactiveParticle()
    {
        foreach (Particle particle in particles)
        {
            if (!particle.isActive)
                return particle;
        }
        return null;
    }
    
    void UpdateParticles()
    {
        foreach (Particle particle in particles)
        {
            if (!particle.isActive) continue;
            
            // Update age
            particle.age += Time.deltaTime;
            
            // Check if particle should die
            if (particle.age >= particle.lifetime)
            {
                particle.isActive = false;
                continue;
            }
            
            // Apply physics
            particle.velocity += gravity * Time.deltaTime;
            particle.position += particle.velocity * Time.deltaTime;
        }
    }
    
    void RenderParticles()
    {
        foreach (Particle particle in particles)
        {
            if (!particle.isActive) continue;
            
            // Calculate normalized age (0 to 1)
            float normalizedAge = particle.age / particle.lifetime;
            
            // Interpolate size and color based on age
            float currentSize = Mathf.Lerp(startSize, endSize, normalizedAge);
            Color currentColor = Color.Lerp(startColor, endColor, normalizedAge);
            
            // More robust camera detection
            Camera currentCamera = Camera.main;
            if (currentCamera == null)
            {
                currentCamera = Camera.current; // Scene view camera
            }
            if (currentCamera == null)
            {
                currentCamera = FindObjectOfType<Camera>(); // Any camera
            }

            Quaternion billboardRotation = currentCamera != null ? 
                currentCamera.transform.rotation : 
                Quaternion.identity;

            Matrix4x4 matrix = Matrix4x4.TRS(
                particle.position,
                billboardRotation,
                Vector3.one * currentSize
            );
            
            // Set material color
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            props.SetColor("_Color", currentColor);
            
            // Draw the particle
            Graphics.DrawMesh(particleMesh, matrix, particleMaterial, 0, null, 0, props);
        }
    }
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, spawnAreaSize);
    }
    
    // Public methods for external control
    public void StartEmission()
    {
        enabled = true;
    }
    
    public void StopEmission()
    {
        enabled = false;
    }
    
    public void ClearAllParticles()
    {
        foreach (Particle particle in particles)
        {
            particle.isActive = false;
        }
    }
    
    public int GetActiveParticleCount()
    {
        int count = 0;
        foreach (Particle particle in particles)
        {
            if (particle.isActive) count++;
        }
        return count;
    }
}