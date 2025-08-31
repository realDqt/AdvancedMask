using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class ExperimentManager3 : MonoBehaviour
{
     [Header("Model Names (must match scene hierarchy)")]
    private string[] m_ModelNames =  { "CubeAndPlane", "SphereAndPlane", "CylinderAndPlane", "LengZhuAndPlane"};

    private GameObject[] m_Models;         // Found models
    private Queue<GameObject> m_ShowQueue; // Upcoming models to display
    private GameObject m_CurrentActiveModel;

    private Texture[] Text4UserStudy;
    
  

    public Camera m_RawCamera;
    public Camera m_MaskCamera;
    public Camera m_BackgroundCamera;
    
    [FormerlySerializedAs("inputField")] public TMP_InputField m_InputField;
    
    private WhiteShadowPostProcess m_WhiteShadowPostProcess;
    private void Awake()
    {
        m_Models = new GameObject[m_ModelNames.Length];
        for (int i = 0; i < m_ModelNames.Length; i++)
        {
            m_Models[i] = GameObject.Find(m_ModelNames[i]);
            if (m_Models[i] == null)
            {
                Debug.LogError("Not find " + m_ModelNames[i]);
            }
            else
            {
                m_Models[i].SetActive(false);
            }
        }
        
        // 注册输入完成时的回调（按回车或失焦）
        if (m_InputField == null)
        {
            Debug.LogError("InputFiled is null!");
        }
        else
        {
            //m_InputField.onEndEdit.AddListener(OnEndEdit);
            m_InputField.transform.parent.gameObject.SetActive(false);
        }

        Text4UserStudy = Resources.LoadAll<Texture>("Text4UserStudy");

        AssignCameraToDisplay();

    }

    private void Start()
    {
        BuildCertainQueue();
        m_CurrentActiveModel = m_ShowQueue.Dequeue();
        m_CurrentActiveModel.SetActive(true);
        AlignAllCamerasWithGo(m_CurrentActiveModel);
        ReplaceMainTexture(m_CurrentActiveModel.transform.GetChild(0).gameObject, Text4UserStudy[0]);
        
        
        
        m_WhiteShadowPostProcess = m_MaskCamera.GetComponent<WhiteShadowPostProcess>();
        Debug.Log("Starting"); 
        if (m_WhiteShadowPostProcess == null)
        {
            Debug.LogError("White Shadow PostProcess not found");
        }
        else
        {
            Debug.Log("White Shadow PostProcess found");
            m_WhiteShadowPostProcess.ConstructGivenObjectMask(m_CurrentActiveModel.transform.GetChild(0).gameObject);
            m_WhiteShadowPostProcess.ConstructGivenShadowMask(m_CurrentActiveModel.transform.GetChild(1).gameObject);
        }
        
    }

    private void AlignAllCamerasWithGo(GameObject go)
    {
        AlignCameraWithGo(m_RawCamera, go);
        AlignCameraWithGo(m_MaskCamera, go);
        AlignCameraWithGo(m_BackgroundCamera, go);
    }

    private void AlignCameraWithGo(Camera camera, GameObject go)
    {
        if (camera != null)
        {
            camera.transform.position = go.transform.position;
            camera.transform.rotation = go.transform.rotation;
        }
        else
        {
            Debug.LogError("Camera to align is null");
        }
    }
    
    private void OnEndEdit(string text)
    {
        Debug.Log("User Input: " + text);
        
    }
    
    

    private void SetCameraRTWidthAndHeight(Camera camera, int targetWidth, int targetHeight)
    {
        RenderTexture rt = new RenderTexture(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); // 24 是 depth buffer bits
        rt.name = "CustomRT";
        rt.Create();

        // 替换旧的 RT（如果有）
        if (camera.targetTexture != null)
        {
            camera.targetTexture.Release();
        }

        camera.targetTexture = rt;
    }

    private void SetCameraWidthAndHeight()
    {
        SetCameraRTWidthAndHeight(m_RawCamera, 1074, 604);
    }

    private void LogCameraWidthAndHeight()
    {
        Debug.Log("m_DepthCamera0.pixelWidth = " + m_RawCamera.pixelWidth);
        Debug.Log("m_DepthCamera0.pixelHeight = " + m_RawCamera.pixelHeight);
        //Debug.Log("m_DepthCamera0.targetTexture.width = " + m_DepthCamera0.targetTexture.width);
        //Debug.Log("m_DepthCamera0.targetTexture.height = " + m_DepthCamera0.targetTexture.height);
    }

 
    private void Update()
    {
        //LogCameraWidthAndHeight();
        if (m_ShowQueue.Count > 0 && Input.GetKeyDown(KeyCode.Space))
        {
            m_InputField.transform.parent.gameObject.SetActive(false);
            m_CurrentActiveModel.SetActive(false);
            m_CurrentActiveModel = m_ShowQueue.Dequeue();
            m_CurrentActiveModel.SetActive(true);
            
            ReplaceMainTexture(m_CurrentActiveModel.transform.GetChild(0).gameObject, Text4UserStudy[0]);
            
            m_WhiteShadowPostProcess.ConstructGivenObjectMask(m_CurrentActiveModel.transform.GetChild(0).gameObject);
            m_WhiteShadowPostProcess.ConstructGivenShadowMask(m_CurrentActiveModel.transform.GetChild(1).gameObject);
            
            AlignAllCamerasWithGo(m_CurrentActiveModel);
        }

        int digit = GetDigitDownThisFrame();
        if (digit != -1)
        {
            m_InputField.transform.parent.gameObject.SetActive(true);
            m_InputField.text = "Your input is " + digit;
        }
    }
    
    private int GetDigitDownThisFrame()
    {
        // 遍历 KeyCode.Alpha0 ~ KeyCode.Alpha9
        for (int i = 0; i <= 9; ++i)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                return i;
        }
        return -1;
    }

    private void BuildCertainQueue()
    {
        m_ShowQueue = new Queue<GameObject>();
        for (int i = 0; i < m_Models.Length; i++)
        {
            m_ShowQueue.Enqueue(m_Models[i]);
        }
    }
    
    private void ReplaceMainTexture(GameObject targetObj, Texture newTexture)
    {
        if (targetObj == null || newTexture == null) return;

        Renderer rend = targetObj.GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.Log("Deeper search for renderer component");
            rend = targetObj.transform.GetChild(0).gameObject.GetComponent<Renderer>();
        }

        // 确保有材质，并避免修改共享材质
        Material mat = rend.material;   // .material 会自动实例化
        if (mat == null)
        {
            Debug.LogError("mat is null");
            return;
        }

        mat.mainTexture = newTexture;
    }
    
    private void AssignCameraToDisplay()
    {
        int monitorCount = Display.displays.Length;   // 等于几就表示连了几台
        for (int i = 0; i < monitorCount; ++i)
        {
            Debug.Log($"显示器 {i} 分辨率 {Display.displays[i].renderingWidth}×{Display.displays[i].renderingHeight}");
        }
        
        // 1. 激活所有可用显示器
        for (int i = 0; i < Display.displays.Length; ++i)
        {
            // Windows 可自定义分辨率；macOS/Linux 会直接用系统分辨率
            Display.displays[i].Activate(
                Display.displays[i].systemWidth,
                Display.displays[i].systemHeight,
                60);
        }

        // 三台显示器?
        m_RawCamera.targetDisplay = 0;
        m_MaskCamera.targetDisplay = 1;
        m_BackgroundCamera.targetDisplay = 2;
    }
}
