using UnityEngine;

/// <summary>
/// 把当前物体上所有材质的 Shader 统一替换成指定的 Shader。
/// 会自动克隆材质，避免影响其它物体。
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ShaderReplacer : MonoBehaviour
{
    [Tooltip("想要替换成的目标 Shader")]
    public Shader targetShader;

    // 记录原始材质，便于在编辑器下恢复
    private Material[] originalMaterials;

    private void Awake()
    {
        if (Application.isPlaying)
            ReplaceShaders();
    }

    /// <summary>
    /// 执行替换：克隆并换 Shader
    /// </summary>
    [ContextMenu("Replace Shaders")]
    public void ReplaceShaders()
    {
        if (targetShader == null) return;

        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        // 第一次记录原始引用
        if (originalMaterials == null)
            originalMaterials = renderer.sharedMaterials;

        Material[] newMats = new Material[renderer.sharedMaterials.Length];
        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            if (renderer.sharedMaterials[i] == null) continue;

            // 克隆
            newMats[i] = new Material(renderer.sharedMaterials[i])
            {
                shader = targetShader
            };
        }
        renderer.materials = newMats;
    }

    /// <summary>
    /// 还原到原始材质
    /// </summary>
    [ContextMenu("Revert")]
    public void Revert()
    {
        if (originalMaterials == null) return;
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        // 释放克隆的材质
        foreach (var mat in renderer.materials)
            if (mat != null && !Application.isPlaying)
                DestroyImmediate(mat);
            else if (mat != null)
                Destroy(mat);

        renderer.sharedMaterials = originalMaterials;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        // 拖拽脚本或 Reset 时自动执行一次
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
                ReplaceShaders();
        };
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && gameObject.activeInHierarchy)
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                    ReplaceShaders();
            };
    }
#endif
}