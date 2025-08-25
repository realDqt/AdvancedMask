using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class WhiteShadowPostProcess : MonoBehaviour
{
    [SerializeField]
    public Shader whiteReceiverShader;
    
    [SerializeField]
    public Shader whiteCasterShader;

    public int maskWidth;
    public int maskHeight;
    
    private Material     _casterMat;
    private Material     _receiverMat; 
    
    private CommandBuffer _cbDrawShadowMask;
    private CommandBuffer _cbDrawObjectMask;
    
    private RenderTexture _shadowMask;
    private RenderTexture _objectMask;
    
    private static readonly int ShadowMaskID = Shader.PropertyToID("_ShadowMaskTex");
    private static readonly int ObjectMaskID = Shader.PropertyToID("_ObjectMaskTex");
    
    private Material      _blitMaterial;
    private Camera _camera;
    

    void Start()
    {
        _camera = GetComponent<Camera>();
        
        ConstructShadowMask();
        ConstructObjectMask();
        
        _blitMaterial = new Material(Shader.Find("Hidden/WhiteShadowBlit"));
    }

    void ConstructShadowMask()
    {
        // 1. ShadowMask RT
        _shadowMask = new RenderTexture(maskWidth, maskHeight, 0,
            RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        _shadowMask.Create();

        // 2. 用来画 ObjectMask 的 CommandBuffer
        _cbDrawShadowMask = new CommandBuffer { name = "BuildShadowMask" };
        _cbDrawShadowMask.SetRenderTarget(_shadowMask);
        _cbDrawShadowMask.ClearRenderTarget(true, true, Color.black);
        
        List<Renderer> renders = new List<Renderer>(FindObjectsOfType<Renderer>());
        
        
        // 3. 遍历可接受阴影物体，绘制阴影
        
        _receiverMat =  new Material(whiteReceiverShader);
        foreach (Renderer r in renders)
        {
            if (r.receiveShadows && r.sharedMaterial != null && r.sharedMaterial.renderQueue <= 2500)
            {
                for (int i = 0; i < r.sharedMaterials.Length; ++i)
                    _cbDrawShadowMask.DrawRenderer(r, _receiverMat, i, 0);
            }
        }
        

        // 4. 把 CommandBuffer 插到相机里
        _camera.AddCommandBuffer(CameraEvent.AfterDepthTexture, _cbDrawShadowMask);
    }
    
    void ConstructObjectMask()
    {
        // 1. ObjectMask RT
        _objectMask = new RenderTexture(maskWidth, maskHeight, 0,
            RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        _objectMask.Create();

        // 2. 用来画 ShadowMask 的 CommandBuffer
        _cbDrawObjectMask = new CommandBuffer { name = "BuildObjectMask" };
        _cbDrawObjectMask.SetRenderTarget(_objectMask);
        _cbDrawObjectMask.ClearRenderTarget(true, true, Color.black);
        
        
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
                    _cbDrawObjectMask.DrawRenderer(r, _casterMat, i, 0);
            }
        }
        

        // 4. 把 CommandBuffer 插到相机里
        _camera.AddCommandBuffer(CameraEvent.AfterDepthTexture, _cbDrawObjectMask);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        _blitMaterial.SetTexture(ShadowMaskID, _shadowMask);
        _blitMaterial.SetTexture(ObjectMaskID, _objectMask);
        Graphics.Blit(src, dst, _blitMaterial);
    }

    void OnDestroy()
    {
        if (_cbDrawShadowMask != null)
            GetComponent<Camera>().RemoveCommandBuffer(CameraEvent.AfterDepthTexture, _cbDrawShadowMask);
        if (_cbDrawObjectMask != null)
            GetComponent<Camera>().RemoveCommandBuffer(CameraEvent.AfterDepthTexture, _cbDrawObjectMask);

        if (_shadowMask != null) _shadowMask.Release();
        if (_objectMask != null) _objectMask.Release();
        if (_casterMat  != null) DestroyImmediate(_casterMat);
        if (_receiverMat != null) DestroyImmediate(_receiverMat);
        if (_blitMaterial != null) DestroyImmediate(_blitMaterial);
    }
}
