using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class WhiteShadowPostProcess : MonoBehaviour
{
    [SerializeField]
    public Shader whiteCasterShader;
    private Material     _casterMat;        // 代替 overrideMaterial
    private CommandBuffer _cbDrawShadowMask;
    private RenderTexture _shadowMask;
    private Material      _blitMaterial;

    private static readonly int ShadowMaskID = Shader.PropertyToID("_ShadowMaskTex");

    void Start()
    {
        Camera cam = GetComponent<Camera>();

        // 1. ShadowMask RT
        _shadowMask = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0,
                                        RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        _shadowMask.Create();

        // 2. 用来画 ShadowMask 的 CommandBuffer
        _cbDrawShadowMask = new CommandBuffer { name = "BuildShadowMask" };
        _cbDrawShadowMask.SetRenderTarget(_shadowMask);
        _cbDrawShadowMask.ClearRenderTarget(true, true, Color.black);

        // 3. 遍历所有 Renderer，把能投射阴影的再画一次
        _casterMat = new Material(whiteCasterShader);
        List<Renderer> renders = new List<Renderer>(FindObjectsOfType<Renderer>());
        foreach (Renderer r in renders)
        {
            // 只处理不透明且投射阴影的
            if ((r.shadowCastingMode != ShadowCastingMode.Off) &&
                (r.sharedMaterial != null) &&
                (r.sharedMaterial.renderQueue <= 2500))
            {
                for (int i = 0; i < r.sharedMaterials.Length; ++i)
                    _cbDrawShadowMask.DrawRenderer(r, _casterMat, i, 0);
            }
        }

        // 4. 把 CommandBuffer 插到相机里
        cam.AddCommandBuffer(CameraEvent.AfterDepthTexture, _cbDrawShadowMask);

        // 5. 后处理 Blit
        _blitMaterial = new Material(Shader.Find("Hidden/WhiteShadowBlit"));
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        _blitMaterial.SetTexture(ShadowMaskID, _shadowMask);
        Graphics.Blit(src, dst, _blitMaterial);
    }

    void OnDestroy()
    {
        if (_cbDrawShadowMask != null)
            GetComponent<Camera>().RemoveCommandBuffer(CameraEvent.AfterDepthTexture, _cbDrawShadowMask);

        if (_shadowMask != null) _shadowMask.Release();
        if (_casterMat  != null) DestroyImmediate(_casterMat);
        if (_blitMaterial != null) DestroyImmediate(_blitMaterial);
    }
}
