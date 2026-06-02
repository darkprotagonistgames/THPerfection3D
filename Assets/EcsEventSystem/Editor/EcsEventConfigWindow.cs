using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace THPerfection.EcsEvents.Editor
{
    public class EcsEventConfigWindow : EditorWindow
    {
        private const string DefaultAssetPath = "Assets/EcsEventSystem/EcsEventConfig.asset";
        private const string DefaultGeneratedPath = "Assets/EcsEventSystem/Generated/EcsEvents.generated.cs";

        private enum ConfigTab
        {
            Enums,
            Events
        }

        [SerializeField] private EcsEventConfig _config;
        private Vector2 _scroll;
        private ConfigTab _activeTab = ConfigTab.Enums;

        [MenuItem("THPerfection/ECS Event System")]
        public static void ShowWindow()
        {
            var window = GetWindow<EcsEventConfigWindow>("ECS Events");
            window.minSize = new Vector2(500, 300);
            window.Show();
        }

        private void OnEnable()
        {
            _config = AssetDatabase.LoadAssetAtPath<EcsEventConfig>(DefaultAssetPath);
            if (_config == null)
            {
                var dir = Path.GetDirectoryName(DefaultAssetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                _config = CreateInstance<EcsEventConfig>();
                AssetDatabase.CreateAsset(_config, DefaultAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                EditorGUILayout.HelpBox("Failed to load or create ECS Event Config asset.", MessageType.Error);
                if (GUILayout.Button("Retry"))
                {
                    OnEnable();
                }
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ECS Event System", EditorStyles.boldLabel);

            _activeTab = (ConfigTab)GUILayout.Toolbar((int)_activeTab, new[] { "Enums", "Events" });
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUI.BeginChangeCheck();

            if (_activeTab == ConfigTab.Enums)
                DrawEnumsSection();
            else
                DrawEventsSection();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssetIfDirty(_config);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Generate Code", GUILayout.Width(150)))
            {
                GenerateCode();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEnumsSection()
        {
            EditorGUILayout.HelpBox(
                "Define project enums here. Use them as event parameter types on the Events tab.\n" +
                "Values marked as Tag add a matching IComponentData tag on the event entity when that value is passed to Create* methods.",
                MessageType.Info);

            var enums = _config.Enums;
            int removeEnumIndex = -1;

            for (int i = 0; i < enums.Count; i++)
            {
                var enumDef = enums[i];
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                enumDef.EnumName = EditorGUILayout.TextField("Enum Name", enumDef.EnumName);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    removeEnumIndex = i;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Values", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                int removeValueIndex = -1;
                for (int v = 0; v < enumDef.Values.Count; v++)
                {
                    var valueDef = enumDef.Values[v];
                    EditorGUILayout.BeginHorizontal();
                    valueDef.Name = EditorGUILayout.TextField(valueDef.Name);
                    valueDef.AddsTag = EditorGUILayout.ToggleLeft("Tag", valueDef.AddsTag, GUILayout.Width(50));
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        removeValueIndex = v;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (removeValueIndex >= 0)
                {
                    enumDef.Values.RemoveAt(removeValueIndex);
                }

                if (GUILayout.Button("Add Value"))
                {
                    enumDef.Values.Add(new EcsEnumValueDefinition
                    {
                        Name = "Value" + enumDef.Values.Count
                    });
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            if (removeEnumIndex >= 0)
            {
                enums.RemoveAt(removeEnumIndex);
            }

            if (GUILayout.Button("Add Enum"))
            {
                enums.Add(new EcsEnumDefinition());
            }
        }

        private void DrawEventsSection()
        {
            EditorGUILayout.HelpBox(
                "Event definitions generate ECS event components, extension methods, and frame-delayed lifecycle systems.\n" +
                "Sender (Entity) is always included automatically.",
                MessageType.Info);

            var events = _config.Events;
            int removeIndex = -1;

            for (int i = 0; i < events.Count; i++)
            {
                var def = events[i];
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                def.EventName = EditorGUILayout.TextField("Event Name", def.EventName);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    removeIndex = i;
                }
                EditorGUILayout.EndHorizontal();

                def.Namespace = EditorGUILayout.TextField("Namespace", def.Namespace);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                int removeParamIndex = -1;
                for (int p = 0; p < def.Parameters.Count; p++)
                {
                    var param = def.Parameters[p];
                    EditorGUILayout.BeginHorizontal();
                    param.Name = EditorGUILayout.TextField(param.Name, GUILayout.MinWidth(80));
                    param.Kind = (EcsEventParameterKind)EditorGUILayout.EnumPopup(param.Kind, GUILayout.Width(100));

                    if (param.Kind == EcsEventParameterKind.Enum)
                    {
                        DrawEnumDefinitionPopup(param);
                    }

                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        removeParamIndex = p;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (removeParamIndex >= 0)
                {
                    def.Parameters.RemoveAt(removeParamIndex);
                }

                if (GUILayout.Button("Add Parameter"))
                {
                    def.Parameters.Add(new EcsEventParameter
                    {
                        Name = "Value",
                        Kind = EcsEventParameterKind.Float
                    });
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
            {
                events.RemoveAt(removeIndex);
            }

            if (GUILayout.Button("Add Event"))
            {
                events.Add(new EcsEventConfig.EventDefinition());
            }
        }

        private void DrawEnumDefinitionPopup(EcsEventParameter param)
        {
            var enumNames = GetEnumDefinitionNames();
            if (enumNames.Count == 0)
            {
                EditorGUILayout.LabelField("(no enums defined)", GUILayout.Width(120));
                return;
            }

            int currentIndex = 0;
            if (!string.IsNullOrEmpty(param.EnumDefinitionName))
            {
                currentIndex = enumNames.IndexOf(param.EnumDefinitionName);
                if (currentIndex < 0)
                    currentIndex = 0;
            }

            int selectedIndex = EditorGUILayout.Popup(currentIndex, enumNames.ToArray(), GUILayout.Width(120));
            param.EnumDefinitionName = enumNames[selectedIndex];
        }

        private List<string> GetEnumDefinitionNames()
        {
            var names = new List<string>();
            if (_config.Enums == null)
                return names;

            foreach (var enumDef in _config.Enums)
            {
                if (!string.IsNullOrWhiteSpace(enumDef.EnumName))
                    names.Add(enumDef.EnumName);
            }

            return names;
        }

        private EcsEnumDefinition FindEnumDefinition(string enumName)
        {
            if (_config.Enums == null || string.IsNullOrWhiteSpace(enumName))
                return null;

            foreach (var enumDef in _config.Enums)
            {
                if (enumDef.EnumName == enumName)
                    return enumDef;
            }

            return null;
        }

        private static string SanitizeIdentifier(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "Unnamed";

            var sb = new StringBuilder(raw.Length);
            char c = raw[0];
            if (!char.IsLetter(c) && c != '_')
            {
                sb.Append('_');
            }
            else
            {
                sb.Append(c);
            }

            for (int i = 1; i < raw.Length; i++)
            {
                c = raw[i];
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_');
            }

            var candidate = sb.ToString();

            switch (candidate)
            {
                case "class":
                case "struct":
                case "namespace":
                case "event":
                case "internal":
                case "public":
                case "private":
                case "protected":
                case "static":
                case "void":
                case "int":
                case "float":
                case "double":
                case "bool":
                case "string":
                case "true":
                case "false":
                case "null":
                    candidate = "_" + candidate;
                    break;
            }

            return candidate;
        }

        private string GetParameterType(EcsEventParameter param)
        {
            if (param.Kind == EcsEventParameterKind.Enum)
            {
                var enumDef = FindEnumDefinition(param.EnumDefinitionName);
                if (enumDef == null)
                    return "int";

                return SanitizeIdentifier(enumDef.EnumName);
            }

            switch (param.Kind)
            {
                case EcsEventParameterKind.Entity: return "Unity.Entities.Entity";
                case EcsEventParameterKind.Bool: return "bool";
                case EcsEventParameterKind.Int: return "int";
                case EcsEventParameterKind.Float: return "float";
                case EcsEventParameterKind.Double: return "double";
                case EcsEventParameterKind.Quaternion: return "Unity.Mathematics.quaternion";
                case EcsEventParameterKind.Float2: return "Unity.Mathematics.float2";
                case EcsEventParameterKind.Float3: return "Unity.Mathematics.float3";
                case EcsEventParameterKind.Float4: return "Unity.Mathematics.float4";
                default: return "float";
            }
        }

        private static string GetTagComponentName(string enumTypeName, string valueName)
        {
            return SanitizeIdentifier(enumTypeName) + SanitizeIdentifier(valueName) + "Tag";
        }

        private static string ToArgName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return "value";

            return char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1);
        }

        private bool ValidateConfig(out string errorMessage)
        {
            if (_config.Events == null || _config.Events.Count == 0)
            {
                errorMessage = "No events defined to generate.";
                return false;
            }

            var enumNames = new HashSet<string>();
            if (_config.Enums != null)
            {
                foreach (var enumDef in _config.Enums)
                {
                    var enumName = SanitizeIdentifier(enumDef.EnumName);
                    if (enumDef.Values == null || enumDef.Values.Count == 0)
                    {
                        errorMessage = $"Enum '{enumDef.EnumName}' has no values.";
                        return false;
                    }

                    if (!enumNames.Add(enumName))
                    {
                        errorMessage = $"Duplicate enum name '{enumDef.EnumName}'.";
                        return false;
                    }
                }
            }

            foreach (var eventDef in _config.Events)
            {
                foreach (var param in eventDef.Parameters)
                {
                    if (param.Kind != EcsEventParameterKind.Enum)
                        continue;

                    if (string.IsNullOrWhiteSpace(param.EnumDefinitionName))
                    {
                        errorMessage = $"Event '{eventDef.EventName}' parameter '{param.Name}' uses Enum kind but has no enum selected.";
                        return false;
                    }

                    if (FindEnumDefinition(param.EnumDefinitionName) == null)
                    {
                        errorMessage = $"Event '{eventDef.EventName}' parameter '{param.Name}' references unknown enum '{param.EnumDefinitionName}'.";
                        return false;
                    }
                }
            }

            errorMessage = null;
            return true;
        }

        private void GenerateCode()
        {
            if (!ValidateConfig(out var errorMessage))
            {
                EditorUtility.DisplayDialog("ECS Events", errorMessage, "OK");
                return;
            }

            var events = _config.Events;

            var sb = new StringBuilder();
            sb.AppendLine("// ----------------------------------------------");
            sb.AppendLine("// Auto-generated by THPerfection ECS Event System");
            sb.AppendLine("// DO NOT EDIT MANUALLY");
            sb.AppendLine("// ----------------------------------------------");
            sb.AppendLine("using Unity.Burst;");
            sb.AppendLine("using Unity.Collections;");
            sb.AppendLine("using Unity.Entities;");
            sb.AppendLine("using Unity.Mathematics;");
            sb.AppendLine();

            sb.AppendLine("namespace THPerfection.GeneratedEvents");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Marker interface so all generated event components are easy to find.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public interface IEcsFrameEvent : IComponentData {}");
            sb.AppendLine();

            GenerateEnums(sb);
            GenerateTagComponents(sb);

            var eventNames = new List<string>();

            foreach (var def in events)
            {
                var eventName = SanitizeIdentifier(def.EventName);
                if (!eventName.EndsWith("Event"))
                    eventName += "Event";

                eventNames.Add(eventName);

                sb.AppendLine($"    public struct {eventName} : IEcsFrameEvent");
                sb.AppendLine("    {");
                sb.AppendLine("        public Entity Sender;");
                sb.AppendLine("        public bool Enabled;");

                foreach (var param in def.Parameters)
                {
                    var pName = SanitizeIdentifier(param.Name);
                    var pType = GetParameterType(param);
                    sb.AppendLine($"        public {pType} {pName};");
                }

                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("    public static class EcsEventExtensions");
            sb.AppendLine("    {");

            foreach (var def in events)
            {
                var eventName = SanitizeIdentifier(def.EventName);
                if (!eventName.EndsWith("Event"))
                    eventName += "Event";

                GenerateCreateMethod(sb, def, eventName, parallelWriter: false);
                GenerateCreateMethod(sb, def, eventName, parallelWriter: true);
            }

            sb.AppendLine("    }");
            sb.AppendLine();

            GenerateEnableSystem(sb, eventNames);
            GenerateCleanupSystem(sb, eventNames);

            sb.AppendLine("}");

            var genPath = DefaultGeneratedPath;
            var genDir = Path.GetDirectoryName(genPath);
            if (!string.IsNullOrEmpty(genDir) && !Directory.Exists(genDir))
            {
                Directory.CreateDirectory(genDir);
            }

            File.WriteAllText(genPath, sb.ToString());
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("ECS Events", "Generated ECS event code:\n" + genPath, "OK");
        }

        private void GenerateEnums(StringBuilder sb)
        {
            if (_config.Enums == null || _config.Enums.Count == 0)
                return;

            foreach (var enumDef in _config.Enums)
            {
                if (enumDef.Values == null || enumDef.Values.Count == 0)
                    continue;

                var enumName = SanitizeIdentifier(enumDef.EnumName);
                sb.AppendLine($"    public enum {enumName}");
                sb.AppendLine("    {");

                for (int i = 0; i < enumDef.Values.Count; i++)
                {
                    var valueName = SanitizeIdentifier(enumDef.Values[i].Name);
                    sb.AppendLine($"        {valueName} = {i},");
                }

                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }

        private void GenerateTagComponents(StringBuilder sb)
        {
            if (_config.Enums == null || _config.Enums.Count == 0)
                return;

            var emittedTags = new HashSet<string>();

            foreach (var enumDef in _config.Enums)
            {
                if (enumDef.Values == null)
                    continue;

                var enumName = SanitizeIdentifier(enumDef.EnumName);
                foreach (var valueDef in enumDef.Values)
                {
                    if (!valueDef.AddsTag)
                        continue;

                    var tagName = GetTagComponentName(enumName, valueDef.Name);
                    if (!emittedTags.Add(tagName))
                        continue;

                    sb.AppendLine($"    public struct {tagName} : IComponentData {{}}");
                }
            }

            if (emittedTags.Count > 0)
                sb.AppendLine();
        }

        private void GenerateCreateMethod(StringBuilder sb, EcsEventConfig.EventDefinition def, string eventName, bool parallelWriter)
        {
            sb.Append($"        public static Entity Create{eventName}(this Entity sender, EntityCommandBuffer");
            if (parallelWriter)
                sb.Append(".ParallelWriter ecb, int sortKey");
            else
                sb.Append(" ecb");

            foreach (var param in def.Parameters)
            {
                var pName = SanitizeIdentifier(param.Name);
                var pType = GetParameterType(param);
                sb.Append($", {pType} {ToArgName(pName)}");
            }

            sb.AppendLine(")");
            sb.AppendLine("        {");

            if (parallelWriter)
                sb.AppendLine("            var entity = ecb.CreateEntity(sortKey);");
            else
                sb.AppendLine("            var entity = ecb.CreateEntity();");

            sb.AppendLine($"            var ev = new {eventName}");
            sb.AppendLine("            {");
            sb.AppendLine("                Sender = sender,");
            sb.AppendLine("                Enabled = false,");

            foreach (var param in def.Parameters)
            {
                var pName = SanitizeIdentifier(param.Name);
                sb.AppendLine($"                {pName} = {ToArgName(pName)},");
            }

            sb.AppendLine("            };");

            if (parallelWriter)
                sb.AppendLine("            ecb.AddComponent(sortKey, entity, ev);");
            else
                sb.AppendLine("            ecb.AddComponent(entity, ev);");

            GenerateTagSwitch(sb, def, parallelWriter);

            sb.AppendLine("            return entity;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private void GenerateTagSwitch(StringBuilder sb, EcsEventConfig.EventDefinition def, bool parallelWriter)
        {
            var enumParamsWithTags = new List<(EcsEventParameter Param, EcsEnumDefinition EnumDef)>();

            foreach (var param in def.Parameters)
            {
                if (param.Kind != EcsEventParameterKind.Enum)
                    continue;

                var enumDef = FindEnumDefinition(param.EnumDefinitionName);
                if (enumDef?.Values == null)
                    continue;

                var hasTaggedValues = false;
                foreach (var valueDef in enumDef.Values)
                {
                    if (valueDef.AddsTag)
                    {
                        hasTaggedValues = true;
                        break;
                    }
                }

                if (hasTaggedValues)
                    enumParamsWithTags.Add((param, enumDef));
            }

            if (enumParamsWithTags.Count == 0)
                return;

            foreach (var (param, enumDef) in enumParamsWithTags)
            {
                var enumTypeName = SanitizeIdentifier(enumDef.EnumName);
                var argName = ToArgName(SanitizeIdentifier(param.Name));

                sb.AppendLine($"            switch ({argName})");
                sb.AppendLine("            {");

                foreach (var valueDef in enumDef.Values)
                {
                    if (!valueDef.AddsTag)
                        continue;

                    var valueName = SanitizeIdentifier(valueDef.Name);
                    var tagName = GetTagComponentName(enumTypeName, valueDef.Name);

                    sb.AppendLine($"                case {enumTypeName}.{valueName}:");
                    if (parallelWriter)
                        sb.AppendLine($"                    ecb.AddComponent(sortKey, entity, new {tagName}());");
                    else
                        sb.AppendLine($"                    ecb.AddComponent(entity, new {tagName}());");
                    sb.AppendLine("                    break;");
                }

                sb.AppendLine("            }");
            }
        }

        private static void GenerateEnableSystem(StringBuilder sb, List<string> eventNames)
        {
            sb.AppendLine("    [BurstCompile]");
            sb.AppendLine("    [UpdateInGroup(typeof(SimulationSystemGroup))]");
            sb.AppendLine("    public partial struct EnableAllEcsEventsSystem : ISystem");
            sb.AppendLine("    {");
            sb.AppendLine("        [BurstCompile]");
            sb.AppendLine("        public void OnCreate(ref SystemState state) { }");
            sb.AppendLine();
            sb.AppendLine("        [BurstCompile]");
            sb.AppendLine("        public void OnDestroy(ref SystemState state) { }");
            sb.AppendLine();
            sb.AppendLine("        [BurstCompile]");
            sb.AppendLine("        public void OnUpdate(ref SystemState state)");
            sb.AppendLine("        {");

            foreach (var eventName in eventNames)
            {
                sb.AppendLine("            foreach (var ev in SystemAPI.Query<RefRW<" + eventName + ">>())");
                sb.AppendLine("            {");
                sb.AppendLine("                if (!ev.ValueRO.Enabled)");
                sb.AppendLine("                {");
                sb.AppendLine("                    ev.ValueRW.Enabled = true;");
                sb.AppendLine("                }");
                sb.AppendLine("            }");
                sb.AppendLine();
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        private static void GenerateCleanupSystem(StringBuilder sb, List<string> eventNames)
        {
            sb.AppendLine("    [BurstCompile]");
            sb.AppendLine("    [UpdateInGroup(typeof(SimulationSystemGroup))]");
            sb.AppendLine("    [UpdateAfter(typeof(EnableAllEcsEventsSystem))]");
            sb.AppendLine("    public partial struct CleanupAllEcsEventsSystem : ISystem");
            sb.AppendLine("    {");
            sb.AppendLine("        [BurstCompile]");
            sb.AppendLine("        public void OnCreate(ref SystemState state) { }");
            sb.AppendLine();
            sb.AppendLine("        [BurstCompile]");
            sb.AppendLine("        public void OnDestroy(ref SystemState state) { }");
            sb.AppendLine();
            sb.AppendLine("        [BurstCompile]");
            sb.AppendLine("        public void OnUpdate(ref SystemState state)");
            sb.AppendLine("        {");
            sb.AppendLine("            var ecb = new EntityCommandBuffer(Allocator.Temp);");

            foreach (var eventName in eventNames)
            {
                sb.AppendLine("            foreach (var (ev, entity) in SystemAPI.Query<RefRO<" + eventName + ">>().WithEntityAccess())");
                sb.AppendLine("            {");
                sb.AppendLine("                if (ev.ValueRO.Enabled)");
                sb.AppendLine("                {");
                sb.AppendLine("                    ecb.DestroyEntity(entity);");
                sb.AppendLine("                }");
                sb.AppendLine("            }");
                sb.AppendLine();
            }

            sb.AppendLine("            ecb.Playback(state.EntityManager);");
            sb.AppendLine("            ecb.Dispose();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }
}
