// Registers UnityEngine component types that Entities bakers reference internally
// but that are not auto-registered by the package in this Unity version.
using UnityEngine;
using Unity.Entities;

[assembly: RegisterUnityEngineComponentType(typeof(MeshFilter))]
[assembly: RegisterUnityEngineComponentType(typeof(MeshRenderer))]
[assembly: RegisterUnityEngineComponentType(typeof(Animator))]
