using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessTransform : MonoBehaviour
{
    public Material material;   // 把上面的 Shader 拖到此处

    [Header("实时参数")]
    public Vector2 offset = Vector2.zero;
    public Vector2 scale  = Vector2.one;
    public bool flipX;
    public bool flipY;

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (material != null)
        {
            material.SetVector("_Offset", new Vector4(offset.x, offset.y, 0, 0));
            material.SetVector("_Scale",  new Vector4(scale.x,  scale.y,  0, 0));
            material.SetFloat("_FlipX", flipX ? 1 : 0);
            material.SetFloat("_FlipY", flipY ? 1 : 0);

            Graphics.Blit(src, dst, material);
        }
        else
        {
            Graphics.Blit(src, dst);
        }
    }
}
