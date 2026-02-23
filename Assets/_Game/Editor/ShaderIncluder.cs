using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace PrismPulse.Editor
{
    /// <summary>
    /// Ensures required shaders are included in builds.
    /// Runs automatically before build via InitializeOnLoad.
    /// Shaders found via Shader.Find() at runtime get stripped unless referenced.
    /// </summary>
    [InitializeOnLoad]
    public static class ShaderIncluder
    {
        private static readonly string[] RequiredShaders =
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Particles/Unlit",
        };

        static ShaderIncluder()
        {
            EnsureShadersIncluded();
        }

        [MenuItem("PrismPulse/Ensure Shaders Included")]
        public static void EnsureShadersIncluded()
        {
            var graphicsSettings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var so = new SerializedObject(graphicsSettings);
            var arrayProp = so.FindProperty("m_AlwaysIncludedShaders");

            var existing = new HashSet<string>();
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var shader = arrayProp.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (shader != null)
                    existing.Add(shader.name);
            }

            bool changed = false;
            foreach (var shaderName in RequiredShaders)
            {
                if (existing.Contains(shaderName))
                    continue;

                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogWarning($"[ShaderIncluder] Shader '{shaderName}' not found, skipping.");
                    continue;
                }

                int index = arrayProp.arraySize;
                arrayProp.InsertArrayElementAtIndex(index);
                arrayProp.GetArrayElementAtIndex(index).objectReferenceValue = shader;
                changed = true;
                Debug.Log($"[ShaderIncluder] Added '{shaderName}' to Always Included Shaders.");
            }

            if (changed)
                so.ApplyModifiedProperties();
        }
    }
}
