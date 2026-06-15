using THPerfection.LevelGen.Authoring;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace THPerfection.LevelGen
{
    /// <summary>
    /// Spawns room prefabs (or procedural placeholders) from a generation result.
    /// Phase 3a bridge: grid layout → scene objects + door state visuals.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelGenRoomSpawner : MonoBehaviour
    {
        [Header("Colors")]
        public Color FloorColor = new(0.55f, 0.55f, 0.58f, 1f);
        public Color OpenDoorColor = new(1f, 0.85f, 0.1f, 1f);
        public Color ConnectedDoorColor = new(0.2f, 0.9f, 0.35f, 1f);
        public Color ClosedDoorColor = new(0.85f, 0.25f, 0.2f, 1f);

        [Header("Legacy Prefab Mapping (optional)")]
        [Tooltip("Used when catalog entry has no prefab.")]
        public GameObject DefaultRoomPrefab;

        public RoomPrefabMapping[] PrefabMappings = System.Array.Empty<RoomPrefabMapping>();

        [Header("Procedural Fallback")]
        [Tooltip("When no prefab is available, build floor tiles and door markers from grid data.")]
        public bool UseProceduralFallback = true;

        RoomCatalog _activeCatalog;
        Transform _spawnRoot;

        public Transform SpawnRoot
        {
            get
            {
                if (_spawnRoot == null)
                {
                    var existing = transform.Find("SpawnedRooms");
                    _spawnRoot = existing != null
                        ? existing
                        : new GameObject("SpawnedRooms").transform;
                    _spawnRoot.SetParent(transform, false);
                }

                return _spawnRoot;
            }
        }

        public void ClearSpawned()
        {
            Transform root = SpawnRoot;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        public void Spawn(
            in BuildingGenerationResult result,
            in BuildingGenConfig config,
            RoomCatalog catalog)
        {
            _activeCatalog = catalog;
            ClearSpawned();

            if (result?.Instances == null || result.Instances.Count == 0)
                return;

            float cellSize = Mathf.Max(0.01f, config.CellSize);
            float floorY = config.FloorY;
            float doorY = floorY + 0.5f;

            foreach (RoomInstance instance in result.Instances)
            {
                if (!catalog.TryGetTemplate(instance.TemplateId, out RoomTemplateDefinition template))
                    continue;

                SpawnRoom(instance, in template, cellSize, floorY, doorY);
            }
        }

        void SpawnRoom(
            RoomInstance instance,
            in RoomTemplateDefinition template,
            float cellSize,
            float floorY,
            float doorY)
        {
            var roomRoot = new GameObject($"Room_{instance.Id}_{instance.TemplateId}");
            roomRoot.transform.SetParent(SpawnRoot, false);
            roomRoot.transform.position = LevelGenWorldTransform.RoomRootPosition(
                instance.Origin, cellSize, floorY);
            roomRoot.transform.rotation = LevelGenWorldTransform.RoomRootRotation(instance.Rotation);

            GameObject prefab = ResolvePrefab(instance.TemplateId);
            DoorSocketMarker[] doorMarkers = null;
            LevelGenRoomVisualAuthoring visualAuthoring = null;

            if (prefab != null)
            {
                GameObject art = Instantiate(prefab, roomRoot.transform);
                art.transform.localPosition = Vector3.zero;
                art.transform.localRotation = Quaternion.identity;
                doorMarkers = art.GetComponentsInChildren<DoorSocketMarker>(true);
                visualAuthoring = art.GetComponentInChildren<LevelGenRoomVisualAuthoring>();
            }
            else if (UseProceduralFallback)
            {
                BuildProceduralFloor(roomRoot.transform, in template, cellSize);
            }

            if (doorMarkers != null && doorMarkers.Length > 0)
            {
                foreach (DoorSocketMarker marker in doorMarkers)
                    marker.ApplyFromInstance(instance);
            }
            else if (visualAuthoring != null)
            {
                visualAuthoring.ApplyFromInstance(instance);
            }
            else
            {
                BuildProceduralDoors(SpawnRoot, instance, cellSize, doorY);
            }
        }

        GameObject ResolvePrefab(string templateId)
        {
            if (_activeCatalog != null && _activeCatalog.TryGetPrefab(templateId, out GameObject catalogPrefab))
                return catalogPrefab;

            for (int i = 0; i < PrefabMappings.Length; i++)
            {
                if (PrefabMappings[i].TemplateId == templateId && PrefabMappings[i].Prefab != null)
                    return PrefabMappings[i].Prefab;
            }

            return DefaultRoomPrefab;
        }

        void BuildProceduralFloor(Transform roomRoot, in RoomTemplateDefinition template, float cellSize)
        {
            const float floorHeight = 0.2f;

            foreach (int2 localCell in template.Cells)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"Floor_{localCell.x}_{localCell.y}";
                tile.transform.SetParent(roomRoot, false);
                tile.transform.localPosition = new Vector3(
                    localCell.x * cellSize,
                    floorHeight * 0.5f,
                    localCell.y * cellSize);
                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale = new Vector3(cellSize * 0.92f, floorHeight, cellSize * 0.92f);
                TintRenderer(tile.GetComponent<Renderer>(), FloorColor);
            }
        }

        void BuildProceduralDoors(
            Transform parent,
            RoomInstance instance,
            float cellSize,
            float doorY)
        {
            foreach (KeyValuePair<DoorEdgeKey, CellDoorState> entry in instance.DoorStates)
            {
                DoorEdgeKey key = entry.Key;
                CellDoorState state = entry.Value;

                var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
                door.name = $"Door_{instance.Id}_{key.Side}_{key.Cell.x}_{key.Cell.y}";
                door.transform.SetParent(parent, false);
                door.transform.position = LevelGenWorldTransform.DoorEdgeCenter(
                    key.Cell, key.Side, cellSize, doorY);
                door.transform.rotation = Quaternion.identity;
                door.transform.localScale = LevelGenWorldTransform.DoorEdgeSize(key.Side, cellSize);

                Color color = state switch
                {
                    CellDoorState.Open      => OpenDoorColor,
                    CellDoorState.Connected => ConnectedDoorColor,
                    _                       => ClosedDoorColor,
                };
                TintRenderer(door.GetComponent<Renderer>(), color);
            }
        }

        static void TintRenderer(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            Material mat = new(renderer.sharedMaterial);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);

            renderer.sharedMaterial = mat;
        }
    }
}
