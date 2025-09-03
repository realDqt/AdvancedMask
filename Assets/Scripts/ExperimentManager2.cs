using System.Collections.Generic;
using UnityEngine;

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
    
    struct ModelSceneInfo
    {
        public string m_ModelName;
        public int m_IntensityIdx;
        public bool m_Swap;
    }
    
    struct ModelSceneInfo
    {
        public string m_ModelName;
        public int m_IntensityIdx;
        public bool m_Swap;
    }

    private string m_PreExperimentModelName = "Capsule";
    private GameObject[] m_PreExperimentModels = new GameObject[2];
    
    [Header("Settings")]
    public int  m_AppearCountPerModel = 2;   // How many times each model should appear

    [Header("Model Names (must match scene hierarchy)")]
    private string[] m_ModelNames =  {"Teapot", "Bunny", "sofa_1", "SM_Veh_Mech_06", "Maple 1", "Spruce_Ball"};

    private GameObject[] m_Models;         // Found models
    private GameObject[] m_CurrentActiveModels = new GameObject[2]; // 当前场景中激活的两个模型
    
    private Dictionary<string, GameObject> m_Name2GO = new Dictionary<string, GameObject>();
    
    private Queue<ModelSceneInfo> m_ModelSceneInfoQueue;

    public Camera m_RawCamera;
    public Camera m_MaskCamera;
    public Camera m_BackgroundCamera;

    public GameObject m_Receiver;

    private WhiteShadowPostProcess m_WhiteShadowPostProcess;

    private int m_CurIntensityIdx = 1; // 当前亮度，取值1 2 3 4，代表四个不同亮度等级
    private int m_IntensityCount = 4; // 总共的亮度种类， 暂时写死是4

    public int[] m_Intensities = new int[] { 235, 200, 150, 110 };
    
    private int m_CurTimes = 1; // 当前实验进度

    private ModelSceneInfo m_CurInfo; // 当前场景信息，包括模型和亮度

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
                    modelSceneInfo.m_IntensityIdx = k + 1;
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

    private PreExperimentInfo GetPreExperimentInfo(int curTime, int curIntensityIdx)
    {
        PreExperimentInfo experimentInfo = new PreExperimentInfo();
        experimentInfo.m_CurTimes = curTime;
        experimentInfo.m_CurIntensity = m_Intensities[curIntensityIdx - 1];
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
    

    private void Start()
    {
        InitialPreExperiment();
    }

    private void InitialPreExperiment()
    {
        m_Phase = ExperimentPhase.PreExperiment;
        m_CurIntensityIdx = 1;
        
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
        InfluenceSceneByIntensity(m_CurIntensityIdx);
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
        m_CurIntensityIdx = 1;

        m_ModelSceneInfoQueue = ConvertArrayToQueue(ConstructRandomModelSceneInfo());
        
        int totalTimes = m_ModelNames.Length * m_IntensityCount * m_AppearCountPerModel;
        Debug.Log(m_ModelNames.Length + " " + m_IntensityCount + " " + m_AppearCountPerModel);
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

        LogPreExperimentInfo(GetPreExperimentInfo(m_CurTimes++, m_CurInfo.m_IntensityIdx));
        InfluenceSceneByIntensity(m_CurInfo.m_IntensityIdx);
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
    
    private void InfluenceSceneByIntensity(int intensityIdx)
    {
        // 使用参数intensityIdx影响当前场景
        Debug.Log("Influence the Scene by Intensity = " + m_Intensities[intensityIdx - 1]);
        m_BackgroundCamera.GetComponent<BGProcess>().multiplier = m_Intensities[intensityIdx - 1] / 255.0f;
    }
    
    private void Update()
    {

        if (m_Phase == ExperimentPhase.PreExperiment)
        {
            HandlePreExperiment();
        }
        else if(m_Phase == ExperimentPhase.FormalExperiment)
        {
            HandleFormalExperiment();
        }
    }

    private void HandlePreExperiment()
    {
        // ------------------------------改变亮度---------------------------------
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ++m_CurIntensityIdx;
            if (m_CurIntensityIdx > m_IntensityCount)
            {
                Debug.Log("Pre Experiment is ending");
                m_PreExperimentModels[0].SetActive(false);
                m_PreExperimentModels[1].SetActive(false);
                
                InitialFormalExperiment();
                
                return;
            }
            InfluenceSceneByIntensity(m_CurIntensityIdx);
        }
        
    }

    private void HandleFormalExperiment()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 记录实验数据到Log类
            Log.Instance.UpdateModelID(GetModelID(m_CurrentActiveModels[0]) + 1);
            Log.Instance.UpdateLightIntensity(m_Intensities[m_CurInfo.m_IntensityIdx - 1]);
            Log.Instance.LogManualEvent();
            // 判断实验是否已经结束
            if (m_ModelSceneInfoQueue.Count == 0)
            {
                Debug.Log("Experiment is ending! Thank you!");
                return;
            }
            // 切换
            m_CurrentActiveModels[0].SetActive(false);
            m_CurrentActiveModels[1].SetActive(false);
            ChangeToNext();
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
    }

}