using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExperimentManager : MonoBehaviour
{
     public GameObject m_StartUI;
     //public GameObject m_IntervalUI;
     public GameObject m_PauseUI;
     public GameObject m_RestUI;
     public GameObject m_FinishUI;

     public Image m_ShowImg;
     
     private Sprite[] m_TestSprites;

     private string m_TestImgPath = "TestImages";

     private float m_ExperimentStartTime = 0.0f;
     private float m_ImgGapTime = 2.0f;
     private float m_PauseUIAppearTime = 2.0f;
     private float m_StartUIAppearTime = 2.0f;
     private int m_CurGroupIdx = -1;
     private int[] m_UserChoices;
    
     
    // Start is called before the first frame update
    void Start()
    {
        LoadImg();
        m_StartUI.SetActive(true);
        //m_IntervalUI.SetActive(false);
        m_PauseUI.SetActive(false);
        m_RestUI.SetActive(false);
        m_FinishUI.SetActive(false);
        m_ShowImg.enabled = true;
        m_ExperimentStartTime = m_StartUIAppearTime;
    }

    // Update is called once per frame
    void Update()
    {
        int endGroupIdx = m_TestSprites.Length / 2 - 1;
        float nextGroupStartTime = m_ExperimentStartTime + (m_CurGroupIdx + 1) * (m_ImgGapTime * 2 + m_PauseUIAppearTime);
        if (m_CurGroupIdx < endGroupIdx && FloatEqual(Time.time, nextGroupStartTime))
        {
            if(++m_CurGroupIdx == 0) m_StartUI.SetActive(false);
            StartCoroutine(ShowImageGroup(m_CurGroupIdx == endGroupIdx));
        }
        ListenUserInput();
    }

    IEnumerator ShowImageGroup(bool isLastGroup)
    {
        //m_ShowImg.enabled = true;
        m_ShowImg.sprite = m_TestSprites[m_CurGroupIdx * 2];
        yield return new WaitForSeconds(m_ImgGapTime);
        m_ShowImg.sprite = m_TestSprites[m_CurGroupIdx * 2 + 1];
        yield return new WaitForSeconds(m_ImgGapTime);
        //m_ShowImg.enabled = false;
        m_PauseUI.SetActive(true);
        yield return new WaitForSeconds(m_PauseUIAppearTime);
        m_PauseUI.SetActive(false);
        m_FinishUI.SetActive(isLastGroup);
        if (isLastGroup) m_ShowImg.sprite = null;
    }

    void ListenUserInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Debug.Log("用户选择第一张图片");
            m_UserChoices[m_CurGroupIdx] = 1;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Debug.Log("用户选择第二张图片");
            m_UserChoices[m_CurGroupIdx] = 2;
        }
    }


    bool FloatEqual(float a, float b, float epsilon = 0.01f)
    {
        return Mathf.Abs(a - b) < epsilon;
    }
    
    void LoadImg()
    {
        m_TestSprites = Resources.LoadAll<Sprite>(m_TestImgPath);
        m_UserChoices = new int[m_TestSprites.Length / 2];
    }
}
