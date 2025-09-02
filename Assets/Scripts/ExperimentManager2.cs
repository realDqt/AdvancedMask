using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;


public class ExperimentManager2 : MonoBehaviour
{
    
    /// <summary>
    /// 实验阶段枚举
    /// </summary>
    public enum ExperimentPhase
    {
        /// <summary>
        /// 预实验阶段
        /// </summary>
        PreExperiment,

        /// <summary>
        /// 正式实验阶段
        /// </summary>
        FormalExperiment
    }
    
    struct PreExperimentInfo
    {
        public int m_CurTimes;
        public int m_CurIntensity;
    }

    struct PostExperimentInfo
    {
        public float m_CurAngle;
        public int m_CurIntensity;
        public string m_CurTimeStr;
        public int m_ModelID;
    }
    
    struct ModelSceneInfo
    {
        public string m_ModelName;
        public int m_Intensity;
        public bool m_Swap;
    }

    private string m_PreExperimentModelName = "Capsule";
    private GameObject[] m_PreExperimentModels = new GameObject[2];
    private int m_CurPreTimes = 1;
    
    [Header("Settings")]
    public int  m_AppearCountPerModel = 2;   // How many times each model should appear

    [Header("Model Names (must match scene hierarchy)")]
    private string[] m_ModelNames =  { "Sphere", "Teapot", "sofa_1", "SM_Veh_Mech_06", "Maple 1"};

    private GameObject[] m_Models;         // Found models
    private GameObject[] m_CurrentActiveModels = new GameObject[2]; // 当前场景中激活的两个模型
    
    private Dictionary<string, GameObject> m_Name2GO = new Dictionary<string, GameObject>();
    
    private Queue<ModelSceneInfo> m_ModelSceneInfoQueue;
    
    // 拟合的系数
    public Vector4[] m_KR = new Vector4[3] { new Vector4(-4.078462e-04f, -9.498750e-03f, 1.025567e+00f, 1.0f), new Vector4(), new Vector4() };
    public Vector4[] m_KG = new Vector4[3] { new Vector4(-3.828662e-04f, -1.002039e-02f, 1.027372e+00f, 1.0f), new Vector4(), new Vector4() };
    public Vector4[] m_KB = new Vector4[3]{ new Vector4(-3.743558e-04f, -1.014192e-02f, 1.028135e+00f, 1.0f), new Vector4(), new Vector4() };

    private int m_CoeffIdx = 0;
    public Camera m_RawCamera;
    public Camera m_MaskCamera;
    public Camera m_BackgroundCamera;

    public GameObject m_Receiver;

    private string m_RawRTSavePath;
    private string m_MaskRTSavePath;
    private string m_BackgrondRTSavePath;

    private WhiteShadowPostProcess m_WhiteShadowPostProcess;

    private int m_CurIntensity = 1; // 当前亮度，取值1 2 3 4，代表四个不同亮度等级
    private int m_IntensityCount = 4; // 总共的亮度种类， 暂时写死是4
    
    private int m_CurTimes = 1; // 当前实验进度

    private ModelSceneInfo m_CurInfo; // 当前场景信息，包括模型和亮度

    public float m_FineAngleStep = 5.0f;    // 细调时，每次偏振片变化角度
    public float m_CoarseAngleStep = 10.0f; // 粗调时，每次偏振片变化角度
    private float m_CurAngle = 0.0f;        // 当前偏振片夹角

    private string m_SavePath = "D:\\DALAB\\Research\\Output\\ExperimentRes.csv";
    
    
    private PostExperimentInfo[] m_PostExperimentInfos;
    private int m_CurModelId = 0;
    
    private ExperimentPhase m_Phase = ExperimentPhase.PreExperiment;
    
    private void Awake()
    {
        m_Models = new GameObject[m_ModelNames.Length * 2];
        for (int i = 0; i < m_ModelNames.Length; i++)
        {
            m_Models[2 * i] = GameObject.Find(m_ModelNames[i]);
            m_Models[2 * i + 1] = GameObject.Find(m_ModelNames[i] + " (1)");
            m_Name2GO[m_ModelNames[i]] = m_Models[2 * i];
            m_Name2GO[m_ModelNames[i] + " (1)"] = m_Models[2 * i + 1];
            
            if (m_Models[2 * i] == null)
            {
                Debug.LogError("Model not found: " + m_ModelNames[i]);
            }
            else if (m_Models[2 * i + 1] == null)
            {
                Debug.LogError("Model not found: " + m_ModelNames[i] + " (1)");
            }
            else
            {
                m_Models[2 * i].SetActive(false);
                m_Models[2 * i + 1].SetActive(false);
            }
        }

        //SetCameraWidthAndHeight(1920, 1080);
        AssignCameraToDisplay();

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        m_RawRTSavePath = $@"E:\UnityProjects\9.2\AdvancedMask\Output\RawRT_{timestamp}.png";
        m_MaskRTSavePath = $@"E:\UnityProjects\9.2\AdvancedMask\Output\MaskRT_{timestamp}.png";
        m_BackgrondRTSavePath = $@"E:\UnityProjects\9.2\AdvancedMask\Output\BackgrondRT_{timestamp}.png";

    }
    
    private ModelSceneInfo[] ConstructRawModelSceneInfo()
    {
        ModelSceneInfo[] res = new ModelSceneInfo[m_ModelNames.Length * m_IntensityCount * m_AppearCountPerModel];
        if (m_AppearCountPerModel != 2)
        {
            Debug.LogError("AppearCountPerModel must be 2");
            return res;
        }

        int idx = 0;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < m_ModelNames.Length; j++)
            {
                for (int k = 0; k < m_IntensityCount; k++)
                {
                    ModelSceneInfo modelSceneInfo = new ModelSceneInfo();
                    modelSceneInfo.m_ModelName = m_ModelNames[j];
                    modelSceneInfo.m_Intensity = k + 1;
                    modelSceneInfo.m_Swap = (i == 0);
                    res[idx++] = modelSceneInfo;
                }
            }
        }
        return res;
    }
    
    

    private ModelSceneInfo[] ConstructRandomModelSceneInfo()
    {
        ModelSceneInfo[] res = ConstructRawModelSceneInfo();

        // Fisher–Yates 洗牌
        System.Random rng = new System.Random();
        for (int i = res.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);          // 0 ≤ j ≤ i
            (res[i], res[j]) = (res[j], res[i]);
        }

        return res;
    }

    private PreExperimentInfo GetPreExperimentInfo(int curTime, int curIntensity)
    {
        PreExperimentInfo experimentInfo = new PreExperimentInfo();
        experimentInfo.m_CurTimes = curTime;
        experimentInfo.m_CurIntensity = curIntensity;
        return experimentInfo;
    }
    
    private void LogPreExperimentInfo(PreExperimentInfo experimentInfo)
    {
        // 开始打印
        Debug.Log("Begin to Log Pre Experiment Info for " + experimentInfo.m_CurTimes);
        
        // 当前实验进度
        int totalTimes = m_ModelNames.Length * m_IntensityCount * m_AppearCountPerModel;;
        Debug.Log("Progress: " + experimentInfo.m_CurTimes + " / " + totalTimes);
        
        // 当前亮度
        Debug.Log("Current Intensity: " + experimentInfo.m_CurIntensity);
        
        // 结束打印
        Debug.Log("End to Log Pre Experiment Info for " + experimentInfo.m_CurTimes);
    }

    private PostExperimentInfo GetPostExperimentInfo(float curAngle, int curIntensity, string curTimeStr, int curModelID)
    {
        PostExperimentInfo experimentInfo = new PostExperimentInfo();
        experimentInfo.m_CurAngle = curAngle;
        experimentInfo.m_CurIntensity = curIntensity;
        experimentInfo.m_CurTimeStr = curTimeStr;
        experimentInfo.m_ModelID = curModelID;
        return experimentInfo;
    }

    private void LogPostExperimentInfo(PostExperimentInfo experimentInfo)
    {
        // 开始打印
        Debug.Log("Begin to Log Post Experiment Info for " + (m_CurTimes - 1));
        
        // 用户最终选择的夹角
        Debug.Log("Final Angle: " + experimentInfo.m_CurAngle);
        
        // 用户选择此夹角时的亮度
        Debug.Log("Cur Intensity: " + experimentInfo.m_CurIntensity);
        
        
        // 结束打印
        Debug.Log("End to Log Post Experiment Info for " + (m_CurTimes - 1));
    }
    

    private void Start()
    {
        // 1. Locate models
        // Now it's implemented in Awake()
        
        // 2. 初始化预实验阶段
        InitialPreExperiment();
        
    }

    private void InitialPreExperiment()
    {
        m_Phase = ExperimentPhase.PreExperiment;
        m_CurIntensity = 1;
        m_CurAngle = GetPhysicalDeviceAngle();
        
        m_PreExperimentModels[0] = GameObject.Find(m_PreExperimentModelName);
        if (m_PreExperimentModels[0] == null)
        {
            Debug.LogError("Pre Experiment Model not found: " + m_PreExperimentModelName);
        }
        m_PreExperimentModels[0].SetActive(true);
        
        m_PreExperimentModels[1] = GameObject.Find(m_PreExperimentModelName + " (1)");
        if (m_PreExperimentModels[1] == null)
        {
            Debug.LogError("Pre Experiment Model not found: " + m_PreExperimentModelName + " (1)");
        }
        m_PreExperimentModels[1].SetActive(true);

        m_WhiteShadowPostProcess = m_MaskCamera.GetComponent<WhiteShadowPostProcess>();
        m_WhiteShadowPostProcess.ConstructGivenObjectsMask(m_PreExperimentModels);
        m_WhiteShadowPostProcess.ConstructGivenShadowMask(m_Receiver);

        Debug.Log("Start Pre Experiment");
        InfluenceSceneByIntensity(m_CurIntensity);
    }

    Queue<ModelSceneInfo> ConvertArrayToQueue(ModelSceneInfo[] arr)
    {
        Queue<ModelSceneInfo> queue = new Queue<ModelSceneInfo>();
        foreach (ModelSceneInfo item in arr)
        {
            queue.Enqueue(item);
        }

        return queue;
    }
    private void InitialFormalExperiment()
    {
        m_Phase = ExperimentPhase.FormalExperiment;
        m_CurIntensity = 1;
        m_CurAngle = GetPhysicalDeviceAngle();

        m_ModelSceneInfoQueue = ConvertArrayToQueue(ConstructRandomModelSceneInfo());
        
        int totalTimes = m_ModelNames.Length * m_IntensityCount * m_AppearCountPerModel;
        Debug.Log(m_ModelNames.Length + " " + m_IntensityCount + " " + m_AppearCountPerModel);
        m_PostExperimentInfos = new PostExperimentInfo[totalTimes];
        Debug.Log("Start Formal Experiment, Total Times = " +  totalTimes);
        


        ChangeToNext();
    }

    private bool ChangeToNext()
    {
        if (m_ModelSceneInfoQueue.Count == 0)
            return false;
        m_CurInfo = m_ModelSceneInfoQueue.Dequeue();
        m_CurrentActiveModels[0] = m_Name2GO[m_CurInfo.m_ModelName];
        m_CurrentActiveModels[1] = m_Name2GO[m_CurInfo.m_ModelName + " (1)"];
        if(m_CurInfo.m_Swap)
            SwapTransform(m_CurrentActiveModels[0], m_CurrentActiveModels[1]);
        m_CurrentActiveModels[0].SetActive(true);
        m_CurrentActiveModels[1].SetActive(true);
        
        //SetCameraWidthAndHeight();

        //Debug.Log("Starting"); 
        if (m_WhiteShadowPostProcess == null)
        {
            Debug.LogError("White Shadow PostProcess not found");
        }
        else
        {
            //Debug.Log("White Shadow PostProcess found");
            m_WhiteShadowPostProcess.ConstructGivenObjectsMask(m_CurrentActiveModels);
            m_WhiteShadowPostProcess.ConstructGivenShadowMask(m_Receiver);
        }

        LogPreExperimentInfo(GetPreExperimentInfo(m_CurTimes++, m_CurInfo.m_Intensity));
        InfluenceSceneByIntensity(m_CurInfo.m_Intensity);
        return true;
    }
    
    void SwapTransform(GameObject go1, GameObject go2)
    {
        // 缓存 go1 的原始变换数据
        Vector3    pos1 = go1.transform.position;
        Quaternion rot1 = go1.transform.rotation;
        Vector3    scl1 = go1.transform.localScale;

        // 把 go1 换成 go2 的变换
        go1.transform.position = go2.transform.position;
        go1.transform.rotation = go2.transform.rotation;
        go1.transform.localScale = go2.transform.localScale;

        // 把 go2 换成 go1 的原始变换
        go2.transform.position = pos1;
        go2.transform.rotation = rot1;
        go2.transform.localScale = scl1;
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

    private void SetCameraWidthAndHeight(int targetWidth, int targetHeight)
    {
        SetCameraRTWidthAndHeight(m_RawCamera, targetWidth, targetHeight);
        SetCameraRTWidthAndHeight(m_MaskCamera, targetWidth, targetHeight);
        SetCameraRTWidthAndHeight(m_BackgroundCamera, targetWidth, targetHeight);
    }

    private void LogCameraWidthAndHeight()
    {
        //Debug.Log("m_DepthCamera0.pixelWidth = " + m_RawCamera.pixelWidth);
        //Debug.Log("m_DepthCamera0.pixelHeight = " + m_RawCamera.pixelHeight);
        //Debug.Log("m_DepthCamera0.targetTexture.width = " + m_DepthCamera0.targetTexture.width);
        //Debug.Log("m_DepthCamera0.targetTexture.height = " + m_DepthCamera0.targetTexture.height);
    }

    private void SaveCameraRTToDisk(Camera camera, string path)
    {
        RenderTexture.active = camera.targetTexture;
        Texture2D tex = new Texture2D(
            camera.targetTexture.width,
            camera.targetTexture.height,
            TextureFormat.RGBA32,
            false
        );
        tex.ReadPixels(
            new Rect(0, 0,
                camera.targetTexture.width,
                camera.targetTexture.height),
            0, 0
        );
        tex.Apply();
        RenderTexture.active = null;

        /* ---------- 关键：把透明像素填成黑色 ---------- */
        Color32[] pixels = tex.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a == 0)          // 完全透明
            {
                pixels[i] = new Color32(0, 0, 0, 255); // 纯黑、不透明
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        /* ------------------------------------------------ */

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        Debug.Log($"RT 已保存为 PNG：{path}");
        Destroy(tex);
    }
    
    private void SaveRTToDisk()
    {
       SaveCameraRTToDisk(m_RawCamera, m_RawRTSavePath);
       SaveCameraRTToDisk(m_MaskCamera, m_MaskRTSavePath);
       SaveCameraRTToDisk(m_BackgroundCamera, m_BackgrondRTSavePath);
    }

    private void InfluenceSceneByIntensity(int intensity)
    {
        // TODO: 使用参数intensity影响当前场景
        Debug.Log("Influence the Scene by Intensity = " + intensity);
    }

    private string GetCurTime()
    {
        string nowStr = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        return nowStr;
    }
    private float GetPhysicalDeviceAngle()
    {
        // TODO: 返回偏振片当前角度
        return 0.0f;
    }

    private void SetPhysicalDeviceAngle(float angle)
    {
        // TODO: 设置偏振片角度
        Debug.Log("Set Angle = " + angle);
    }
    
    private void Update()
    {
        //LogCameraWidthAndHeight();
        //SetCoefficient();


        if (m_Phase == ExperimentPhase.PreExperiment)
        {
            HandlePreExperiment();
        }
        else if(m_Phase == ExperimentPhase.FormalExperiment)
        {
            HandleFormalExperiment();
        }

        
        //if(Input.GetKeyDown(KeyCode.R))
        //SaveRTToDisk();
    }

    private void HandlePreExperiment()
    {
        // ------------------------------改变亮度---------------------------------
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ++m_CurIntensity;
            if (m_CurIntensity > m_IntensityCount)
            {
                Debug.Log("Pre Experiment is ending");
                m_PreExperimentModels[0].SetActive(false);
                m_PreExperimentModels[1].SetActive(false);
                
                InitialFormalExperiment();
                
                return;
            }
            InfluenceSceneByIntensity(m_CurIntensity);
        }
        
        // ------------------------------调整偏振片---------------------------------
        //AdjustPolarizerAngle();
    }

    
    private void HandleFormalExperiment()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 记录
            PostExperimentInfo experimentInfo = GetPostExperimentInfo(m_CurAngle, m_CurInfo.m_Intensity, GetCurTime(), GetModelID(m_CurrentActiveModels[0]) + 1);
            LogPostExperimentInfo(experimentInfo);
            m_PostExperimentInfos[m_CurTimes - 2] = experimentInfo;
            
            // 判断实验是否已经结束
            if (m_CurTimes - 2 == m_PostExperimentInfos.Length - 1)
            {
                Debug.Log("Experiment is ending! Thank you!");
                SavePostExperimentInfoToCsv(m_PostExperimentInfos, m_SavePath);
                return;
            }
            
            // 切换
            m_CurrentActiveModels[0].SetActive(false);
            m_CurrentActiveModels[1].SetActive(false);
            ChangeToNext();
        }
        
        
        
        
        // ------------------------------调整偏振片---------------------------------
        //AdjustPolarizerAngle();
    }

    //void AdjustPolarizerAngle()
    //{
    //    // 左右粗调 上下细调整
    //    // 左加右减 上加下减
    //    if (Input.GetKeyDown(KeyCode.LeftArrow))
    //    {
    //        m_CurAngle += m_CoarseAngleStep;
    //        Debug.Log("Coarse Adjustment: CurAngle = " + m_CurAngle);
    //        SetPhysicalDeviceAngle(m_CurAngle);
    //    }else if (Input.GetKeyDown(KeyCode.RightArrow))
    //    {
    //        m_CurAngle -= m_CoarseAngleStep;
    //        Debug.Log("Coarse Adjustment: CurAngle = " + m_CurAngle);
    //        SetPhysicalDeviceAngle(m_CurAngle);
    //    }else if (Input.GetKeyDown(KeyCode.UpArrow))
    //    {
    //        m_CurAngle += m_FineAngleStep;
    //        Debug.Log("Fine Adjustment: CurAngle = " + m_CurAngle);
    //        SetPhysicalDeviceAngle(m_CurAngle);
    //    }else if (Input.GetKeyDown(KeyCode.DownArrow))
    //    {
    //        m_CurAngle -= m_FineAngleStep;
    //        Debug.Log("Fine Adjustment: CurAngle = " + m_CurAngle);
    //        SetPhysicalDeviceAngle(m_CurAngle);
    //    }
    //}
    
    void SetCoefficient()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            m_CoeffIdx = 0;
        }else if (Input.GetKeyDown(KeyCode.W))
        {
            m_CoeffIdx = 1;
        }else if (Input.GetKeyDown(KeyCode.E))
        {
            m_CoeffIdx = 2;
        }
        
        //Debug.Log("Test: coefficient idx = " + m_Idx);

        var antiDistortion = m_RawCamera.GetComponent<AntiDistortion>();
        if (antiDistortion)
        {
            antiDistortion.m_KR = m_KR[m_CoeffIdx];
            antiDistortion.m_KG = m_KG[m_CoeffIdx];
            antiDistortion.m_KB = m_KB[m_CoeffIdx]; 
        }
    }



    private int GetModelID(GameObject go)
    {
        for (int i = 0; i < m_ModelNames.Length; ++i)
        {
            if (go.name == m_ModelNames[i])
            {
                return i;
            }
        }
        Debug.LogError("Not found " + go.name);
        return -1;
    }

    private void AssignCameraToDisplay()
    {
        int monitorCount = Display.displays.Length;   // 等于几就表示连了几台
        for (int i = 0; i < monitorCount; ++i)
        {
            //Debug.Log($"显示器 {i} 分辨率 {Display.displays[i].renderingWidth}×{Display.displays[i].renderingHeight}");
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
        //m_RawCamera.targetDisplay = 0;
        //m_MaskCamera.targetDisplay = 1;
        //m_BackgroundCamera.targetDisplay = 2;
    }
    
   private void SavePostExperimentInfoToCsv(PostExperimentInfo[] data, string filePath)
    {
        if (data == null || data.Length == 0)
        {
            Debug.LogWarning("数据为空，未生成 CSV。");
            return;
        }

        // 确保目录存在
        string dir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using (StreamWriter sw = new StreamWriter(filePath, false)) // false = 覆盖写入
        {
            // 写表头
            sw.WriteLine("ExperimentID,Angle,Intensity,ModelID,Time"); // TODO: Angle在此记录?

            // 写数据
            for (int i = 0; i < data.Length; i++)
            {
                sw.WriteLine($"{i + 1},{data[i].m_CurAngle},{data[i].m_CurIntensity},{data[i].m_ModelID},{data[i].m_CurTimeStr}");
            }
        }

        Debug.Log($"CSV 已保存至：{filePath}");
    }
}