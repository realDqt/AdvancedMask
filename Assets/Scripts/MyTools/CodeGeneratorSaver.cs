// 文件名：CodeGeneratorSaver.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class CodeGeneratorSaver : MonoBehaviour
{
    [Tooltip("输出文件名，放在 persistentDataPath 下")]
    public string fileName = "D:\\DALAB\\Research\\Output\\RandomCodes.txt";

    private void Start()
    {
        List<string> codes = GenerateCodes(36);
        SaveToFile(codes);
    }

    /// <summary>
    /// 生成 n 个 “3字母+1数字” 字符串，4 个位置各出现 n/4 次数字
    /// </summary>
    private List<string> GenerateCodes(int n)
    {
        const int len = 4;            // 4 位
        int digitPerPos = n / len;    // 每个位置数字出现次数（36/4=9）

        List<string> result = new List<string>(n);
        char[] letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        char[] digits  = "0123456789".ToCharArray();
        System.Random rng = new System.Random();

        // 1. 先把 36 个位置全设为字母
        char[][] chars = new char[n][];
        for (int i = 0; i < n; i++)
        {
            chars[i] = new char[len];
            for (int p = 0; p < len; p++)
                chars[i][p] = letters[rng.Next(letters.Length)];
        }

        // 2. 按位置依次把 9 个字母换成数字
        for (int pos = 0; pos < len; pos++)
        {
            // 随机挑 9 行
            int[] rows = Enumerable.Range(0, n).OrderBy(_ => rng.Next()).Take(digitPerPos).ToArray();
            foreach (int r in rows)
                chars[r][pos] = digits[rng.Next(digits.Length)];
        }

        // 3. 组装字符串
        for (int i = 0; i < n; i++)
            result.Add(new string(chars[i]));

        return result;
    }

    /// <summary>
    /// 保存列表到本地 txt
    /// </summary>
    private void SaveToFile(List<string> codes)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllLines(path, codes);
        Debug.Log($"已生成并保存 36 个随机字符串到：{path}");
    }
}