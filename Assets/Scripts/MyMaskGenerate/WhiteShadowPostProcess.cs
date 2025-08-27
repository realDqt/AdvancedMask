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



    void Awake()
    {
        _camera = GetComponent<Camera>();
    }
    void Start()
    {
        //_camera = GetComponent<Camera>();
        
        //ConstructShadowMask();
        
        _blitMaterial = new Material(Shader.Find("Hidden/WhiteShadowBlit"));
    }

    void ConstructMask(ref CommandBuffer cmdDrawMask,
                       ref RenderTexture maskTexture,
                       List<Renderer> renders,
                       Material material)
    {
        // 1. 移除旧 CB
        if (cmdDrawMask != null)
            _camera.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, cmdDrawMask);

        // 2. 重建 RT
        if (maskTexture != null)
            maskTexture.Release();
        maskTexture = new RenderTexture(maskWidth, maskHeight, 0,
            RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        maskTexture.Create();

        // 3. 重建 CB
        cmdDrawMask?.Dispose();  // 彻底释放
        cmdDrawMask = new CommandBuffer { name = "BuildMask" };
        cmdDrawMask.SetRenderTarget(maskTexture);
        cmdDrawMask.ClearRenderTarget(true, true, Color.black);

        foreach (Renderer r in renders)
        {
            for (int i = 0; i < r.sharedMaterials.Length; ++i)
                cmdDrawMask.DrawRenderer(r, material, i, 0);
        }

        // 4. 重新绑定
        _camera.AddCommandBuffer(CameraEvent.AfterDepthTexture, cmdDrawMask);
    }

    void ConstructShadowMask()
    {
        List<Renderer> allRenders = new List<Renderer>(FindObjectsOfType<Renderer>());
        List<Renderer> renders = new List<Renderer>();
        foreach (Renderer r in allRenders)
        {
            if (r.receiveShadows && r.sharedMaterial != null && r.sharedMaterial.renderQueue <= 2500)
            {
                renders.Add(r);
            }
        }
        
        ConstructMask(ref _cbDrawShadowMask, ref _shadowMask, renders, new Material(whiteReceiverShader));
    }
    
    void ConstructObjectMask()
    {
        List<Renderer> allRenders = new List<Renderer>(FindObjectsOfType<Renderer>());
        List<Renderer> renders = new List<Renderer>();
        foreach (Renderer r in allRenders)
        {
            if ((r.shadowCastingMode != ShadowCastingMode.Off) &&
                (r.sharedMaterial != null) &&
                (r.sharedMaterial.renderQueue <= 2500))
            {
                renders.Add(r);
            }
        }
        
        ConstructMask(ref _cbDrawObjectMask, ref _objectMask, renders, new Material(whiteCasterShader));
    }

    public void ConstructGivenObjectMask(GameObject go)
    {
        if (go == null)
        {
            Debug.LogError("Object to construct is null");
            return;
        }
        else
        {
            Debug.Log("Construct mask for " + go.name);
        }
        
        List<Renderer> renders = new List<Renderer>(go.GetComponentsInChildren<Renderer>());
        ConstructMask(ref _cbDrawObjectMask, ref _objectMask, renders, new Material(whiteCasterShader));
    }
    
    public void ConstructGivenShadowMask(GameObject go)
    {
        if (go == null)
        {
            Debug.LogError("Object to construct is null");
            return;
        }
        else
        {
            Debug.Log("Construct mask for " + go.name);
        }
        
        List<Renderer> renders = new List<Renderer>(go.GetComponentsInChildren<Renderer>());
        ConstructMask(ref _cbDrawShadowMask, ref _shadowMask, renders, new Material(whiteReceiverShader));
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
