using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Catsss.EditorTools
{
    /// <summary>
    /// После реимпорта mouse model.fbx пересобирает клипы/контроллер, чтобы ссылки не ломались.
    /// </summary>
    [InitializeOnLoad]
    public sealed class MouseModelImportPostprocessor : AssetPostprocessor
    {
        private static bool _repairQueued;
        private static bool _startupRepairQueued;

        static MouseModelImportPostprocessor()
        {
            EditorApplication.delayCall += QueueStartupRepair;
        }

        private static void QueueStartupRepair()
        {
            if (_startupRepairQueued)
            {
                return;
            }

            _startupRepairQueued = true;
            EditorApplication.delayCall += RunStartupRepair;
        }

        private static void RunStartupRepair()
        {
            EditorApplication.delayCall -= RunStartupRepair;

            if (!NeedsRepair())
            {
                return;
            }

            MouseLocomotionEditorUtility.RepairAll(logResult: true);
        }

        private static bool NeedsRepair()
        {
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(MouseLocomotionEditorUtility.MouseRootPrefabPath);
            Animator animator = prefabRoot != null
                ? prefabRoot.GetComponentInChildren<Animator>(true)
                : null;

            if (animator == null || animator.avatar == null)
            {
                return true;
            }

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(MouseLocomotionEditorUtility.ControllerPath);

            if (controller == null)
            {
                return true;
            }

            Motion motion = controller.layers[0].stateMachine.defaultState?.motion;
            if (motion is BlendTree blendTree)
            {
                return blendTree.children == null || blendTree.children.Length == 0;
            }

            return motion == null;
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (path == MouseLocomotionEditorUtility.MouseFbxPath)
                {
                    QueueRepair();
                    return;
                }
            }
        }

        private static void QueueRepair()
        {
            if (_repairQueued)
            {
                return;
            }

            _repairQueued = true;
            EditorApplication.delayCall += RunQueuedRepair;
        }

        private static void RunQueuedRepair()
        {
            _repairQueued = false;
            EditorApplication.delayCall -= RunQueuedRepair;
            MouseLocomotionEditorUtility.RepairAll(logResult: false);
        }
    }
}
