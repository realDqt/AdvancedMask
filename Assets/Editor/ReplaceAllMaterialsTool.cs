using UnityEngine;
using UnityEditor;

public class ReplaceAllMaterialsTool : EditorWindow
{
    Material newMat;

    [MenuItem("Tools/Replace All Materials In Children %#r")]   // Ctrl+Shift+R 快捷键
    static void Open()
    {
        // 弹出小窗口
        GetWindow<ReplaceAllMaterialsTool>("Replace Materials");
    }

    void OnGUI()
    {
        newMat = (Material)EditorGUILayout.ObjectField("New Material", newMat, typeof(Material), false);

        if (GUILayout.Button("Replace All Selected + Children"))
        {
            if (newMat == null)
            {
                EditorUtility.DisplayDialog("提示", "请先拖一个材质！", "OK");
                return;
            }

            int count = 0;
            foreach (GameObject go in Selection.gameObjects)
            {
                foreach (Renderer rd in go.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] mats = rd.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = newMat;
                    rd.sharedMaterials = mats;
                    count++;
                }
            }

            Debug.Log($"已替换 {count} 个 Renderer 的材质为：{newMat.name}");
            SceneView.RepaintAll();   // 立即刷新 Scene 视图
        }
    }
}