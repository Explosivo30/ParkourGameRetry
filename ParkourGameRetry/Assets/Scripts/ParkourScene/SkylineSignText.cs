using UnityEngine;

/// <summary>Mantiene el atlas dinámico de texto en el material con prueba de profundidad.</summary>
[ExecuteAlways, RequireComponent(typeof(TextMesh))]
public class SkylineSignText : MonoBehaviour
{
    void OnEnable()
    {
        Font.textureRebuilt += RefreshAtlas;
        RefreshAtlas(GetComponent<TextMesh>().font);
    }
    void OnDisable() => Font.textureRebuilt -= RefreshAtlas;
    void RefreshAtlas(Font font)
    {
        var text = GetComponent<TextMesh>();
        var renderer = GetComponent<MeshRenderer>();
        if (font != null && text.font == font && renderer.sharedMaterial != null)
            renderer.sharedMaterial.mainTexture = font.material.mainTexture;
    }
}