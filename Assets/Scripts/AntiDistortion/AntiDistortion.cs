using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiDistortion : PostEffectsBase
{
    public Material material;

    public Vector4 m_KR = Vector4.one;
    public Vector4 m_KG = Vector4.one;
    public Vector4 m_KB = Vector4.one;
    void OnRenderImage(RenderTexture src, RenderTexture dst) {
        if (material != null)
        {
            /*
            float screenWidth = src.width;
            float screenHeight = src.height;
            Debug.Log("Screen Width = " + screenWidth + " Screen Height = " + screenHeight);
            material.SetFloat("_Screen_Width", screenWidth);
            material.SetFloat("_Screen_Height", screenHeight);
            */
            Debug.Log("Test: K_R = " + m_KR);
            Debug.Log("Test: K_G = " + m_KG);
            Debug.Log("Test: K_B = " + m_KB);
            material.SetVector("_K_R", m_KR);   
            material.SetVector("_K_G", m_KG);
            material.SetVector("_K_B", m_KB);
            Graphics.Blit(src, dst, material);
        }
        else {
            Graphics.Blit(src, dst);
        }
    }
}
