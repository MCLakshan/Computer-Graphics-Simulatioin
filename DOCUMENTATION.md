﻿# Technical Documentation - Computer Graphics Simulation

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Procedural Terrain Generation](#procedural-terrain-generation)
3. [GPU Instancing System](#gpu-instancing-system)
4. [Procedural Texture Generation](#procedural-texture-generation)
5. [Particle System Implementation](#particle-system-implementation)
6. [Volumetric Cloud System](#volumetric-cloud-system)
7. [NPC AI System](#npc-ai-system)
8. [Collision Detection System](#collision-detection-system)
9. [Water Detection & Rendering](#water-detection--rendering)
10. [Environmental Systems](#environmental-systems)
11. [Performance Optimization](#performance-optimization)
12. [Shader Implementations](#shader-implementations)
13. [API Reference](#api-reference)

---

## Architecture Overview

### System Design Philosophy

This project follows a modular, component-based architecture leveraging Unity's GameObject-Component pattern. Key design principles include:

- **Separation of Concerns**: Each system handles a specific responsibility
- **Event-Driven Communication**: Decoupled systems using UnityEvents and C# delegates
- **Manager Pattern**: Centralized managers for global state and references
- **Data-Oriented Design**: Efficient data structures for performance-critical code

### Core Systems Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    Main Manager                          │
│              (Mng_GlobalReferences)                      │
└────────────────────┬────────────────────────────────────┘
                     │
        ┌────────────┴──────────────┐
        │                           │
        ▼                           ▼
┌───────────────┐          ┌───────────────────┐
│ Terrain       │          │ GPU Instancing    │
│ Generation    │◄────────►│ System            │
└───────┬───────┘          └─────────┬─────────┘
        │                            │
        │  ┌─────────────────────────┤
        │  │                         │
        ▼  ▼                         ▼
┌───────────────┐          ┌──────────────────┐
│ Water         │          │ Object Spawners  │
│ Detection     │          │ (Grass, Trees)   │
└───────────────┘          └──────────────────┘

┌─────────────────────────────────────────────────────────┐
│                    NPC AI System                         │
│        (State Machine + NavMesh + Perception)            │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│              Environmental Effects                       │
│     (Day/Night, Particles, Clouds, Fog)                 │
└─────────────────────────────────────────────────────────┘
```

---

## Procedural Terrain Generation

### Algorithm Overview

The terrain generation system uses **multi-octave Perlin noise** combined with **island masking** to create realistic, varied landscapes.

### Core Components

#### 1. PerlinNoiseTerrainGenerator.cs

**Purpose**: Generates terrain heightmaps using layered Perlin noise with configurable parameters.

**Key Features**:
- Multi-octave noise generation for detail at multiple scales
- Island generation with two modes: Real Center and Multi-Center
- Height redistribution for controlling terrain shape
- Real-time regeneration with randomized seed offsets

### Detailed Algorithm Breakdown

#### Step 1: Base Noise Generation (First Pass)

```csharp
// For each point in the terrain
for (int x = 0; x < xRange; x++) {
    for (int z = 0; z < zRange; z++) {
        float noiseHeight = 0f;
        
        // Calculate base coordinates
        float xCoordBase = (float)x / xRange * noiseScale + offsetX;
        float zCoordBase = (float)z / zRange * noiseScale + offsetZ;
        
        // Apply octaves
        for (int i = 0; i < octaves; i++) {
            float octaveScale = Mathf.Pow(2, i);
            float sampleX = xCoordBase * octaveScale;
            float sampleZ = zCoordBase * octaveScale;
            
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);
            float heightValue = (1.0f / octaveScale) * perlinValue;
            noiseHeight += heightValue;
        }
        
        heights[x, z] = noiseHeight;
    }
}
```

**Octave Explanation**:
- **Octave 0** (scale 1): Base terrain features, large smooth hills
- **Octave 1** (scale 2): Medium detail, rolling hills
- **Octave 2** (scale 4): Fine detail, small variations
- **Octave 3+** (scale 8+): Micro-detail, surface roughness

Each octave contributes detail at different frequencies, creating natural-looking terrain.

#### Step 2: Normalization & Island Masking (Second Pass)

```csharp
for (int x = 0; x < xRange; x++) {
    for (int z = 0; z < zRange; z++) {
        // Normalize using actual min/max values
        float normalizedHeight = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, heights[x, z]);
        
        // Apply redistribution (power function for shaping)
        normalizedHeight = Mathf.Pow(normalizedHeight, redistributionExponent);
        
        // Apply island mask
        float islandMask = CalculateIslandMask(x, z);
        heights[x, z] = normalizedHeight * islandMask;
    }
}
```

**Height Redistribution**: The power function (`redistributionExponent`) controls terrain character:
- **Exponent < 1**: Flatter terrain with gradual slopes
- **Exponent = 1**: Linear distribution (no change)
- **Exponent > 1**: Steeper peaks and deeper valleys
- **Exponent = 2**: Quadratic falloff, dramatic mountains

#### Step 3: Island Mask Calculation

**Real Center Mode**:
```csharp
float centerX = xRange * 0.5f;
float centerZ = zRange * 0.5f;
float maxDistance = Mathf.Min(xRange, zRange) * 0.5f;

float distanceFromCenter = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ));
float distanceRatio = distanceFromCenter / maxDistance;

if (distanceRatio <= innerEdge)
    return 1f; // Full height in center

// Smooth falloff from edge to inner edge
float t = Mathf.InverseLerp(1f, innerEdge, distanceRatio);
return Mathf.SmoothStep(0f, 1f, t);
```

**Multi-Center Mode**:
- Generates multiple mountain centers with random positions and weights
- Each center has its own sphere of influence
- Final height is the maximum influence from any center
- Creates archipelago-like terrain with multiple peaks

### Texture Painting System

#### TerrainTexturePainter.cs

Automatically assigns textures to terrain based on height values.

**Process**:
1. Define texture layers with height ranges
2. Calculate texture weights for each terrain point
3. Generate alphamap (texture blend map)
4. Apply to Unity terrain system

**Example Layer Configuration**:
```csharp
Layer 0 (Sand):     0.0 - 0.2 height
Layer 1 (Grass):    0.15 - 0.6 height
Layer 2 (Rock):     0.5 - 0.8 height
Layer 3 (Snow):     0.75 - 1.0 height
```

Overlapping ranges create smooth transitions between textures.

### Water Detection System

#### InnerTerrainWaterDitection.cs

Identifies water bodies within the terrain using grid-based analysis.

**Algorithm**:
1. Divide terrain into NxN grid cells
2. Analyze average height of each cell
3. Classify cells as water (low) or land (high)
4. Use flood-fill algorithm to find connected water clusters
5. Place water planes for each cluster

**Grid Classification**:
```csharp
if (avgHeight < waterLowerThreshold) 
    return 1; // Water
else if (avgHeight > waterUpperThreshold) 
    return 0; // Land
else if (/* in skip range */)
    return 2; // Skip detection
```

**Cluster Detection**:
- Uses iterative flood-fill algorithm
- Finds all connected water cells
- Creates separate water planes for isolated lakes/oceans

---

## GPU Instancing System

### Overview

The GPU instancing system renders thousands of objects efficiently by batching draw calls and using GPU hardware instancing.

### Architecture

```
ProceduralObjectSpawnerGPUMaster (Coordinator)
    │
    ├─► ProceduralObjectSpawnerGPU (Grass)
    ├─► ProceduralObjectSpawnerGPU (Flowers)
    ├─► ProceduralObjectSpawnerGPU (Rocks)
    └─► FogChunkSpawner (Fog volumes)
```

### Core Components

#### 1. ProceduralObjectSpawnerGPUMaster.cs

**Purpose**: Centralized coordinator for all GPU spawners.

**Responsibilities**:
- Tracks player position and chunk changes
- Fires events when player enters new chunk
- Prevents redundant updates across multiple spawners

**Key Code**:
```csharp
void Update() {
    var currentChunk = WorldToChunkCoord(player.position);
    if (_lastPlayerChunk != currentChunk) {
        onPlayerMovedToNewChunk?.Invoke(); // Notify all spawners
        _lastPlayerChunk = currentChunk;
    }
}
```

#### 2. ProceduralObjectSpawnerGPU.cs

**Purpose**: Spawns and renders specific object types using GPU instancing.

**Key Features**:
- Chunk-based spatial partitioning
- Frustum culling for visibility optimization
- Blue noise distribution for natural placement
- Terrain-aware positioning (slope, height, normal)

### Detailed Implementation

#### Chunk System

**Chunk Structure**:
```csharp
public class ObjectChunk {
    public Vector2Int coordinate;           // Chunk grid position
    public Matrix4x4[] transforms;          // Transform matrices for all objects
    public int objectCount;                 // Number of objects in chunk
    public bool isGenerated;                // Generation status
    public MaterialPropertyBlock propertyBlock; // Per-chunk rendering properties
    public bool isActive;                   // Visibility flag
}
```

**Chunk Coordinate Calculation**:
```csharp
Vector2Int WorldToChunkCoord(Vector3 worldPos) {
    return new Vector2Int(
        Mathf.FloorToInt(worldPos.x / chunkSize),
        Mathf.FloorToInt(worldPos.z / chunkSize)
    );
}
```

#### Object Generation Process

**Step 1: Determine Chunk Load**:
```csharp
Vector2Int playerChunk = WorldToChunkCoord(player.position);

for (int x = -chunksToRender; x <= chunksToRender; x++) {
    for (int z = -chunksToRender; z <= chunksToRender; z++) {
        Vector2Int chunkCoord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);
        
        // Check distance (squared for performance)
        float sqrDistance = (chunkCoord - playerChunk).sqrMagnitude * chunkSize * chunkSize;
        
        if (sqrDistance <= cullingDistance * cullingDistance) {
            GenerateOrActivateChunk(chunkCoord);
        }
    }
}
```

**Step 2: Generate Objects in Chunk**:
```csharp
void GenerateObjectChunk(Vector2Int chunkCoord) {
    ObjectChunk newChunk = new ObjectChunk(chunkCoord);
    Vector3 chunkWorldPos = ChunkCoordToWorldPos(chunkCoord);
    List<Matrix4x4> chunkTransforms = new List<Matrix4x4>();
    
    for (int i = 0; i < objectDensity && chunkTransforms.Count < maxObjectsPerChunk; i++) {
        Vector3 localPos = new Vector3(
            Random.Range(0, chunkSize),
            0,
            Random.Range(0, chunkSize)
        );
        Vector3 worldPos = chunkWorldPos + localPos;
        
        // Check placement validity
        if (ShouldPlaceObjectAt(worldPos)) {
            // Sample terrain
            float terrainHeight = SampleTerrainHeight(worldPos);
            worldPos.y = terrainHeight + objectYOffset;
            
            // Check slope
            float slope = GetTerrainSlope(worldPos);
            if (slope > 45f) {
                float probability = 1f - (slope / 90f);
                if (Random.value > probability) continue;
            }
            
            // Create transform matrix
            Vector3 terrainNormal = GetTerrainNormal(worldPos);
            Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, terrainNormal);
            Quaternion rotation = normalRotation * Quaternion.Euler(
                Random.Range(-5f, 5f),
                Random.Range(0, 360f),
                Random.Range(-5f, 5f)
            );
            Vector3 scale = Vector3.one * Random.Range(minObjectScale, maxObjectScale);
            
            Matrix4x4 matrix = Matrix4x4.TRS(worldPos, rotation, scale);
            chunkTransforms.Add(matrix);
        }
    }
    
    newChunk.transforms = chunkTransforms.ToArray();
    newChunk.objectCount = chunkTransforms.Count;
    objectChunks.Add(chunkCoord, newChunk);
}
```

#### Blue Noise Distribution

**Purpose**: Creates more natural-looking distribution than pure random or grid patterns.

**Implementation**:
```csharp
bool ShouldPlaceObjectAt(Vector3 worldPos) {
    if (blueNoiseTexture == null) {
        return Random.value > noiseThreshold; // Fallback
    }
    
    // Sample blue noise texture
    Vector2 noiseUV = new Vector2(
        (worldPos.x * noiseScale) % 1f,
        (worldPos.z * noiseScale) % 1f
    );
    
    // Ensure positive UV
    if (noiseUV.x < 0) noiseUV.x += 1f;
    if (noiseUV.y < 0) noiseUV.y += 1f;
    
    Color noiseValue = blueNoiseTexture.GetPixelBilinear(noiseUV.x, noiseUV.y);
    return noiseValue.r > noiseThreshold;
}
```

**Blue Noise Benefits**:
- No obvious patterns or clustering
- Better visual distribution than random
- Prevents "clumping" artifacts
- Maintains statistical randomness

#### Rendering System

**Frustum Culling**:
```csharp
void RenderVisibleObjects() {
    foreach (var kvp in objectChunks) {
        ObjectChunk chunk = kvp.Value;
        
        if (!chunk.isActive || chunk.objectCount == 0) continue;
        
        // Check if chunk is in camera frustum
        if (enableFrustumCulling) {
            Vector3 chunkCenter = ChunkCoordToWorldPos(chunk.coordinate);
            chunkCenter.y = maxTerrainHeight * 0.5f;
            
            Vector3 dirToChunk = (chunkCenter - playerCamera.position).normalized;
            float angle = Vector3.Dot(playerCamera.forward, dirToChunk);
            
            if (angle < cosHalfFOV) continue; // Outside FOV
        }
        
        // Render chunk using GPU instancing
        int rendered = 0;
        while (rendered < chunk.objectCount) {
            int batchSize = Mathf.Min(1023, chunk.objectCount - rendered);
            Matrix4x4[] batch = new Matrix4x4[batchSize];
            System.Array.Copy(chunk.transforms, rendered, batch, 0, batchSize);
            
            Graphics.DrawMeshInstanced(
                objectMesh,
                0,
                objectMaterial,
                batch,
                batchSize,
                chunk.propertyBlock
            );
            
            rendered += batchSize;
        }
    }
}
```

**Performance Optimization**:
- Maximum 1023 instances per draw call (GPU limit)
- Chunks outside camera FOV are skipped
- Inactive chunks are not rendered
- Squared distance calculations avoid expensive sqrt()

### Performance Metrics

**Typical Performance** (RTX 3060, i7-12700):
- 50 chunks × 500 grass = 25,000 instances: ~80 FPS
- 100 chunks × 1000 grass = 100,000 instances: ~45 FPS
- Draw calls: ~25-50 (vs thousands without instancing)

---
## Procedural Texture Generation

### Overview

Two procedural texture generators create realistic terrain textures at runtime using algorithmic generation rather than pre-made image files. While not currently integrated into the main terrain system, these demonstrate advanced procedural content generation techniques.

### Core Components

#### 1. TextureGenerator_Sand.cs

**Purpose**: Generates realistic sand textures using grain-based procedural generation.

**Key Features**:
- Voronoi-style grain distribution for realistic sand particles
- Perlin noise for surface detail and roughness
- Configurable grain size, contrast, and density
- Color variation between light and dark sand
- Tileable textures for seamless material application

**Algorithm Breakdown**:

**Step 1: Grain Center Generation**
```csharp
private Vector2[] GenerateGrainCenters(int count, int width, int height) {
    Vector2[] centers = new Vector2[count];
    
    for (int i = 0; i < count; i++) {
        centers[i] = new Vector2(
            Random.Range(0, width),
            Random.Range(0, height)
        );
    }
    
    return centers;
}
```

**Step 2: Pixel Color Calculation**
```csharp
private Color CalculatePixelColor(int x, int y) {
    // Find distance to nearest grain center (Voronoi)
    float minDistance = float.MaxValue;
    
    foreach (Vector2 center in grainCenters) {
        float distance = Vector2.Distance(new Vector2(x, y), center);
        if (distance < minDistance) {
            minDistance = distance;
        }
    }
    
    // Normalize distance for grain appearance
    float grainValue = Mathf.Clamp01(minDistance / grainSize);
    grainValue = Mathf.Pow(grainValue, contrast); // Apply contrast
    
    // Add Perlin noise for surface detail
    float noiseX = x / (float)textureWidth * detailScale;
    float noiseY = y / (float)textureHeight * detailScale;
    float noiseValue = Mathf.PerlinNoise(noiseX, noiseY);
    
    // Combine grain and detail
    float finalValue = grainValue + (noiseValue - 0.5f) * detailStrength;
    finalValue = Mathf.Clamp01(finalValue);
    
    // Blend between light and dark sand colors
    return Color.Lerp(darkColor, lightColor, finalValue);
}
```

**Technical Details**:
- **Voronoi Diagram**: Each pixel finds its nearest grain center, creating natural cell patterns
- **Contrast Control**: Power function sharpens grain boundaries for more defined particles
- **Detail Layer**: Perlin noise adds micro-surface roughness
- **Color Blending**: Smooth transitions between sand color variations

**Parameters**:
- `grainCount` (500-2000): More grains = finer sand texture
- `grainSize` (0.5-5): Larger values = bigger sand particles
- `contrast` (0.1-3): Higher values = sharper grain edges
- `detailScale` (5-50): Controls frequency of surface roughness
- `detailStrength` (0-0.5): How prominent the surface detail is

#### 2. TextureGenerator_Dirt.cs

**Purpose**: Generates realistic dirt/soil textures with multiple layered elements.

**Key Features**:
- Multi-layer texture composition (soil base, pebbles, particles, moisture, organic matter)
- Perlin noise for natural soil variation
- Procedural pebble/rock placement using Voronoi cells
- Moisture map for wet/dry patches
- Organic matter distribution (decomposed leaves, twigs)
- Highly configurable color palette

**Algorithm Breakdown**:

**Step 1: Base Soil Layer**
```csharp
// Generate base soil using layered Perlin noise
float soilNoise = Mathf.PerlinNoise(
    x / (float)textureWidth * soilScale,
    y / (float)textureHeight * soilScale
);

// Apply contrast to make soil more varied
soilNoise = Mathf.Pow(soilNoise, 2f - soilContrast);

// Base color from light/dark dirt blend
Color baseColor = Color.Lerp(darkDirtColor, lightDirtColor, soilNoise);
```

**Step 2: Pebble Layer**
```csharp
// Find nearest pebble center (Voronoi)
float minPebbleDistance = float.MaxValue;

foreach (Vector2 center in pebbleCenters) {
    float distance = Vector2.Distance(new Vector2(x, y), center);
    if (distance < minPebbleDistance) {
        minPebbleDistance = distance;
    }
}

// Calculate pebble influence
float pebbleValue = Mathf.Clamp01(minPebbleDistance / pebbleSize);
pebbleValue = Mathf.Pow(pebbleValue, pebbleSharpness);

// Blend pebble color with base
if (pebbleValue < 0.5f) {
    baseColor = Color.Lerp(pebbleColor, baseColor, pebbleValue * 2f);
}
```

**Step 3: Particle Detail Layer**
```csharp
// Add fine dirt particles using high-frequency noise
float particleNoise = Mathf.PerlinNoise(
    x / (float)textureWidth * particleScale,
    y / (float)textureHeight * particleScale
);

// Apply particle detail to color
baseColor = Color.Lerp(
    baseColor,
    baseColor * (0.8f + particleNoise * 0.4f),
    particleStrength
);
```

**Step 4: Moisture Layer**
```csharp
// Generate moisture map using Perlin noise
float moistureNoise = Mathf.PerlinNoise(
    x / (float)textureWidth * moistureScale,
    y / (float)textureHeight * moistureScale
);

// Apply moisture effect (darker = wetter)
if (moistureNoise < moistureAmount) {
    float wetness = 1f - (moistureNoise / moistureAmount);
    baseColor = Color.Lerp(baseColor, wetDirtColor, wetness * 0.6f);
}
```

**Step 5: Organic Matter Layer**
```csharp
// Find nearest organic matter
float minOrganicDistance = float.MaxValue;

foreach (Vector2 center in organicCenters) {
    float distance = Vector2.Distance(new Vector2(x, y), center);
    if (distance < minOrganicDistance) {
        minOrganicDistance = distance;
    }
}

// Add organic matter if close enough
float organicValue = Mathf.Clamp01(minOrganicDistance / organicSize);
if (organicValue < organicDensity) {
    baseColor = Color.Lerp(organicColor, baseColor, organicValue / organicDensity);
}
```

**Layering Architecture**:
```
Base Soil (Perlin Noise)
    ↓
Add Pebbles (Voronoi)
    ↓
Add Fine Particles (High-Freq Noise)
    ↓
Apply Moisture (Noise-based Darkening)
    ↓
Add Organic Matter (Voronoi Dark Spots)
    ↓
Final Dirt Texture
```

**Parameters**:
- `soilScale` (5-50): Base soil pattern size
- `pebbleCount` (200-2000): Number of rocks/pebbles
- `pebbleSize` (0.5-8): Size of individual pebbles
- `particleScale` (20-100): Frequency of dirt grain detail
- `moistureScale` (5-30): Size of wet/dry patches
- `moistureAmount` (0-1): Overall wetness level
- `organicCount` (50-500): Amount of decomposed matter
- `organicSize` (1-10): Size of organic bits

### Implementation Details

#### Texture Creation
```csharp
private void CreateTexture() {
    generatedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
    generatedTexture.filterMode = FilterMode.Bilinear; // Smooth filtering
    generatedTexture.wrapMode = TextureWrapMode.Repeat; // Seamless tiling
}
```

#### Efficient Pixel Generation
```csharp
// Generate all pixels at once
Color[] pixels = new Color[textureWidth * textureHeight];

for (int y = 0; y < textureHeight; y++) {
    for (int x = 0; x < textureWidth; x++) {
        int pixelIndex = y * textureWidth + x;
        pixels[pixelIndex] = CalculatePixelColor(x, y);
    }
}

// Apply to GPU texture
generatedTexture.SetPixels(pixels);
generatedTexture.Apply();
```

#### Display Options
```csharp
// Display on UI (for preview/debugging)
if (uiRawImage != null) {
    uiRawImage.texture = generatedTexture;
}

// Apply to material (for actual use)
if (applyToMaterial) {
    Renderer renderer = GetComponent<Renderer>();
    if (renderer != null) {
        renderer.material.mainTexture = generatedTexture;
    }
}
```

### Use Cases

**Current Status**: Experimental/Unused
- Created as learning exercises in procedural texture generation
- Not integrated into main terrain system
- Could be used for runtime texture generation in future

**Potential Applications**:
1. **Dynamic Terrain Textures**: Generate unique textures per terrain instance
2. **Memory Optimization**: Generate textures at runtime instead of storing large files
3. **Texture Variation**: Create multiple variations from same algorithm with different seeds
4. **Real-time Editing**: Adjust parameters and see immediate results
5. **Biome-Specific Textures**: Generate appropriate textures for different terrain types

### Technical Advantages

**Procedural Generation Benefits**:
- **Small Memory Footprint**: Algorithms are tiny compared to texture files
- **Infinite Variations**: Change parameters for unique results
- **Resolution Independence**: Generate at any resolution needed
- **Seamless Tiling**: Easier to ensure textures tile perfectly
- **Runtime Modification**: Adjust appearance dynamically

**Algorithmic Techniques Demonstrated**:
- **Voronoi Diagrams**: Natural cellular patterns (grains, pebbles)
- **Perlin Noise**: Smooth, organic variation (soil, moisture)
- **Multi-Layer Composition**: Combining multiple effects
- **Color Blending**: Smooth transitions between elements
- **Distance Fields**: Calculating influence zones

### Performance Considerations

**Generation Time**:
- 512×512 texture: ~50-100ms (single frame)
- 1024×1024 texture: ~200-400ms (may cause stutter)
- 2048×2048 texture: ~800ms-1.5s (should be done at load time)

**Optimization Strategies**:
- Generate textures during loading screens
- Use lower resolutions for distant terrain
- Cache generated textures for reuse
- Consider using compute shaders for GPU acceleration

### Future Integration Ideas

1. **Terrain Texture System Integration**:
   - Replace static texture painting with procedural generation
   - Generate textures based on terrain height/slope
   - Blend multiple procedural textures per biome

2. **Runtime Customization**:
   - Allow players to adjust terrain appearance
   - Generate seasonal variations (wet spring, dry summer)
   - Weather-based texture changes

3. **Compute Shader Port**:
   - Move generation to GPU for real-time updates
   - Generate normal maps procedurally
   - Create displacement maps for tessellation

### Code Example: Integration with Terrain

```csharp
// Example: How to integrate with terrain system
public class ProceduralTerrainTextures : MonoBehaviour {
    [SerializeField] private TextureGenerator_Sand sandGen;
    [SerializeField] private TextureGenerator_Dirt dirtGen;
    [SerializeField] private Terrain terrain;
    
    public void ApplyProceduralTextures() {
        TerrainLayer[] layers = new TerrainLayer[2];
        
        // Generate sand texture
        Texture2D sandTexture = sandGen.GenerateTexture();
        layers[0] = new TerrainLayer();
        layers[0].diffuseTexture = sandTexture;
        
        // Generate dirt texture
        Texture2D dirtTexture = dirtGen.GenerateTexture();
        layers[1] = new TerrainLayer();
        layers[1].diffuseTexture = dirtTexture;
        
        // Apply to terrain
        terrain.terrainData.terrainLayers = layers;
    }
}
```

---


## Particle System Implementation

### VFX_ParticalSystem.cs

A custom CPU-based particle system built from scratch without Unity's built-in ParticleSystem.

### Architecture

**Particle Class**:
```csharp
public class Particle {
    public Vector3 position;
    public Vector3 velocity;
    public float age;
    public float lifetime;
    public bool isActive;
}
```

### Particle Lifecycle

#### 1. Initialization

```csharp
void Start() {
    CreateParticleMesh(); // Simple quad
    
    // Initialize particle pool
    for (int i = 0; i < maxParticles; i++) {
        particles.Add(new Particle());
    }
}
```

#### 2. Emission

```csharp
void EmitParticles() {
    timeSinceLastEmission += Time.deltaTime;
    
    if (timeSinceLastEmission >= 1f / emissionRate) {
        SpawnParticle();
        timeSinceLastEmission = 0f;
    }
}

void SpawnParticle() {
    Particle particle = GetInactiveParticle();
    if (particle == null) return;
    
    particle.isActive = true;
    particle.age = 0f;
    particle.lifetime = particleLifetime + Random.Range(-0.5f, 0.5f);
    
    // Random position within spawn area
    particle.position = transform.position + new Vector3(
        Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
        Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
        Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
    );
    
    // Random velocity
    particle.velocity = startVelocity + new Vector3(
        Random.Range(-1f, 1f) * velocityMultiplier,
        Random.Range(-1f, 1f) * velocityMultiplier,
        Random.Range(-1f, 1f) * velocityMultiplier
    );
}
```

#### 3. Update

```csharp
void UpdateParticles() {
    foreach (Particle p in particles) {
        if (!p.isActive) continue;
        
        // Update physics
        p.velocity += gravity * Time.deltaTime;
        p.position += p.velocity * Time.deltaTime;
        
        // Update age
        p.age += Time.deltaTime;
        
        // Deactivate old particles
        if (p.age >= p.lifetime) {
            p.isActive = false;
        }
    }
}
```

#### 4. Rendering

```csharp
void RenderParticles() {
    foreach (Particle p in particles) {
        if (!p.isActive) continue;
        
        // Calculate color based on lifetime
        float t = p.age / p.lifetime;
        Color color;
        if (t < 0.5f) {
            color = Color.Lerp(startColor, midColor, t * 2f);
        } else {
            color = Color.Lerp(midColor, endColor, (t - 0.5f) * 2f);
        }
        
        // Calculate size
        float size = Mathf.Lerp(startSize, endSize, t);
        
        // Billboard rotation (face camera)
        Quaternion rotation = Quaternion.LookRotation(
            Camera.main.transform.position - p.position
        );
        
        // Create transform matrix
        Matrix4x4 matrix = Matrix4x4.TRS(
            p.position,
            rotation,
            Vector3.one * size
        );
        
        // Set material properties
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetColor("_Color", color);
        
        // Draw particle
        Graphics.DrawMesh(
            particleMesh,
            matrix,
            particleMaterial,
            0,
            null,
            0,
            props
        );
    }
}
```

### Features

- **Object Pooling**: Reuses inactive particles for efficiency
- **Physics Simulation**: Gravity and velocity integration
- **Color Gradients**: Smooth transitions through 3 color stops
- **Size Animation**: Particles shrink/grow over lifetime
- **Billboard Rendering**: Always faces camera
- **Spawn Area**: Configurable emission volume

---

## Volumetric Cloud System

### VolumetricClouds.cs (Cellular Automata)

Generates 3D cloud volumes using cellular automata algorithm.

### Cellular Automata Algorithm

**Concept**: Each cell in a 3D grid is either "alive" (cloud) or "dead" (air) based on its neighbors.

**Rules**:
- If cell is alive and has >= `deathThreshold` neighbors: stays alive
- If cell is dead and has >= `birthThreshold` neighbors: becomes alive
- Otherwise: cell state inverts

### Implementation

#### 1. Grid Initialization

```csharp
void InitializeGrid() {
    for (int x = 0; x < width; x++) {
        for (int y = 0; y < height; y++) {
            for (int z = 0; z < depth; z++) {
                // Random initial state based on density
                grid[x, y, z] = Random.value < initialDensity;
            }
        }
    }
}
```

#### 2. Simulation Step

```csharp
bool[,,] SimulateStep(bool[,,] oldGrid) {
    bool[,,] newGrid = new bool[width, height, depth];
    
    for (int x = 0; x < width; x++) {
        for (int y = 0; y < height; y++) {
            for (int z = 0; z < depth; z++) {
                int neighbors = CountNeighbors(oldGrid, x, y, z);
                
                if (oldGrid[x, y, z]) {
                    // Alive cell: check death threshold
                    newGrid[x, y, z] = neighbors >= deathThreshold;
                } else {
                    // Dead cell: check birth threshold
                    newGrid[x, y, z] = neighbors >= birthThreshold;
                }
            }
        }
    }
    
    return newGrid;
}
```

#### 3. Neighbor Counting

```csharp
int CountNeighbors(bool[,,] grid, int x, int y, int z) {
    int count = 0;
    
    // Check 26 neighbors (3x3x3 cube minus center)
    for (int dx = -1; dx <= 1; dx++) {
        for (int dy = -1; dy <= 1; dy++) {
            for (int dz = -1; dz <= 1; dz++) {
                if (dx == 0 && dy == 0 && dz == 0) continue; // Skip center
                
                int nx = x + dx;
                int ny = y + dy;
                int nz = z + dz;
                
                // Check bounds
                if (nx >= 0 && nx < width && 
                    ny >= 0 && ny < height && 
                    nz >= 0 && nz < depth) {
                    if (grid[nx, ny, nz]) count++;
                }
            }
        }
    }
    
    return count;
}
```

#### 4. Visualization

```csharp
void CreateCloudMesh() {
    for (int x = 0; x < width; x++) {
        for (int y = 0; y < height; y++) {
            for (int z = 0; z < depth; z++) {
                if (grid[x, y, z]) {
                    // Instantiate cube at this position
                    GameObject cube = Instantiate(
                        cloudPrefab,
                        cloudParent.transform
                    );
                    cube.transform.localPosition = new Vector3(
                        x * cubeSize,
                        y * cubeSize,
                        z * cubeSize
                    );
                    cube.transform.localScale = Vector3.one * cubeSize;
                }
            }
        }
    }
}
```

### Parameter Tuning

**Initial Density** (0.0 - 1.0):
- Low (0.2-0.3): Sparse, wispy clouds
- Medium (0.4-0.5): Balanced cloud formation
- High (0.6-0.7): Dense, thick clouds

**Birth Threshold** (10-15):
- Lower: More cloud growth, fuller clouds
- Higher: Slower growth, sparse clouds

**Death Threshold** (8-14):
- Lower: Clouds dissolve faster, more holes
- Higher: Clouds maintain shape, denser

**Iterations** (3-6):
- More iterations = smoother, more organic shapes
- Too many = clouds become too uniform

---

## NPC AI System

### NPC_Controller.cs

A complete AI system with state machine, perception, and pathfinding.

### State Machine Architecture

```
┌─────────┐
│  IDLE   │
└────┬────┘
     │
     ▼
┌─────────┐
│ PATROL  │◄──────────────────┐
└────┬────┘                   │
     │                        │
     │ (detect player)        │
     ▼                        │
┌─────────┐                   │
│  CHASE  │───(lose sight)────┘
└────┬────┘
     │
     │ (in attack range)
     ▼
┌─────────┐
│ ATTACK  │
└─────────┘
```

### State Implementations

#### 1. Detection System

```csharp
void CheckRanges() {
    if (target == null) return;
    
    // 2D distance (ignore Y axis)
    Vector2 npcPos2D = new Vector2(transform.position.x, transform.position.z);
    Vector2 targetPos2D = new Vector2(target.position.x, target.position.z);
    float distance = Vector2.Distance(npcPos2D, targetPos2D);
    
    isInDetectionRange = distance <= detectionRange;
    
    // Vision cone check
    if (isInDetectionRange) {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
        isInVisionAngle = angleToTarget <= visionAngle / 2f;
    }
    
    // Attack range check
    isInAttackRange = distance <= attackRange;
}
```

#### 2. Line-of-Sight Check

```csharp
void CheckNpcState() {
    // ... range checks ...
    
    if (isInDetectionRange && isInVisionAngle && !isInAttackRange) {
        // Raycast to check for obstacles
        Vector3 rayCastStart = eyesTransform.position;
        Vector3 rayCastEnd = new Vector3(
            target.position.x,
            rayCastStart.y,
            target.position.z
        );
        
        RaycastHit hit;
        if (Physics.Raycast(
            rayCastStart,
            (rayCastEnd - rayCastStart).normalized,
            out hit,
            detectionRange
        )) {
            if (hit.transform != target) {
                // Obstacle blocking view
                currentState = NPC_State.Patrol;
                return;
            }
        }
        
        currentState = NPC_State.Chase;
    }
}
```

#### 3. State Actions

**Patrol**:
```csharp
if (currentState == NPC_State.Patrol) {
    if (roamPoints.Length == 0) return;
    
    agent.speed = roamSpeed;
    
    if (!agent.hasPath || agent.remainingDistance < agent.stoppingDistance) {
        // Move to next patrol point
        currentRoamIndex = (currentRoamIndex + 1) % roamPoints.Length;
        agent.SetDestination(roamPoints[currentRoamIndex].position);
    }
}
```

**Chase**:
```csharp
if (currentState == NPC_State.Chase) {
    if (target == null) return;
    agent.speed = chaseSpeed;
    agent.SetDestination(target.position);
}
```

**Attack**:
```csharp
if (currentState == NPC_State.Attack) {
    agent.ResetPath(); // Stop moving
    animator.SetBool("Attack", true);
}
```

### Animation Integration

```csharp
void Update() {
    CheckRanges();
    CheckNpcState();
    PerformActions();
    
    // Update animator with velocity
    float velocity = agent.velocity.magnitude;
    animator.SetFloat("Velocity", velocity / chaseSpeed);
}
```

**Animation Parameters**:
- `Velocity` (float): Blend tree for walk/run animations
- `Attack` (bool): Triggers attack animation

### Gizmo Visualization

```csharp
void OnDrawGizmos() {
    // Detection range
    Gizmos.color = isInDetectionRange ? Color.green : Color.red;
    Gizmos.DrawWireSphere(transform.position, detectionRange);
    
    // Vision cone
    Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward;
    Vector3 rightBoundary = Quaternion.Euler(0, visionAngle / 2f, 0) * transform.forward;
    Gizmos.DrawLine(transform.position, transform.position + leftBoundary * detectionRange);
    Gizmos.DrawLine(transform.position, transform.position + rightBoundary * detectionRange);
    
    // Attack range
    Gizmos.color = isInAttackRange ? Color.blue : Color.yellow;
    Gizmos.DrawWireSphere(transform.position, attackRange);
    
    // Line of sight ray
    if (target != null) {
        Gizmos.color = (currentState == NPC_State.Chase) ? Color.green : Color.red;
        Gizmos.DrawLine(eyesTransform.position, target.position);
    }
}
```

---

## Collision Detection System

### Player_HitCollisionManager.cs

Multi-layer collision system for precise hit detection.

### System Architecture

```
Layer 1 (Outer Shell)
  └─► Layer 2 (Mid Shell)
        └─► Layer 3 (Core)
```

**Detection Logic**:
- All 3 layers must be penetrated for full collision
- Early exit if outer layers not hit (optimization)
- Each layer can have different radius

### Mathematical Foundation

#### Sphere-Line Segment Intersection

**Problem**: Determine if a line segment AB intersects a sphere with center C and radius r.

**Solution** (from MathUtils.cs):

```csharp
public static bool CheckSpereCollisionWithLine(
    Vector3 linePointA,
    Vector3 linePointB,
    Vector3 sphereCenter,
    float sphereRadius
) {
    Vector3 d = linePointB - linePointA;  // Line direction
    Vector3 f = linePointA - sphereCenter; // Line start to sphere center
    
    // Quadratic equation coefficients
    float a = Vector3.Dot(d, d);
    float b = 2 * Vector3.Dot(f, d);
    float c = Vector3.Dot(f, f) - sphereRadius * sphereRadius;
    
    float discriminant = b * b - 4 * a * c;
    
    if (discriminant < 0) {
        return false; // No intersection
    }
    
    discriminant = Mathf.Sqrt(discriminant);
    float t1 = (-b - discriminant) / (2 * a);
    float t2 = (-b + discriminant) / (2 * a);
    
    // Check if intersection occurs within segment [0, 1]
    return (t1 >= 0 && t1 <= 1) || (t2 >= 0 && t2 <= 1);
}
```

**Mathematical Derivation**:

1. Parametric line: P(t) = A + t(B - A), where t ∈ [0, 1]
2. Sphere equation: ||P - C||² = r²
3. Substitute: ||(A + t(B - A)) - C||² = r²
4. Expand: ||f + td||² = r² where f = A - C, d = B - A
5. Expand dot product: (d·d)t² + 2(f·d)t + (f·f - r²) = 0
6. Solve quadratic for t values
7. Check if t ∈ [0, 1] for segment intersection

### Layer-Based Detection

```csharp
public bool CheckCollisionWithGivenLine(Vector3 linePointA, Vector3 linePointB) {
    // Layer 1 check
    foreach (var point1 in Layer_1_CollitionPoints) {
        if (point1 != null && MathUtils.CheckSpereCollisionWithLine(
            linePointA, linePointB, point1.position, Layer_1_CollitionRadius
        )) {
            // Layer 2 check
            foreach (var point2 in Layer_2_CollitionPoints) {
                if (point2 != null && MathUtils.CheckSpereCollisionWithLine(
                    linePointA, linePointB, point2.position, Layer_2_CollitionRadius
                )) {
                    // Layer 3 check
                    foreach (var point3 in Layer_3_CollitionPoints) {
                        if (point3 != null && MathUtils.CheckSpereCollisionWithLine(
                            linePointA, linePointB, point3.position, Layer_3_CollitionRadius
                        )) {
                            return true; // Full penetration
                        }
                    }
                    return true; // Partial penetration
                }
            }
        }
    }
    return false; // No collision
}
```

### Use Cases

**Weapon Hit Detection**:
```csharp
// In weapon script
Vector3 weaponStart = weaponTip.position - weaponTip.forward * weaponLength;
Vector3 weaponEnd = weaponTip.position;

if (enemyCollisionManager.CheckCollisionWithGivenLine(weaponStart, weaponEnd)) {
    // Apply damage
    enemy.TakeDamage(weaponDamage);
}
```

---

## Environmental Systems

### Day/Night Cycle

#### Mng_DayAndNightCycle.cs

**Features**:
- 24-hour time system
- Sun rotation based on time
- Dynamic light intensity using animation curves
- Color temperature transitions
- Configurable time speed

**Implementation**:
```csharp
void Update() {
    currentTime += Time.deltaTime * timeSpeed;
    if (currentTime >= 24f) {
        currentTime -= 24f;
    }
    UpdateLight();
}

void UpdateLight() {
    // Rotate sun based on time
    float sunRotation = (currentTime / 24f) * 360f;
    sunLight.transform.rotation = Quaternion.Euler(sunRotation - 90f, sunPosition, 0f);
    
    // Intensity curve
    float normalizedTime = currentTime / 24f;
    float intensityCurveValue = sunIntensityCurve.Evaluate(normalizedTime);
    hdData.intensity = sunIntensity * intensityCurveValue;
    
    // Color temperature
    float temperatureCurveValue = lightTemperatureCurve.Evaluate(normalizedTime);
    sunLight.colorTemperature = temperatureCurveValue * 6750;
}
```

**Animation Curves**:
- **Intensity Curve**: 0 at midnight, 1 at noon
- **Temperature Curve**: Warm (0.8) at sunrise/sunset, cool (1.0) at noon

### Fog System

#### FogChunkSpawner.cs

**Features**:
- Chunk-based fog placement
- Height-based spawning (valleys only)
- Distance-based or count-based loading
- Automatic cleanup of distant fog

**Algorithm**:
```csharp
void UpdateFogChunks() {
    Vector2Int playerChunk = GetChunkCoord(player.position);
    HashSet<Vector2Int> chunksToKeep = new HashSet<Vector2Int>();
    
    for (int x = -chunksAroundPlayer; x <= chunksAroundPlayer; x++) {
        for (int z = -chunksAroundPlayer; z <= chunksAroundPlayer; z++) {
            Vector2Int chunkCoord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);
            Vector3 chunkWorldPos = GetChunkWorldPosition(chunkCoord);
            
            // Height check
            float normalizedHeight = chunkWorldPos.y / maxTerrainHeight;
            if (normalizedHeight < minHeightLimit || normalizedHeight > maxHeightLimit)
                continue;
            
            chunksToKeep.Add(chunkCoord);
            SpawnFogChunkIfNeeded(chunkCoord);
        }
    }
    
    // Remove distant fog
    RemoveFogNotInSet(chunksToKeep);
}
```

---

## Performance Optimization

### Optimization Techniques Used

#### 1. Spatial Partitioning
- **Chunk System**: Divides world into fixed-size cells
- **Benefits**: Only process nearby chunks, easy culling
- **Cost**: Memory overhead for chunk tracking

#### 2. Frustum Culling
```csharp
// Check if object is in camera view
Vector3 dirToObject = (objectPos - cameraPos).normalized;
float angle = Vector3.Dot(cameraForward, dirToObject);
if (angle < cosHalfFOV) {
    return; // Outside field of view
}
```

#### 3. Distance Culling
```csharp
// Use squared distance to avoid sqrt
float sqrDistance = (objectPos - playerPos).sqrMagnitude;
if (sqrDistance > cullingDistance * cullingDistance) {
    return; // Too far
}
```

#### 4. Event-Driven Updates
```csharp
// Only update when necessary
if (playerChunk != lastPlayerChunk) {
    onChunkChanged?.Invoke();
    lastPlayerChunk = playerChunk;
}
```

#### 5. Object Pooling
- Particle system reuses inactive particles
- Chunk objects persist between activations
- Avoids garbage collection overhead

#### 6. GPU Instancing
- Batches identical objects into single draw call
- Reduces CPU-GPU communication
- Supports up to 1023 instances per batch

### Performance Monitoring

#### Mng_FpsCounter.cs

Displays real-time performance metrics:
- FPS (frames per second)
- Frame time in milliseconds
- Running average

### Profiling Tips

1. **Unity Profiler**:
   - CPU Usage → Rendering for draw calls
   - CPU Usage → Scripts for logic overhead
   - Memory → Track allocation spikes

2. **Frame Debugger**:
   - View actual draw calls
   - Verify GPU instancing is working
   - Check batching efficiency

3. **Statistics Window**:
   - Draw calls: Aim for < 100
   - Batches: Higher is better for instancing
   - Triangles: Monitor poly count

---

## Shader Implementations

### Water Shader (Shader Graph)

**Location**: `Assets/Shaders/Water/WaterShader.shadergraph`

**Features**:
- Animated wave displacement using sine waves
- Normal mapping for surface detail
- Refraction effect for underwater visibility
- Reflection sampling from scene
- Foam at shallow depths

**Key Nodes**:
1. **Wave Animation**:
   - Time node → Multiply by wave speed
   - Add to UV coordinates
   - Sample normal map with animated UVs

2. **Depth Fade**:
   - Scene Depth node
   - Compare with fragment depth
   - Controls transparency and foam

3. **Refraction**:
   - Normal map distortion
   - Offset screen UVs
   - Sample scene color texture

### Fog Shader (Shader Graph)

**Location**: `Assets/Shaders/Fog/FOG.shadergraph`

**Features**:
- Volumetric appearance using soft particles
- Depth fade for smooth integration
- Height-based density
- Distance fade

**Implementation**:
- Soft particle factor based on depth difference
- Alpha blending with scene
- Noise texture for variation

---

## API Reference

### PerlinNoiseTerrainGenerator

#### Public Methods

**`void GenerateNewTerrain()`**
- Generates a new random terrain
- Randomizes seed offsets
- Triggers `onTerrainGenerated` event

**`float GetXRange()`**
- Returns terrain width

**`float GetZRange()`**
- Returns terrain depth

**`float GetYRange()`**
- Returns terrain maximum height

**`float[,] GetTerrainHeights()`**
- Returns normalized height map (0-1 range)

#### Public Events

**`UnityEvent onTerrainGenerated`**
- Fired when terrain generation completes

### ProceduralObjectSpawnerGPU

#### Public Fields

**`Mesh objectMesh`**
- Mesh to instance

**`Material objectMaterial`**
- Material (must have GPU instancing enabled)

**`float objectDensity`**
- Target objects per chunk

**`int chunksToRender`**
- Radius of chunks around player

**`float chunkSize`**
- Size of each chunk in world units

**`float cullingDistance`**
- Maximum render distance

### NPC_Controller

#### Public Methods

*None exposed - uses Inspector serialization*

#### Serialized Fields

**`Transform target`**
- Player or target to track

**`float detectionRange`**
- Maximum detection distance

**`float visionAngle`**
- Field of view in degrees

**`Transform[] roamPoints`**
- Patrol path waypoints

### MathUtils

#### Static Methods

**`bool CheckSpereCollisionWithLine(Vector3 linePointA, Vector3 linePointB, Vector3 sphereCenter, float sphereRadius)`**
- Returns true if line segment intersects sphere
- Uses parametric line equation and quadratic solver

---

## Code Style Guidelines

### Naming Conventions

- **Public fields**: camelCase with clear names
- **Private fields**: _camelCase with underscore prefix
- **Methods**: PascalCase, verb-based names
- **Classes**: PascalCase, noun-based names
- **Managers**: Prefix with `Mng_`
- **Interfaces**: Prefix with `I`

### Component Organization

```csharp
// 1. Serialized fields (Inspector visible)
[Header("Settings")]
[SerializeField] private float speed = 10f;

// 2. Public properties
public float Speed => speed;

// 3. Private fields
private float _currentVelocity;

// 4. Unity lifecycle methods
void Start() { }
void Update() { }

// 5. Public methods
public void DoSomething() { }

// 6. Private methods
private void InternalLogic() { }

// 7. Nested classes/structs
private class Helper { }
```

### Performance Considerations

- Use `[SerializeField]` instead of public for Inspector fields
- Cache component references in `Start()`/`Awake()`
- Avoid `GetComponent()` in `Update()`
- Use squared distance for comparisons
- Profile before optimizing

---

## Troubleshooting

### Common Issues

**Issue: Objects not rendering with GPU instancing**
- Solution: Ensure material has "Enable GPU Instancing" checked
- Verify mesh has proper UVs and normals

**Issue: Terrain appears flat**
- Solution: Increase `yRange` parameter
- Adjust `redistributionExponent` for more variation
- Add more octaves for detail

**Issue: NPC not detecting player**
- Solution: Ensure NavMesh is baked
- Check detection range and vision angle
- Verify target is assigned in Inspector

**Issue: Poor performance with many objects**
- Solution: Reduce `objectDensity`
- Decrease `chunksToRender`
- Enable frustum culling
- Lower `cullingDistance`

**Issue: Water not appearing**
- Solution: Check `waterHeight` parameter
- Verify water prefab is assigned
- Ensure terrain has low areas for water

---

## Future Development

### Planned Features

1. **Compute Shader Terrain Generation**
   - Move noise calculation to GPU
   - Significantly faster generation
   - Support for massive terrains

2. **Biome System**
   - Multiple terrain types (desert, forest, tundra)
   - Biome-specific vegetation
   - Climate zones

3. **Advanced Weather**
   - Rain particle system
   - Dynamic clouds
   - Lightning effects
   - Weather transitions

4. **Save/Load System**
   - Serialize generated worlds
   - Save terrain seeds
   - Player progress persistence

5. **Procedural Structures**
   - Buildings and ruins
   - Dungeon generation
   - Road networks

---

## References & Resources

### Academic Papers
- Ken Perlin - "An Image Synthesizer" (1985)
- Cellular Automata for procedural generation

### Online Resources
- Unity Documentation: GPU Instancing
- Sebastian Lague: Procedural Landmass Generation
- Catlike Coding: Noise and Gradients

### Books
- "Procedural Content Generation in Games" - Noor Shaker et al.
- "Real-Time Rendering" - Tomas Akenine-Möller et al.

---

**Last Updated**: October 25, 2025  
**Unity Version**: 6000.0.58f2 (Unity 6)
**Unity Version**: 6000.0.58f2 (Unity 6)

---

*This documentation is maintained as part of the Computer Graphics Simulation project. For questions or contributions, please refer to the project repository.*

