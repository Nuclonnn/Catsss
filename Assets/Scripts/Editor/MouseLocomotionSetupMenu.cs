using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Catsss.EditorTools
{
    /// <summary>Создаёт MouseLocomotion.controller (idle/walk Blend Tree) из клипов в FBX мыши.</summary>
    public static class MouseLocomotionSetupMenu
    {
        private const string MouseFbxPath = "Assets/Models/Mouse/source/mouse model.fbx";
        private const string ControllerPath = "Assets/Animations/MouseLocomotion.controller";

        [MenuItem("Catsss/Fix Mouse Animation Loop")]
        public static void FixMouseAnimationLoop()
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
            Debug.Log($"[Mouse Locomotion] Loop включён на {fixedCount} клип(ах) в {MouseFbxPath}.");
        }

        [MenuItem("Catsss/Setup Mouse Locomotion Controller")]
        public static void SetupMouseLocomotionController()
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(MouseFbxPath))
            {
                Debug.LogError($"[Mouse Locomotion] Не найден FBX: {MouseFbxPath}");
                return;
            }

            List<AnimationClip> clips = LoadAnimationClips(MouseFbxPath);

            if (clips.Count == 0)
            {
                Debug.LogError(
                    "[Mouse Locomotion] В FBX нет AnimationClip. Выбери модель → Rig: Generic/Humanoid, Animation Type: Import, Apply.");
                return;
            }

            AnimationClip walkClip = FindClip(clips, "walk", "run", "locomotion", "move");
            AnimationClip idleClip = FindClip(clips, "idle", "rest", "stand");

            if (walkClip == null)
            {
                walkClip = clips[0];
                Debug.LogWarning(
                    $"[Mouse Locomotion] Клип walk не найден по имени — используем '{walkClip.name}'. Переименуй клип в FBX или поправь Blend Tree вручную.");
            }

            if (idleClip == null)
            {
                idleClip = walkClip;
                Debug.LogWarning(
                    $"[Mouse Locomotion] Клип idle не найден — на Speed=0 будет тот же клип '{idleClip.name}' (замри на месте через Speed=0).");
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            EnsureClipLoops(idleClip);
            EnsureClipLoops(walkClip);

            BuildLocomotionController(controller, idleClip, walkClip);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[Mouse Locomotion] Готово: {ControllerPath}\n" +
                $"  idle = {idleClip.name}\n" +
                $"  walk = {walkClip.name}\n" +
                "Назначь Controller на Animator модели мыши в MouseRoot.");
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

        private static List<AnimationClip> LoadAnimationClips(string assetPath)
        {
            return AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .Where(clip => clip != null && !clip.name.StartsWith("__preview", StringComparison.Ordinal))
                .ToList();
        }

        private static bool EnsureClipLoops(AnimationClip clip)
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

        private static AnimationClip FindClip(List<AnimationClip> clips, params string[] keywords)
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
