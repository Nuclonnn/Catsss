using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Catsss.EditorTools
{
    /// <summary>
    /// Сборка MouseLocomotion.controller, loop-клипов и Avatar на префабе мыши.
    /// Вызывается из меню и из AssetPostprocessor после реимпорта FBX.
    /// </summary>
    public static class MouseLocomotionEditorUtility
    {
        public const string MouseFbxPath = "Assets/Models/Mouse/source/mouse model.fbx";
        public const string ControllerPath = "Assets/Animations/MouseLocomotion.controller";
        public const string MouseRootPrefabPath = "Assets/Prefabs/Mouse/MouseRoot.prefab";

        public static void RepairAll(bool logResult = true)
        {
            bool avatarReady = TryEnsureAvatarInFbx(out string avatarImportMessage);
            int loopedClips = FixMouseAnimationLoop();
            bool controllerBuilt = TryBuildController(out string controllerMessage);
            string avatarMessage = avatarImportMessage;
            bool avatarAssigned = avatarReady && TryAssignPrefabAvatar(out avatarMessage);

            if (!logResult)
            {
                return;
            }

            if (controllerBuilt && avatarAssigned)
            {
                Debug.Log(
                    $"[Mouse Locomotion] Repair complete.\n" +
                    $"  Loop clips: {loopedClips}\n" +
                    $"  Controller: {controllerMessage}\n" +
                    $"  Avatar: {avatarMessage}");
            }
            else
            {
                Debug.LogWarning(
                    $"[Mouse Locomotion] Repair partial.\n" +
                    $"  Loop clips: {loopedClips}\n" +
                    $"  Controller: {controllerMessage}\n" +
                    $"  Avatar: {avatarMessage}");
            }
        }

        /// <summary>
        /// Generic-риг должен импортироваться с Create From This Model, иначе Avatar sub-asset не создаётся.
        /// </summary>
        public static bool TryEnsureAvatarInFbx(out string message)
        {
            Avatar avatar = FindMouseAvatarAsset();
            if (avatar != null)
            {
                message = $"Avatar '{avatar.name}' already in FBX.";
                return true;
            }

            ModelImporter importer = AssetImporter.GetAtPath(MouseFbxPath) as ModelImporter;
            if (importer == null)
            {
                message = $"ModelImporter not found: {MouseFbxPath}";
                return false;
            }

            bool needsReimport =
                importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                importer.animationType != ModelImporterAnimationType.Generic ||
                !importer.importAnimation;

            if (needsReimport)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.SaveAndReimport();
            }
            else
            {
                AssetDatabase.ImportAsset(MouseFbxPath, ImportAssetOptions.ForceUpdate);
            }

            avatar = FindMouseAvatarAsset();
            if (avatar == null)
            {
                message =
                    "Avatar still missing after reimport. In Inspector on mouse model.fbx set " +
                    "Rig: Generic + Create From This Model, then Apply.";
                return false;
            }

            message = $"Avatar '{avatar.name}' created in FBX.";
            return true;
        }

        private static Avatar FindMouseAvatarAsset()
        {
            return AssetDatabase.LoadAllAssetsAtPath(MouseFbxPath).OfType<Avatar>().FirstOrDefault();
        }

        public static int FixMouseAnimationLoop()
        {
            int fixedCount = 0;

            foreach (AnimationClip clip in LoadAnimationClips(MouseFbxPath))
            {
                if (EnsureClipLoops(clip))
                {
                    fixedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            return fixedCount;
        }

        public static bool TryBuildController(out string message)
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(MouseFbxPath))
            {
                message = $"FBX not found: {MouseFbxPath}";
                return false;
            }

            List<AnimationClip> clips = LoadAnimationClips(MouseFbxPath);
            if (clips.Count == 0)
            {
                message = "No AnimationClips in mouse FBX. Check Rig = Generic and Import Animation.";
                return false;
            }

            AnimationClip runClip = FindClip(clips, "run", "walk", "locomotion", "move", "cycle");
            AnimationClip idleClip = FindClip(clips, "idle", "idol", "rest", "stand");

            if (runClip == null)
            {
                runClip = clips[0];
            }

            if (idleClip == null)
            {
                idleClip = runClip;
            }

            EnsureClipLoops(idleClip);
            EnsureClipLoops(runClip);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            BuildLocomotionController(controller, idleClip, runClip);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            message = $"idle='{idleClip.name}', run='{runClip.name}'";
            return true;
        }

        public static bool TryAssignPrefabAvatar(out string message)
        {
            Avatar avatar = FindMouseAvatarAsset();

            if (avatar == null)
            {
                message = "Avatar sub-asset not found in mouse FBX. Run Repair again after reimport.";
                return false;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MouseRootPrefabPath);
            bool changed = false;

            try
            {
                foreach (Animator animator in prefabRoot.GetComponentsInChildren<Animator>(true))
                {
                    if (animator.avatar == avatar)
                    {
                        continue;
                    }

                    animator.avatar = avatar;
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, MouseRootPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            message = changed ? $"Assigned '{avatar.name}' on MouseRoot." : "Avatar already assigned.";
            return true;
        }

        private static void BuildLocomotionController(
            AnimatorController controller,
            AnimationClip idleClip,
            AnimationClip walkClip)
        {
            controller.parameters = new[]
            {
                new AnimatorControllerParameter
                {
                    name = "Speed",
                    type = AnimatorControllerParameterType.Float,
                    defaultFloat = 0f
                }
            };

            AnimatorControllerLayer layer = controller.layers[0];
            layer.name = "Base Layer";

            AnimatorStateMachine stateMachine = layer.stateMachine;
            ClearStateMachine(stateMachine);

            BlendTree blendTree = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };

            blendTree.AddChild(idleClip, 0f);
            blendTree.AddChild(walkClip, 1f);

            ChildMotion[] children = blendTree.children;
            if (children.Length >= 2 && ReferenceEquals(idleClip, walkClip))
            {
                children[0].timeScale = 1f;
                children[1].timeScale = 1.25f;
                blendTree.children = children;
            }

            AssetDatabase.AddObjectToAsset(blendTree, controller);

            AnimatorState locomotionState = stateMachine.AddState("Locomotion", new Vector3(300f, 120f, 0f));
            locomotionState.motion = blendTree;
            stateMachine.defaultState = locomotionState;
        }

        private static void ClearStateMachine(AnimatorStateMachine stateMachine)
        {
            foreach (ChildAnimatorState childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            foreach (ChildAnimatorStateMachine childMachine in stateMachine.stateMachines.ToArray())
            {
                stateMachine.RemoveStateMachine(childMachine.stateMachine);
            }
        }

        public static List<AnimationClip> LoadAnimationClips(string assetPath)
        {
            return AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .Where(clip => clip != null && !clip.name.StartsWith("__preview", StringComparison.Ordinal))
                .ToList();
        }

        public static bool EnsureClipLoops(AnimationClip clip)
        {
            if (clip == null)
            {
                return false;
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);

            if (settings.loopTime && clip.isLooping)
            {
                return false;
            }

            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return true;
        }

        public static AnimationClip FindClip(List<AnimationClip> clips, params string[] keywords)
        {
            foreach (string keyword in keywords)
            {
                AnimationClip match = clips.FirstOrDefault(clip =>
                    clip.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
