using System;
using UnityEngine;

namespace THPerfection.LevelGen
{
    [Serializable]
    public struct RoomPrefabMapping
    {
        public string TemplateId;
        public GameObject Prefab;
    }
}
