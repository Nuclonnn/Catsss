using System.IO;
using UnityEditor;
using UnityEngine;

namespace Catsss.EditorTools
{
    /// <summary>
    /// CFXR (.cfxrshader) должен компилироваться под URP. Если пакет импортировали до настройки URP —
    /// материалы молний розовые. Этот пункт принудительно пересобирает шейдеры.
    /// </summary>
    public static class CfxrUrpRepairMenu
    {
        private const int ForceUniversalRenderPipeline = 2;

        [MenuItem("Catsss/Repair CFXR Shaders (URP)")]
        public static void RepairCfxrShaders()
        {
            string[] shaderPaths = Directory.GetFiles(Application.dataPath, "*.cfxrshader", SearchOption.AllDirectories);

            if (shaderPaths.Length == 0)
            {
                Debug.LogWarning("[CFXR Repair] Не найдены .cfxrshader. Установи Cartoon FX Remaster в Assets/JMO Assets/.");
                return;
            }

            int repaired = 0;

            foreach (string absolutePath in shaderPaths)
            {
                string assetPath = "Assets" + absolutePath.Substring(Application.dataPath.Length).Replace('\\', '/');
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);

                if (importer == null)
                {
                    continue;
                }

                SerializedObject serializedImporter = new SerializedObject(importer);
                SerializedProperty pipelineProperty = serializedImporter.FindProperty("renderPipelineDetection");

                if (pipelineProperty != null)
                {
                    pipelineProperty.intValue = ForceUniversalRenderPipeline;
                    serializedImporter.ApplyModifiedPropertiesWithoutUndo();
                }

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                repaired++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CFXR Repair] Переимпортировано шейдеров: {repaired}. Проверь Console на ошибки компиляции и превью CFXR Electrified 3.");
        }

        [MenuItem("Catsss/Validate Charge Projectile VFX")]
        public static void ValidateChargeProjectileVfx()
        {
            const string electrifiedPrefab = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Electric/CFXR Electrified 3.prefab";
            const string glowMaterial = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/cfxr proc glow soft add.mat";

            ValidateShaderOnAsset(electrifiedPrefab);
            ValidateShaderOnAsset(glowMaterial);
            ValidateShaderOnAsset("Assets/Prefabs/ChargeProjectileRoot.prefab");
        }

        private static void ValidateShaderOnAsset(string assetPath)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);

            if (asset == null)
            {
                Debug.LogError($"[CFXR Repair] Не найден ассет: {assetPath}");
                return;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (material != null)
            {
                LogMaterialShader(assetPath, material);
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab == null)
            {
                Debug.LogWarning($"[CFXR Repair] Пропуск (не Material/Prefab): {assetPath}");
                return;
            }

            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material rendererMaterial in renderer.sharedMaterials)
                {
                    if (rendererMaterial != null)
                    {
                        LogMaterialShader($"{assetPath} → {renderer.name}", rendererMaterial);
                    }
                }
            }

            foreach (ParticleSystem particleSystem in prefab.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();

                if (particleRenderer == null)
                {
                    continue;
                }

                foreach (Material particleMaterial in particleRenderer.sharedMaterials)
                {
                    if (particleMaterial != null)
                    {
                        LogMaterialShader($"{assetPath} → {particleSystem.name}", particleMaterial);
                    }
                }
            }
        }

        private static void LogMaterialShader(string context, Material material)
        {
            Shader shader = material.shader;
            bool broken = shader == null || shader.name.Contains("InternalError") || shader.name.Contains("Error");

            if (broken)
            {
                Debug.LogError($"[CFXR Repair] Сломанный шейдер у '{material.name}' ({context}): {(shader != null ? shader.name : "null")}", material);
                return;
            }

            Debug.Log($"[CFXR Repair] OK: {material.name} → {shader.name} ({context})", material);
        }
    }
}
