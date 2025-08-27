using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class ExperimentManager2 : MonoBehaviour
{
    [Header("Settings")]
    public int  m_AppearCountPerModel = 3;   // How many times each model should appear
    private float m_ApearTimePerModel  = 2f;  // How long each model stays visible

    [Header("Model Names (must match scene hierarchy)")]
    private string[] m_ModelNames = new string[]
    {
        "Teapot", "Monkey", "Dragon", "Buddha", "Bunny", "Sphere", "Torus", "Intergalactic_Spaceship-(Wavefront)", "Only_Spider_with_Animations_Export",
        "rose2",
    };

    private GameObject[] m_Models;         // Found models
    private Queue<GameObject> m_ShowQueue; // Upcoming models to display
    private GameObject m_CurrentActive = null;    // Currently active model
    
    // 拟合的系数
    public Vector4[] m_KR = new Vector4[3] { new Vector4(-4.078462e-04f, -9.498750e-03f, 1.025567e+00f, 1.0f), new Vector4(), new Vector4() };
    public Vector4[] m_KG = new Vector4[3] { new Vector4(-3.828662e-04f, -1.002039e-02f, 1.027372e+00f, 1.0f), new Vector4(), new Vector4() };
    public Vector4[] m_KB = new Vector4[3]{ new Vector4(-3.743558e-04f, -1.014192e-02f, 1.028135e+00f, 1.0f), new Vector4(), new Vector4() };

    private int m_CoeffIdx = 0;
    public Camera m_RawCamera;
    public Camera m_MaskCamera;
    public Camera m_BackgroundCamera;

    public GameObject m_Receiver;

    private string m_RawRTSavePath = "D:\\DALAB\\Research\\AdvancedMask\\Output\\RawRT.png";
    private string m_MaskRTSavePath = "D:\\DALAB\\Research\\AdvancedMask\\Output\\MaskRT.png";
    private string m_BackgrondRTSavePath = "D:\\DALAB\\Research\\AdvancedMask\\Output\\BackgrondRT.png";

    private WhiteShadowPostProcess m_WhiteShadowPostProcess;

    private void Awake()
    {
        m_Models = new GameObject[m_ModelNames.Length];
        for (int i = 0; i < m_Models.Length; i++)
        {
            m_Models[i] = GameObject.Find(m_ModelNames[i]);
            if (m_Models[i] == null)
            {
                Debug.LogError("Model not found: " + m_ModelNames[i]);
            }
            else
            {
                m_Models[i].SetActive(false);
            }
        }
    }

    private void Start()
    {
        // 1. Locate models
        // Now it's implemented in Awake()

        // 2. Build randomized queue
        BuildRandomQueue();

        // 3. Start showing loop
        //StartCoroutine(ShowLoop());
        
        m_CurrentActive = m_ShowQueue.Count > 0 ? m_ShowQueue.Dequeue() : null;
        if(m_CurrentActive)m_CurrentActive.SetActive(true);
        else Debug.LogError("Initial Current active is null");

        SetCameraWidthAndHeight();

        m_WhiteShadowPostProcess = m_MaskCamera.GetComponent<WhiteShadowPostProcess>();
        Debug.Log("Starting");
        if (m_WhiteShadowPostProcess == null)
        {
            Debug.LogError("White Shadow PostProcess not found");
        }
        else
        {
            Debug.Log("White Shadow PostProcess found");
            m_WhiteShadowPostProcess.ConstructGivenObjectMask(m_CurrentActive);
            m_WhiteShadowPostProcess.ConstructGivenShadowMask(m_Receiver);
        }
        
        //LoadImgToBackgroundCamera("Backgrounds/Small8");
    }

    private void LoadImgToBackgroundCamera(string imgPath)
    {
        
        Texture2D srcTex = Resources.Load<Texture2D>(imgPath);
        if (srcTex == null)
        {
            Debug.LogError("Failed to find img at ：" + imgPath);
            return;
        }
        
        // copy
        Graphics.Blit(srcTex, m_BackgroundCamera.targetTexture);
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
        SetCameraRTWidthAndHeight(m_MaskCamera, 1074, 604);
        SetCameraRTWidthAndHeight(m_BackgroundCamera, 1074, 604);
    }

    private void LogCameraWidthAndHeight()
    {
        Debug.Log("m_DepthCamera0.pixelWidth = " + m_RawCamera.pixelWidth);
        Debug.Log("m_DepthCamera0.pixelHeight = " + m_RawCamera.pixelHeight);
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

    private void Update()
    {
        LogCameraWidthAndHeight();
        SetCoefficient();
        if (m_ShowQueue.Count > 0 && Input.GetKeyDown(KeyCode.Space))
        {
            m_CurrentActive.SetActive(false);
            m_CurrentActive = m_ShowQueue.Dequeue();
            m_CurrentActive.SetActive(true);
            
            m_WhiteShadowPostProcess.ConstructGivenObjectMask(m_CurrentActive);
            m_WhiteShadowPostProcess.ConstructGivenShadowMask(m_Receiver);
        }
        if(Input.GetKeyDown(KeyCode.R))
            SaveRTToDisk();
    }
    
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

    /// <summary>
    /// Builds a queue of models ensuring each appears exactly m_AppearCountPerModel times
    /// and that no two consecutive models are identical.
    /// </summary>
    private void BuildRandomQueue()
    {
        // 1. Populate the master list: each valid model is added m_AppearCountPerModel times.
        List<GameObject> masterList = new List<GameObject>();
        foreach (var go in m_Models)
        {
            if (go == null) continue;
            for (int i = 0; i < m_AppearCountPerModel; i++)
                masterList.Add(go);
        }

        // 2. Randomly extract models while preventing adjacent duplicates.
        m_ShowQueue = new Queue<GameObject>();
        GameObject lastSelected = null;

        while (masterList.Count > 0)
        {
            // Gather indices of models that differ from the last selected one.
            List<int> validIndices = new List<int>();
            for (int i = 0; i < masterList.Count; i++)
                if (masterList[i] != lastSelected || masterList.Count == 1) // Accept forced pick if only one remains.
                    validIndices.Add(i);

            // Choose a random valid index.
            int chosenIndex = validIndices[Random.Range(0, validIndices.Count)];
            GameObject chosenModel = masterList[chosenIndex];

            m_ShowQueue.Enqueue(chosenModel);
            lastSelected = chosenModel;

            // Remove the chosen instance from the master list.
            masterList.RemoveAt(chosenIndex);
        }
    }

    /// <summary>
    /// Coroutine that displays models one by one
    /// </summary>
    private IEnumerator ShowLoop()
    {
        while (m_ShowQueue.Count > 0)
        {
            // Hide previous model
            if (m_CurrentActive != null)
                m_CurrentActive.SetActive(false);

            // Show next model
            m_CurrentActive = m_ShowQueue.Dequeue();
            m_CurrentActive.SetActive(true);

            // Wait
            yield return new WaitForSeconds(m_ApearTimePerModel);
        }

        // All done, hide last model
        if (m_CurrentActive != null)
            m_CurrentActive.SetActive(false);

        Debug.Log("All models have been displayed.");
    }
}