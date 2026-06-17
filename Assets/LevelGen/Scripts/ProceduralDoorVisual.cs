using Unity.Mathematics;
using UnityEngine;

namespace THPerfection.LevelGen
{
    /// <summary>
    /// Procedural fallback door cube tinted by logical state and visual phase.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProceduralDoorVisual : MonoBehaviour
    {
        public int2 WorldCell;
        public DoorSide Side;
        public FloorId Floor = FloorId.Main;

        [SerializeField] Renderer _renderer;
        [SerializeField] Color _openColor = new(1f, 0.85f, 0.1f, 1f);
        [SerializeField] Color _connectedColor = new(0.2f, 0.9f, 0.35f, 1f);
        [SerializeField] Color _closedColor = new(0.85f, 0.25f, 0.2f, 1f);

        public void ConfigureColors(Color open, Color connected, Color closed)
        {
            _openColor = open;
            _connectedColor = connected;
            _closedColor = closed;
        }

        public void ApplyFromInstance(RoomInstance instance, RoomDoorVisualPhase phase)
        {
            var key = new DoorEdgeKey(Floor, WorldCell, Side);
            if (!instance.TryGetDoorState(key, out CellDoorState logical))
            {
                Tint(_closedColor);
                return;
            }

            bool showOpen = RoomDoorVisualRules.ShouldShowOpen(logical, phase);
            Color color = logical switch
            {
                CellDoorState.Connected => _connectedColor,
                CellDoorState.Open when showOpen => _openColor,
                _ => _closedColor,
            };
            Tint(color);
        }

        void Reset()
        {
            _renderer = GetComponent<Renderer>();
        }

        void Tint(Color color)
        {
            if (_renderer == null)
                _renderer = GetComponent<Renderer>();

            if (_renderer == null)
                return;

            Material mat = new(_renderer.sharedMaterial);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);

            _renderer.sharedMaterial = mat;
        }
    }
}
