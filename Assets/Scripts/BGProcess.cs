using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGProcess : MonoBehaviour
{
    [Range(0f, 1f)]
    public float multiplier = 1f;
    public Texture2D backgroundImg;

    public int quadrantIndex = 0;

    private Material mat;

    void OnEnable()
    {
        // 运行时动态创建材质，避免修改原资源
        Shader shader = Shader.Find("Unlit/BGProcess");
        if (shader == null)
        {
            Debug.LogError("找不到 Unlit/BGProcess Shader，请确认路径/拼写正确！");
            enabled = false;
            return;
        }
        mat = new Material(shader);
    }
    
    
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (mat == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        float h = Screen.height;                 // 正方形边长
        float w = Screen.width;
        float offsetX = (w - h) * 0.5f;          // 水平居中
        float offsetY = 0;                       // 垂直已贴顶

        mat.SetFloat("_SquareSize", h);
        mat.SetVector("_Offset", new Vector2(offsetX, offsetY));
        mat.SetFloat("_Multiplier", multiplier);
        mat.SetTexture("_BackgroundTex", backgroundImg);
        mat.SetInt("_QuadrantIndex", quadrantIndex);

        Graphics.Blit(src, dest, mat);
    }

    void OnDisable()
    {
        if (mat != null)
            DestroyImmediate(mat);
    }
}
