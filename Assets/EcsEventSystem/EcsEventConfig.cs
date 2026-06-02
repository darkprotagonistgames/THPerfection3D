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
        Float4,
        Enum
    }

    [Serializable]
    public class EcsEnumValueDefinition
    {
        public string Name = "Value0";

        [Tooltip("When this value is passed to an event parameter, the generated Create method also adds a matching tag component on the event entity.")]
        public bool AddsTag;
    }

    [Serializable]
    public class EcsEnumDefinition
    {
        public string EnumName = "NewEnum";
        public List<EcsEnumValueDefinition> Values = new List<EcsEnumValueDefinition>();
    }

    [Serializable]
    public class EcsEventParameter
    {
        public string Name;
        public EcsEventParameterKind Kind;

        [Tooltip("Name of the configured enum definition to use when Kind is Enum.")]
        public string EnumDefinitionName;
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

        public List<EcsEnumDefinition> Enums = new List<EcsEnumDefinition>();
        public List<EventDefinition> Events = new List<EventDefinition>();
    }
}
