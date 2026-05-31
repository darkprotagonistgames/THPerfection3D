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

        [SerializeField] private EcsEventConfig _config;
        private Vector2 _scroll;

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
            EditorGUILayout.LabelField("ECS Event Definitions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These definitions generate ECS event components, extension methods, and frame-delayed lifecycle systems.\n" +
                                    "Sender (Entity) is always included automatically.", MessageType.Info);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUI.BeginChangeCheck();

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
                    param.Name = EditorGUILayout.TextField(param.Name);
                    param.Kind = (EcsEventParameterKind)EditorGUILayout.EnumPopup(param.Kind, GUILayout.Width(120));
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

        private static string SanitizeIdentifier(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "UnnamedEvent";

            var sb = new StringBuilder(raw.Length);
            // First char must be letter or underscore
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

            // Avoid C# keywords and literals
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

        private static string GetParameterType(EcsEventParameterKind kind)
        {
            switch (kind)
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

        private void GenerateCode()
        {
            var events = _config.Events;
            if (events == null || events.Count == 0)
            {
                EditorUtility.DisplayDialog("ECS Events", "No events defined to generate.", "OK");
                return;
            }

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

            // Group everything under a shared namespace while still allowing per-event inner namespaces if desired.
            sb.AppendLine("namespace THPerfection.GeneratedEvents");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Marker interface so all generated event components are easy to find.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public interface IEcsFrameEvent : IComponentData {}");
            sb.AppendLine();

            var eventNames = new List<string>();

            // First, generate top-level event structs so they are visible everywhere in this namespace.
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
                    var pType = GetParameterType(param.Kind);
                    sb.AppendLine($"        public {pType} {pName};");
                }

                sb.AppendLine("    }");
                sb.AppendLine();
            }

            // Extension methods container
            sb.AppendLine("    public static class EcsEventExtensions");
            sb.AppendLine("    {");

            foreach (var def in events)
            {
                var eventName = SanitizeIdentifier(def.EventName);
                if (!eventName.EndsWith("Event"))
                    eventName += "Event";

                // Extension method to create the event
                sb.Append($"        public static Entity Create{eventName}(this Entity sender, EntityCommandBuffer ecb");
                foreach (var param in def.Parameters)
                {
                    var pName = SanitizeIdentifier(param.Name);
                    var pType = GetParameterType(param.Kind);
                    sb.Append($", {pType} {char.ToLowerInvariant(pName[0])}{pName.Substring(1)}");
                }
                sb.AppendLine(")");
                sb.AppendLine("        {");
                sb.AppendLine("            var entity = ecb.CreateEntity();");
                sb.AppendLine($"            var ev = new {eventName}");
                sb.AppendLine("            {");
                sb.AppendLine("                Sender = sender,");
                sb.AppendLine("                Enabled = false,");

                foreach (var param in def.Parameters)
                {
                    var pName = SanitizeIdentifier(param.Name);
                    var fieldName = pName;
                    var argName = char.ToLowerInvariant(pName[0]) + pName.Substring(1);
                    sb.AppendLine($"                {fieldName} = {argName},");
                }

                sb.AppendLine("            };");
                sb.AppendLine("            ecb.AddComponent(entity, ev);");
                sb.AppendLine("            return entity;");
                sb.AppendLine("        }");
                sb.AppendLine();

                // ParallelWriter overload
                sb.Append($"        public static Entity Create{eventName}(this Entity sender, EntityCommandBuffer.ParallelWriter ecb, int sortKey");
                foreach (var param in def.Parameters)
                {
                    var pName = SanitizeIdentifier(param.Name);
                    var pType = GetParameterType(param.Kind);
                    sb.Append($", {pType} {char.ToLowerInvariant(pName[0])}{pName.Substring(1)}");
                }
                sb.AppendLine(")");
                sb.AppendLine("        {");
                sb.AppendLine("            var entity = ecb.CreateEntity(sortKey);");
                sb.AppendLine($"            var ev = new {eventName}");
                sb.AppendLine("            {");
                sb.AppendLine("                Sender = sender,");
                sb.AppendLine("                Enabled = false,");

                foreach (var param in def.Parameters)
                {
                    var pName = SanitizeIdentifier(param.Name);
                    var fieldName = pName;
                    var argName = char.ToLowerInvariant(pName[0]) + pName.Substring(1);
                    sb.AppendLine($"                {fieldName} = {argName},");
                }

                sb.AppendLine("            };");
                sb.AppendLine("            ecb.AddComponent(sortKey, entity, ev);");
                sb.AppendLine("            return entity;");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine();

            // Single enable system for all events
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

            // Single cleanup system for all events
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
    }
}

