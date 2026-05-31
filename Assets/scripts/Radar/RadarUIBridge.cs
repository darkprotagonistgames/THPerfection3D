using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Assigns the ECS-generated radar texture to a <see cref="RawImage"/> each frame.
/// Attach this to the same GameObject as the <see cref="RawImage"/> in the UI Canvas.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class RadarUIBridge : MonoBehaviour
{
    private RawImage _rawImage;

    private void Start()
    {
        _rawImage = GetComponent<RawImage>();
    }

    /// <summary>
    /// Assigns <see cref="RadarTextureHolder.Texture"/> once it becomes available,
    /// and keeps it current if it is ever replaced.
    /// </summary>
    private void Update()
    {
        Texture2D tex = RadarTextureHolder.Texture;
        if (tex != null && _rawImage.texture != tex)
            _rawImage.texture = tex;
    }
}
