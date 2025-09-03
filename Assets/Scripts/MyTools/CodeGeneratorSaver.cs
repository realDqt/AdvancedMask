using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public class CodeGeneratorSaver : MonoBehaviour
{
    // 存储所有生成的字符串
    private List<string>[] allStrings = new List<string>[4];
    // 存储所有字符串的总列表
    private List<string> allStringsCombined = new List<string>();

    void Start()
    {
        // 初始化列表
        for (int i = 0; i < 4; i++)
        {
            allStrings[i] = new List<string>();
        }

        // 生成各类字符串
        GenerateStringsWithNumberAtPosition(0, 9);  // 数字在第1个位置(索引0)
        GenerateStringsWithNumberAtPosition(1, 9);  // 数字在第2个位置(索引1)
        GenerateStringsWithNumberAtPosition(2, 9);  // 数字在第3个位置(索引2)
        GenerateStringsWithNumberAtPosition(3, 9);  // 数字在第4个位置(索引3)

        // 合并所有字符串
        CombineAllStrings();
        
        // 使用洗牌算法打乱顺序
        ShuffleStrings();

        // 打印结果
        PrintResults();

        // 保存到本地文件
        SaveToFile();
    }

    /// <summary>
    /// 生成指定数量的字符串，其中数字位于指定位置
    /// </summary>
    /// <param name="numberPosition">数字所在的位置(0-3)</param>
    /// <param name="count">要生成的数量</param>
    void GenerateStringsWithNumberAtPosition(int numberPosition, int count)
    {
        if (numberPosition < 0 || numberPosition > 3)
        {
            Debug.LogError("数字位置必须在0-3之间");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            char[] chars = new char[4];
            
            for (int j = 0; j < 4; j++)
            {
                if (j == numberPosition)
                {
                    // 生成随机数字(0-9)
                    chars[j] = (char)('0' + UnityEngine.Random.Range(0, 10));
                }
                else
                {
                    chars[j] = (char)('A' + UnityEngine.Random.Range(0, 26));
                }
            }
            
            allStrings[numberPosition].Add(new string(chars));
        }
    }

    /// <summary>
    /// 合并所有字符串到一个列表
    /// </summary>
    void CombineAllStrings()
    {
        allStringsCombined.Clear();
        foreach (var list in allStrings)
        {
            allStringsCombined.AddRange(list);
        }
    }

    /// <summary>
    /// 使用Fisher-Yates洗牌算法打乱字符串顺序
    /// </summary>
    void ShuffleStrings()
    {
        System.Random rng = new System.Random();
        int n = allStringsCombined.Count;
        
        // Fisher-Yates洗牌算法
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            // 交换元素
            string value = allStringsCombined[k];
            allStringsCombined[k] = allStringsCombined[n];
            allStringsCombined[n] = value;
        }
    }

    /// <summary>
    /// 打印所有生成的字符串
    /// </summary>
    void PrintResults()
    {
        Debug.Log("===== 原始分组字符串 =====");
        for (int i = 0; i < 4; i++)
        {
            Debug.Log($"----- 数字在第{i + 1}个位置的字符串 -----");
            foreach (string str in allStrings[i])
            {
                Debug.Log(str);
            }
        }

        Debug.Log("\n===== 打乱顺序后的字符串 =====");
        foreach (string str in allStringsCombined)
        {
            Debug.Log(str);
        }
    }

    /// <summary>
    /// 将打乱顺序后的字符串保存到本地TXT文件
    /// </summary>
    void SaveToFile()
    {
        try
        {
            // 获取保存路径 - 桌面
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "RandomStrings.txt");

            // 写入文件
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (string str in allStringsCombined)
                {
                    writer.WriteLine(str);
                }
            }

            Debug.Log($"成功保存到文件: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"保存文件失败: {e.Message}");
        }
    }
}