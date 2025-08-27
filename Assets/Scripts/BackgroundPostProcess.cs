using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundPostProcess : MonoBehaviour
{

    private Material material;
    public Texture2D backgroundImng;

    void Awake()
    {
        if (backgroundImng == null)
        {
            Debug.LogError("backgroundImng is null");
            enabled = false;
            return;
        }

        Shader shader = Shader.Find("Hidden/OverlayBackground");
        if (shader == null)
        {
            Debug.LogError("Failed to find OverlayBackground Shader");
            enabled = false;
            return;
        }

        material = new Material(shader);
        material.SetTexture("_BackgroundTex", backgroundImng);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (material != null)
            Graphics.Blit(src, dst, material);
        else
            Graphics.Blit(src, dst);
    }

    void OnDestroy()
    {
        if (material != null) DestroyImmediate(material);
    }
}
