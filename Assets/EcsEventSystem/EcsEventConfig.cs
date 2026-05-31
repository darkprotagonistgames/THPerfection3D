using System;
using System.Collections.Generic;
using UnityEngine;

namespace THPerfection.EcsEvents
{
    public enum EcsEventParameterKind
    {
        Entity,
        Bool,
        Int,
        Float,
        Double,
        Quaternion,
        Float2,
        Float3,
        Float4
    }

    [Serializable]
    public class EcsEventParameter
    {
        public string Name;
        public EcsEventParameterKind Kind;
    }

    [CreateAssetMenu(
        fileName = "EcsEventConfig",
        menuName = "THPerfection/ECS Event Config",
        order = 0)]
    public class EcsEventConfig : ScriptableObject
    {
        [Serializable]
        public class EventDefinition
        {
            public string EventName = "NewEvent";
            [Tooltip("Optional namespace to put the generated event struct into. Leave empty to use default.")]
            public string Namespace = "THPerfection.GeneratedEvents";

            [Tooltip("Parameters for this event. Sender is always an Entity and is generated automatically.")]
            public List<EcsEventParameter> Parameters = new List<EcsEventParameter>();
        }

        public List<EventDefinition> Events = new List<EventDefinition>();
    }
}

