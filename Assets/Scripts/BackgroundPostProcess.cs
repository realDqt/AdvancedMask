using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Quarter
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public class BackgroundPostProcess : MonoBehaviour
{

    private Material overlayMaterial;
    public Quarter    targetQuarter = Quarter.BottomLeft;
    public Texture2D backgroundImng;

    void OnValidate()
    {
        if (overlayMaterial == null) return;
        UpdateQuarter(targetQuarter);
    }
    
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

        overlayMaterial = new Material(shader);
        overlayMaterial.SetTexture("_BackgroundTex", backgroundImng);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (overlayMaterial != null)
            Graphics.Blit(src, dst, overlayMaterial);
        else
            Graphics.Blit(src, dst);
    }

    void OnDestroy()
    {
        if (overlayMaterial != null) DestroyImmediate(overlayMaterial);
    }
    
    // 把枚举转成四个边界值
    public void UpdateQuarter(Quarter q)
    {
        if (overlayMaterial == null) return;

        float l = 0, r = 0.5f, top = 0, bottom = 0.5f;

        switch (q)
        {
            case Quarter.TopLeft:     l = 0;   r = 0.5f; top = 0;   bottom = 0.5f; break;
            case Quarter.TopRight:    l = 0.5f; r = 1;   top = 0;   bottom = 0.5f; break;
            case Quarter.BottomLeft:  l = 0;   r = 0.5f; top = 0.5f; bottom = 1;   break;
            case Quarter.BottomRight: l = 0.5f; r = 1;   top = 0.5f; bottom = 1;   break;
        }

        // 翻转上下
        overlayMaterial.SetFloat("_Top",    1.0f - bottom);
        overlayMaterial.SetFloat("_Bottom", 1.0f - top);
        overlayMaterial.SetFloat("_Left",   l);
        overlayMaterial.SetFloat("_Right",  r);
    }
}
