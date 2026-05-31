using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Managed system that paints a <see cref="Texture2D"/> heatmap each frame
/// from every entity carrying a <see cref="HeatSignatureData"/> component.
/// Uses <see cref="SystemBase"/> (class) so that managed fields like Texture2D and arrays
/// are kept alive across frames. <see cref="ISystem"/> is a struct and cannot safely
/// hold managed references between OnCreate and OnUpdate.
/// The resulting texture is stored in <see cref="RadarTextureHolder.Texture"/> and consumed
/// by <see cref="RadarUIBridge"/>.
/// Buffers are allocated lazily on the first <see cref="OnUpdate"/> once the config singleton
/// is available, and reallocated if <see cref="RadarRendererConfig.MapSizePixels"/> changes.
/// </summary>
public partial class RadarRendererSystem : SystemBase
{
    private int       _allocatedSize;
    private int[]     _heatGrid;
    private Color32[] _pixels;
    private Texture2D _texture;
    private Unity.Mathematics.Random _random;

    protected override void OnCreate()
    {
        RequireForUpdate<RadarRendererConfig>();
        _random = new Unity.Mathematics.Random((uint)Environment.TickCount | 1u);
    }

    protected override void OnDestroy()
    {
        if (_texture != null)
            UnityEngine.Object.Destroy(_texture);

        RadarTextureHolder.Texture = null;
    }

    protected override void OnUpdate()
    {
        RadarRendererConfig config  = SystemAPI.GetSingleton<RadarRendererConfig>();
        int                 mapSize = config.MapSizePixels;

        // Allocate (or reallocate) buffers when the configured size changes.
        if (mapSize != _allocatedSize)
            ReallocateBuffers(mapSize);

        // 1 — Clear grid.
        Array.Clear(_heatGrid, 0, _heatGrid.Length);

        // 2 — Accumulate heat from all qualifying entities.
        foreach (var (dataRef, transformRef) in
                 SystemAPI.Query<RefRO<HeatSignatureData>, RefRO<LocalTransform>>())
        {
            HeatSignatureData data      = dataRef.ValueRO;
            LocalTransform    transform = transformRef.ValueRO;

            if ((data.SignatureType & config.FilterFlags) == 0)
                continue;

            // World → pixel space. World origin is at (0,0), world extents = worldSize.
            float normalizedX = (float)(transform.Position.x / WorldParams.worldSize);
            float normalizedZ = (float)(transform.Position.z / WorldParams.worldSize);
            int centerX = math.clamp((int)(normalizedX * mapSize), 0, mapSize - 1);
            int centerY = math.clamp((int)(normalizedZ * mapSize), 0, mapSize - 1);

            int spread = data.Spread;
            for (int h = 0; h < data.Heat; h++)
            {
                int dx = _random.NextInt(-spread, spread + 1);
                int dy = _random.NextInt(-spread, spread + 1);
                int x  = math.clamp(centerX + dx, 0, mapSize - 1);
                int y  = math.clamp(centerY + dy, 0, mapSize - 1);
                _heatGrid[y * mapSize + x]++;
            }
        }

        // 3 — Find peak cell for normalization.
        int maxCell = 0;
        for (int i = 0; i < _heatGrid.Length; i++)
            if (_heatGrid[i] > maxCell)
                maxCell = _heatGrid[i];

        // 4 — Colorize.
        Color32 black  = new Color32(0, 0, 0, 255);
        Color32 target = new Color32(
            (byte)(config.TargetColorR * 255f),
            (byte)(config.TargetColorG * 255f),
            (byte)(config.TargetColorB * 255f),
            255);

        if (maxCell == 0)
        {
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = black;
        }
        else
        {
            float invMax = 1f / maxCell;
            for (int i = 0; i < _pixels.Length; i++)
            {
                float t = _heatGrid[i] * invMax;
                _pixels[i] = Color32.Lerp(black, target, t);
            }
        }

        // 5 — Upload to GPU.
        _texture.SetPixels32(_pixels);
        _texture.Apply();
    }

    /// <summary>
    /// Destroys the old texture and recreates all buffers at the new <paramref name="mapSize"/>.
    /// Also refreshes <see cref="RadarTextureHolder.Texture"/> so <see cref="RadarUIBridge"/>
    /// picks up the new instance automatically.
    /// </summary>
    private void ReallocateBuffers(int mapSize)
    {
        if (_texture != null)
            UnityEngine.Object.Destroy(_texture);

        _heatGrid = new int[mapSize * mapSize];
        _pixels   = new Color32[mapSize * mapSize];
        _texture  = new Texture2D(mapSize, mapSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
        };

        RadarTextureHolder.Texture = _texture;
        _allocatedSize = mapSize;
    }
}
