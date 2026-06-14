using System.Collections.Generic;

namespace THPerfection.LevelGen
{
    public sealed class DoorwayFrontier
    {
        readonly List<DoorwaySlot> _open = new();
        readonly HashSet<DoorEdgeKey> _dead = new();

        public int OpenCount => _open.Count;

        public IReadOnlyList<DoorwaySlot> OpenSlots => _open;

        public void Clear()
        {
            _open.Clear();
            _dead.Clear();
        }

        public void Enqueue(in DoorwaySlot slot)
        {
            var key = new DoorEdgeKey(slot);
            if (_dead.Contains(key))
                return;

            for (int i = 0; i < _open.Count; i++)
            {
                if (Matches(_open[i], slot))
                    return;
            }

            _open.Add(slot);
        }

        public bool TryPick(ref Unity.Mathematics.Random rng, out DoorwaySlot slot)
        {
            if (_open.Count == 0)
            {
                slot = default;
                return false;
            }

            int index = rng.NextInt(0, _open.Count);
            slot = _open[index];
            return true;
        }

        public void Remove(in DoorwaySlot slot)
        {
            for (int i = _open.Count - 1; i >= 0; i--)
            {
                if (Matches(_open[i], slot))
                    _open.RemoveAt(i);
            }
        }

        public void MarkDead(in DoorwaySlot slot)
        {
            _dead.Add(new DoorEdgeKey(slot));
            Remove(slot);
        }

        public bool IsDead(in DoorEdgeKey key) => _dead.Contains(key);

        static bool Matches(in DoorwaySlot a, in DoorwaySlot b) =>
            a.Floor == b.Floor
            && a.Cell.Equals(b.Cell)
            && a.Side == b.Side;
    }
}
