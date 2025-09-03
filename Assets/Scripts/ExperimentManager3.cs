using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using System.IO;

public class ExperimentManager3 : MonoBehaviour
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
        public int m_TextIdx; // 从0开始
    }

    struct PostExperimentInfo
    {
        public float m_CurAngle;
        public int m_CurIntensity;
        public string m_CurTimeStr;
        public int m_ModelID;
        public int m_TextSize;
        public int m_TextIdx; // 从1开始
        public int m_Answer;
    }

    struct ModelSceneInfo
    {
        public string m_ModelName;
        public int m_TextIdx; // 从0开始
        public int m_IntensityIdx;
        public int m_TextSize;
    }
    
    [Header("Model Names (must match scene hierarchy)")]
    private string[] m_ModelNames =  { "CubeAndPlane", "SphereAndPlane", "CylinderAndPlane"};
    
    private Dictionary<string, GameObject> m_Name2GO = new Dictionary<string, GameObject>();

    private GameObject[] m_Models;         // Found models
    private Queue<GameObject> m_ShowQueue; // Upcoming models to display
    private GameObject m_CurrentActiveModel;

    private Texture[] m_Text4UserStudy;
    private int[] m_Idx2Digit = new int[]{5,8,5,0,9,6,8,8,2,7,4,3,8,3,9,4,6,4,0,5,2,5,1,1,9,7,7,8,7,4,3,2,4,8,5,8};
    
  

    public Camera m_RawCamera;
    public Camera m_MaskCamera;
    public Camera m_BackgroundCamera;

    public int m_BackgroundWidth = 512;
    public int m_BackgroundHeight = 512;
    
    public int  m_AppearCountPerModel = 2;
    
    private int m_CurIntensityIdx = 1; // 当前亮度，取值1 2 3 4，代表四个不同亮度等级
    private int m_IntensityCount = 4; // 总共的亮度种类， 暂时写死是4
    
    private Queue<ModelSceneInfo> m_ModelSceneInfoQueue;

    public int[] m_Intensities = new int[] { 110, 150, 200, 235};
    
    [FormerlySerializedAs("inputField")] public TMP_InputField m_InputField;
    
    private WhiteShadowPostProcess m_WhiteShadowPostProcess;

    private string m_RawRTSavePath = "D:\\DALAB\\Research\\AdvancedMask\\Output\\RawRT.png";
    private string m_BackgroundRTSavePath = "D:\\DALAB\\Research\\Output\\BackgroundRT.png";

    private int m_CurTimes = 1;
    private ModelSceneInfo m_CurInfo;
    
    private PostExperimentInfo[] m_PostExperimentInfos;
    
    private float m_CurAngle = 0.0f;        // 当前偏振片夹角

    private string m_SavePath = "D:\\DALAB\\Research\\Output\\TextExperimentRes.csv";
    
    private ExperimentPhase m_Phase = ExperimentPhase.PreExperiment;
    
    private GameObject m_PreExperimentModel;
    private string m_PreExperimentModelName = "LengZhuAndPlane";
    private int[] m_PreExperimentTextIdxs = new int[]{0, 12, 24, 35};
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
                m_Name2GO[m_ModelNames[i]] = m_Models[i];
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

        m_Text4UserStudy = Resources.LoadAll<Texture>("Text4UserStudy");
        
        Debug.Log("Text Textures' Num = " + m_Text4UserStudy.Length);
        AssignCameraToDisplay();

        //SetCameraRTWidthAndHeight(m_RawCamera, 1920, 1080);
    }
    
    private ModelSceneInfo[] ConstructRawModelSceneInfo()
    {
        ModelSceneInfo[] res = new ModelSceneInfo[m_ModelNames.Length * m_IntensityCount * m_AppearCountPerModel];

        int idx = 0;
        for (int i = 0; i < m_AppearCountPerModel; i++) // 3
        {
            ModelSceneInfo modelSceneInfo = new ModelSceneInfo();
            for (int j = 0; j < m_ModelNames.Length; j++) // 3
            {
                for (int k = 0; k < m_IntensityCount; k++) // 4
                {
                    modelSceneInfo.m_ModelName = m_ModelNames[j];
                    modelSceneInfo.m_IntensityIdx = k + 1;
                    modelSceneInfo.m_TextIdx = idx;
                    modelSceneInfo.m_TextSize = idx / (m_ModelNames.Length * m_IntensityCount) + 1;
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
    
    Queue<ModelSceneInfo> ConvertArrayToQueue(ModelSceneInfo[] arr)
    {
        Queue<ModelSceneInfo> queue = new Queue<ModelSceneInfo>();
        foreach (ModelSceneInfo item in arr)
        {
            queue.Enqueue(item);
        }
        return queue;
    }
    
    private PreExperimentInfo GetPreExperimentInfo(int curTime, int curIntensityIdx, int curTextIdx)
    {
        PreExperimentInfo experimentInfo = new PreExperimentInfo();
        experimentInfo.m_CurTimes = curTime;
        experimentInfo.m_CurIntensity = m_Intensities[curIntensityIdx - 1];
        experimentInfo.m_TextIdx = curTextIdx;
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
        
        // 正确答案
        Debug.Log("Answer: " + m_Idx2Digit[experimentInfo.m_TextIdx]);
        
        // 结束打印
        Debug.Log("End to Log Pre Experiment Info for " + experimentInfo.m_CurTimes);
    }

    private PostExperimentInfo GetPostExperimentInfo(float curAngle, int curIntensityIdx, string curTimeStr, int curModelID, int curTextSize, int curTextIdx)
    {
        PostExperimentInfo experimentInfo = new PostExperimentInfo();
        experimentInfo.m_CurAngle = curAngle;
        experimentInfo.m_CurIntensity = m_Intensities[curIntensityIdx - 1];
        experimentInfo.m_CurTimeStr = curTimeStr;
        experimentInfo.m_ModelID = curModelID;
        experimentInfo.m_TextSize = curTextSize;
        experimentInfo.m_TextIdx = curTextIdx;
        experimentInfo.m_Answer = m_Idx2Digit[curTextIdx - 1];
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
        
        // 字体大小
        Debug.Log("Cur Text Size: " + experimentInfo.m_TextSize);
        
        // 字体纹理索引
        Debug.Log("Cur Text Idx: " + experimentInfo.m_TextIdx);
        
        // 结束打印
        Debug.Log("End to Log Post Experiment Info for " + (m_CurTimes - 1));
    }

    private Texture GetTextTexture(int textIdx)
    {
        //Debug.Log("Try to get " + textIdx);
        return m_Text4UserStudy[textIdx];
    }

    private void Start()
    {
        //BuildCertainQueue();

        InitialPreExperiment();
        
    }

    private void InitialFormalExperiment()
    {
        m_Phase = ExperimentPhase.FormalExperiment;
        
        int totalTimes = m_ModelNames.Length * m_IntensityCount * m_AppearCountPerModel;
        Debug.Log(m_ModelNames.Length + " " + m_IntensityCount + " " + m_AppearCountPerModel);
        m_PostExperimentInfos = new PostExperimentInfo[totalTimes];
        
        m_ModelSceneInfoQueue = ConvertArrayToQueue(ConstructRandomModelSceneInfo());
        Debug.Log("queue count = " + m_ModelSceneInfoQueue.Count);

        ChangeToNext();
    }


    private bool ChangeToNext()
    {
        if (m_ModelSceneInfoQueue.Count == 0)
            return false;
        m_CurInfo = m_ModelSceneInfoQueue.Dequeue();
        m_CurrentActiveModel = m_Name2GO[m_CurInfo.m_ModelName];
        m_CurrentActiveModel.SetActive(true);
        AlignAllCamerasWithGo(m_CurrentActiveModel);
        ReplaceMainTexture(m_CurrentActiveModel.transform.GetChild(0).gameObject, GetTextTexture(m_CurInfo.m_TextIdx));
        
        //SetCameraRTWidthAndHeight(m_BackgroundCamera, m_BackgroundWidth, m_BackgroundHeight);
        
        //m_WhiteShadowPostProcess = m_MaskCamera.GetComponent<WhiteShadowPostProcess>();
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
        
        LogPreExperimentInfo(GetPreExperimentInfo(m_CurTimes++, m_CurInfo.m_IntensityIdx, m_CurInfo.m_TextIdx));
        InfluenceSceneByIntensity(m_CurInfo.m_IntensityIdx);
        return true;
    }
    
    private void InfluenceSceneByIntensity(int intensityIdx)
    {
        // 使用参数intensityIdx影响当前场景
        Debug.Log("Influence the Scene by Intensity = " + m_Intensities[intensityIdx - 1]);
        m_BackgroundCamera.GetComponent<BGProcess>().multiplier = m_Intensities[intensityIdx - 1] / 255.0f;
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
        SetCameraRTWidthAndHeight(m_RawCamera, targetWidth, targetWidth);
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
    

    private void InitialPreExperiment()
    {
        m_Phase = ExperimentPhase.PreExperiment;
        m_CurIntensityIdx = 1;
        m_CurAngle = GetPhysicalDeviceAngle();
        
        
        m_PreExperimentModel = GameObject.Find(m_PreExperimentModelName);
        if (m_PreExperimentModel == null)
        {
            Debug.LogError("Pre Experiment Model not found: " + m_PreExperimentModelName);
        }
        m_PreExperimentModel.SetActive(true);
        
        AlignAllCamerasWithGo(m_PreExperimentModel);
        
        ReplaceMainTexture(m_PreExperimentModel.transform.GetChild(0).gameObject, GetTextTexture(m_PreExperimentTextIdxs[m_CurIntensityIdx - 1]));

        m_WhiteShadowPostProcess = m_MaskCamera.GetComponent<WhiteShadowPostProcess>();
        if (m_WhiteShadowPostProcess == null)
        {
            Debug.LogError("White Shadow PostProcess not found");
        }
        else
        {
            m_WhiteShadowPostProcess.ConstructGivenObjectMask(m_PreExperimentModel.transform.GetChild(0).gameObject);
            m_WhiteShadowPostProcess.ConstructGivenShadowMask(m_PreExperimentModel.transform.GetChild(1).gameObject);
        }

        Debug.Log("Start Pre Experiment");
        InfluenceSceneByIntensity(m_CurIntensityIdx);
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
        if(m_Phase == ExperimentPhase.PreExperiment){
            HandlePreExperiment();
        }else
        {
            HandleFormalExperiment();
        }
    }

    private void HandlePreExperiment()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ++m_CurIntensityIdx;
            if (m_CurIntensityIdx > m_IntensityCount)
            {
                Debug.Log("Pre Experiment is ending");
                m_PreExperimentModel.SetActive(false);
                
                InitialFormalExperiment();
                
                return;
            }
            ReplaceMainTexture(m_PreExperimentModel.transform.GetChild(0).gameObject, GetTextTexture(m_PreExperimentTextIdxs[m_CurIntensityIdx - 1]));
            if (m_WhiteShadowPostProcess == null)
            {
                Debug.LogError("White Shadow PostProcess not found");
            }
            else
            {
                Debug.Log("White Shadow PostProcess found");
                m_WhiteShadowPostProcess.ConstructGivenObjectMask(m_PreExperimentModel.transform.GetChild(0).gameObject);
                m_WhiteShadowPostProcess.ConstructGivenShadowMask(m_PreExperimentModel.transform.GetChild(1).gameObject);
            }
            InfluenceSceneByIntensity(m_CurIntensityIdx);
        }
    }

    private void HandleFormalExperiment()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 记录
            PostExperimentInfo experimentInfo = GetPostExperimentInfo(m_CurAngle, m_CurInfo.m_IntensityIdx, GetCurTime(), GetModelID(m_CurrentActiveModel) + 1, m_CurInfo.m_TextSize, m_CurInfo.m_TextIdx + 1);
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
            m_CurrentActiveModel.SetActive(false);
            ChangeToNext();
        }
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
            sw.WriteLine("ExperimentID,Angle,Intensity,ModelID,Time,TextSize,TextIdx,Answer"); // TODO: Angle在此记录?

            // 写数据
            for (int i = 0; i < data.Length; i++)
            {
                sw.WriteLine($"{i + 1},{data[i].m_CurAngle},{data[i].m_CurIntensity},{data[i].m_ModelID},{data[i].m_CurTimeStr},{data[i].m_TextSize},{data[i].m_TextIdx},{data[i].m_Answer}");
            }
        }

        Debug.Log($"CSV 已保存至：{filePath}");
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
    
    private string GetCurTime()
    {
        string nowStr = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        return nowStr;
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
        // m_RawCamera.targetDisplay = 0;
        // m_MaskCamera.targetDisplay = 1;
        // m_BackgroundCamera.targetDisplay = 2;
    }
}
