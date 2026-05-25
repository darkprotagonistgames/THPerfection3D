using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class VoxelRiggingTool : EditorWindow
{
    private GameObject sourceModel;
    private List<PartConfig> partConfigs = new List<PartConfig>();
    private Vector2 scrollPos;
    private string newPrefabName = "NewRiggedModel";

    [System.Serializable]
    public class PartConfig
    {
        public string name;
        public GameObject originalGO;
        public string parentName = "Root";
        public PivotType pivotType = PivotType.Center;
        public Vector3 manualOffset;
    }

    public enum PivotType
    {
        Center,
        Bottom,
        Top,
        Left,
        Right,
        Front,
        Back,
        ClosestToParent,
        Manual
    }

    [MenuItem("Assets/Voxel/Rig Model Tool")]
    public static void OpenFromProject()
    {
        VoxelRiggingTool window = GetWindow<VoxelRiggingTool>("Voxel Rigging");
        if (Selection.activeGameObject != null)
        {
            window.sourceModel = Selection.activeGameObject;
            window.ScanModel();
        }
    }

    [MenuItem("GameObject/Voxel/Rig Model Tool", false, 10)]
    public static void OpenFromHierarchy()
    {
        OpenFromProject();
    }

    [System.Serializable]
    public class RigConfigData
    {
        public List<PartConfigData> parts = new List<PartConfigData>();
    }

    [System.Serializable]
    public class PartConfigData
    {
        public string name;
        public string parentName;
        public PivotType pivotType;
        public Vector3 manualOffset;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        
        GUILayout.Label("Voxel Rigging Tool", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Import Config")) ImportConfig();
        if (GUILayout.Button("Export Config")) ExportConfig();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
sourceModel = (GameObject)EditorGUILayout.ObjectField("Source Model", sourceModel, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck() && sourceModel != null)
        {
            ScanModel();
        }

        if (sourceModel == null)
        {
            EditorGUILayout.HelpBox("Select a Voxel model (GameObject with children) to begin.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        newPrefabName = EditorGUILayout.TextField("New Prefab Name", newPrefabName);

        EditorGUILayout.Space();
        GUILayout.Label("Hierarchy Configuration", EditorStyles.boldLabel);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        string[] parentOptions = new string[] { "Root" }.Concat(partConfigs.Select(p => p.name)).ToArray();

        for (int i = 0; i < partConfigs.Count; i++)
        {
            var config = partConfigs[i];
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField(config.name, EditorStyles.boldLabel);
            
            int currentIndex = System.Array.IndexOf(parentOptions, config.parentName);
            if (currentIndex == -1) currentIndex = 0;
            
            int newIndex = EditorGUILayout.Popup("Parent Joint", currentIndex, parentOptions);
            config.parentName = parentOptions[newIndex];

            config.pivotType = (PivotType)EditorGUILayout.EnumPopup("Pivot Position", config.pivotType);
            if (config.pivotType == PivotType.Manual)
            {
                config.manualOffset = EditorGUILayout.Vector3Field("Offset (Rel to Center)", config.manualOffset);
            }

            EditorGUILayout.EndVertical();
}
        
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Generate Rigged Prefab", GUILayout.Height(30)))
        {
            GenerateRig();
        }

        EditorGUILayout.EndVertical();
    }

    private void ExportConfig()
    {
        string path = EditorUtility.SaveFilePanel("Export Rig Config", "", "RigConfig.json", "json");
        if (string.IsNullOrEmpty(path)) return;

        RigConfigData data = new RigConfigData();
        foreach (var config in partConfigs)
        {
            data.parts.Add(new PartConfigData
            {
                name = config.name,
                parentName = config.parentName,
                pivotType = config.pivotType,
                manualOffset = config.manualOffset
            });
        }

        string json = JsonUtility.ToJson(data, true);
        System.IO.File.WriteAllText(path, json);
        Debug.Log("Exported rig configuration to: " + path);
    }

    private void ImportConfig()
    {
        string path = EditorUtility.OpenFilePanel("Import Rig Config", "", "json");
        if (string.IsNullOrEmpty(path)) return;

        string json = System.IO.File.ReadAllText(path);
        RigConfigData data = JsonUtility.FromJson<RigConfigData>(json);

        if (data == null || data.parts == null)
        {
            EditorUtility.DisplayDialog("Import Error", "The file is not a valid rig configuration.", "OK");
            return;
        }

        List<string> missingParts = new List<string>();
        int appliedCount = 0;

        foreach (var importedPart in data.parts)
        {
            var existingConfig = partConfigs.FirstOrDefault(p => p.name == importedPart.name);
            if (existingConfig != null)
            {
                existingConfig.parentName = importedPart.parentName;
                existingConfig.pivotType = importedPart.pivotType;
                existingConfig.manualOffset = importedPart.manualOffset;
                appliedCount++;
            }
            else
            {
                missingParts.Add(importedPart.name);
            }
        }

        if (missingParts.Count > 0)
        {
            string message = $"Applied {appliedCount} configurations.\n\nThe following parts from the config were not found in the current model:\n" + string.Join(", ", missingParts);
            EditorUtility.DisplayDialog("Import Partial Success", message, "OK");
        }
        else
        {
            Debug.Log("Successfully imported rig configuration.");
        }
    }

    private void ScanModel()
{
        partConfigs.Clear();
        if (sourceModel == null) return;

        foreach (Transform child in sourceModel.transform)
        {
            // Only add objects that have a MeshRenderer (the parts)
            if (child.GetComponent<MeshRenderer>() != null)
            {
                partConfigs.Add(new PartConfig
                {
                    name = child.name,
                    originalGO = child.gameObject,
                    pivotType = PivotType.Manual
                });
}
        }
    }

    private Vector3 GetClosestPointToParent(GameObject child, GameObject parent)
    {
        MeshFilter childMf = child.GetComponent<MeshFilter>();
        MeshRenderer parentRenderer = parent.GetComponent<MeshRenderer>();

        if (childMf == null || childMf.sharedMesh == null || parentRenderer == null)
            return child.transform.position;

        Vector3[] vertices = childMf.sharedMesh.vertices;
        Vector3 parentCenter = parentRenderer.bounds.center;
        
        float minDistance = float.MaxValue;
        Vector3 bestWorldPoint = child.transform.position;

        // For performance and to find the "center of contact", 
        // find the closest vertex to the parent's center.
        foreach (Vector3 v in vertices)
        {
            Vector3 worldV = child.transform.TransformPoint(v);
            float dist = Vector3.Distance(worldV, parentCenter);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestWorldPoint = worldV;
            }
        }

        return bestWorldPoint;
    }

    private void GenerateRig()
    {
        if (sourceModel == null) return;

        GameObject root = new GameObject(newPrefabName);
        Dictionary<string, GameObject> joints = new Dictionary<string, GameObject>();

        // 1. Create all joints
        foreach (var config in partConfigs)
        {
            GameObject joint = new GameObject(config.name + "_Joint");
            
            // Calculate Pivot Position
            MeshFilter mf = config.originalGO.GetComponent<MeshFilter>();
            Vector3 worldPivotPos = config.originalGO.transform.position; // Default to mesh center
            
            if (mf != null && mf.sharedMesh != null)
            {
                if (config.pivotType == PivotType.ClosestToParent && config.parentName != "Root")
                {
                    GameObject parentGO = partConfigs.FirstOrDefault(p => p.name == config.parentName)?.originalGO;
                    if (parentGO != null)
                    {
                        worldPivotPos = GetClosestPointToParent(config.originalGO, parentGO);
                    }
                }
                else
                {
                    Bounds b = mf.sharedMesh.bounds;
                    Vector3 localOffset = Vector3.zero;
                    
                    switch (config.pivotType)
                    {
                        case PivotType.Bottom: localOffset = Vector3.down * b.extents.y; break;
                        case PivotType.Top: localOffset = Vector3.up * b.extents.y; break;
                        case PivotType.Left: localOffset = Vector3.left * b.extents.x; break;
                        case PivotType.Right: localOffset = Vector3.right * b.extents.x; break;
                        case PivotType.Front: localOffset = Vector3.forward * b.extents.z; break;
                        case PivotType.Back: localOffset = Vector3.back * b.extents.z; break;
                        case PivotType.Manual: localOffset = config.manualOffset; break;
                    }
                    
                    worldPivotPos = config.originalGO.transform.TransformPoint(localOffset);
}
            }

            joint.transform.position = worldPivotPos;
            joints.Add(config.name, joint);
            
            GameObject meshClone = Instantiate(config.originalGO);
            meshClone.name = config.name + "_Mesh";
            meshClone.transform.SetParent(joint.transform, true);
        }

        // 3. Set up hierarchy
        foreach (var config in partConfigs)
        {
            GameObject joint = joints[config.name];
            joint.AddComponent<VoxelJoint>(); // Add the pivot editing component
            
            if (config.parentName == "Root" || !joints.ContainsKey(config.parentName))
            {
                joint.transform.SetParent(root.transform, true);
            }
            else
            {
                joint.transform.SetParent(joints[config.parentName].transform, true);
            }
        }

        // 4. Clean up the cloned meshes (remove their children if they have any, though they shouldn't)
        // And ensure they don't have scripts from the original if any were there.

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        Debug.Log("Generated rigged model: " + newPrefabName);
    }
}
