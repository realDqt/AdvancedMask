using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class MaskGenerator : MonoBehaviour
{
    public Material maskPass;
    // public Camera mainCamera; 
    [Range(0.0f,1.0f)]
    public float thick = 1.0f;
    [Range(0.0f,1.0f)]
    public float depth = 0.0f;
    [Range(0.0f,20.0f)]
    public float bright = 0.4f;
    [Range(0.0f,1.0f)]
    public float alpha = 0.0f;
    [Range(0.0f,1.0f)]
    public float color = 0.0f;
    [Range(0.0f,1.0f)]
    public float transparency = 0.0f;
    [Range(0.0f,1.0f)]
    public float shadow = 0.0f;

    private TransparencyCapturer transparencyCapturer;
    
    public Texture mainTex;
    public Texture colorBufferTex;
    public Texture depthBufferTex;
    public Texture thicknessBufferTex;
    public Texture globalTransparencyTex;
    public Texture curveTex;

    // public RenderTexture[] delayrenderTextures;
    // public int frameCount = 10;
    private int currentFrame = 0;
    
    private Camera cam;
    
    void Start()
    {
        transparencyCapturer = GetComponent<TransparencyCapturer>();
        cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;
        // mainCamera.depthTextureMode |= DepthTextureMode.Depth;
    }
    
    void OnPreRender()
    {
        // not sure
        /*
        if (maskPass == null) return;

        maskPass.SetTexture("_MainTex",               mainTex             ?? Texture2D.whiteTexture);
        maskPass.SetTexture("colorBuffer",            colorBufferTex      ?? Texture2D.blackTexture);
        maskPass.SetTexture("depthBuffer",            depthBufferTex      ?? Texture2D.blackTexture);
        maskPass.SetTexture("thicknessBuffer",        thicknessBufferTex  ?? Texture2D.blackTexture);
        maskPass.SetTexture("_GlobalTransparencyTexture", globalTransparencyTex ?? Texture2D.blackTexture);
        maskPass.SetTexture("_CurveTex",              curveTex            ?? Texture2D.whiteTexture);
        */
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        transparencyCapturer?.CaptureTransparency();
        maskPass.SetFloat("_Thick", thick);
        maskPass.SetFloat("_Depth", depth);
        maskPass.SetFloat("_Bright", bright);
        maskPass.SetFloat("_Alpha", alpha);
        maskPass.SetFloat("_Color", color);
        maskPass.SetFloat("_Shadow", shadow);
        maskPass.SetFloat("_Transparency", transparency);
        
        Graphics.Blit(src, dest, maskPass, 0);
    }

}