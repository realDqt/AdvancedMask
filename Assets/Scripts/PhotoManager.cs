using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PhotoManager : MonoBehaviour
{

    [Header("Model Names (must match scene hierarchy)")]
    private string[] m_ModelNames =  { "Desk", "Laocoon_Statue", "Armadillo", "sofa_1", "SM_Veh_Mech_01", "Acacia 2"};

    private GameObject[] m_Models;         // Found models
    private Queue<GameObject> m_ShowQueue; // Upcoming models to display
    private GameObject m_CurrentActiveModel;
    
  

    public Camera m_RawCamera;

    private string m_RawRTSavePath = "D:\\DALAB\\Research\\AdvancedMask\\Output\\RawRT.png";
    

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
    }

    private void Start()
    {
        SetCameraWidthAndHeight();
        BuildCertainQueue();
        m_CurrentActiveModel = m_ShowQueue.Dequeue();
        m_CurrentActiveModel.SetActive(true);

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
    }

    private void Update()
    {
        LogCameraWidthAndHeight();
        if (m_ShowQueue.Count > 0 && Input.GetKeyDown(KeyCode.Space))
        {
            m_CurrentActiveModel.SetActive(false);
            m_CurrentActiveModel = m_ShowQueue.Dequeue();
            m_CurrentActiveModel.SetActive(true);
        }
        if(Input.GetKeyDown(KeyCode.R))
            SaveRTToDisk();
    }
    

    private void BuildCertainQueue()
    {
        m_ShowQueue = new Queue<GameObject>();
        for (int i = 0; i < m_Models.Length; i++)
        {
            m_ShowQueue.Enqueue(m_Models[i]);
        }
    }
  
}
