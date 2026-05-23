using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Catsss.Rendering
{
    /// <summary>
    /// URP Renderer Feature (Render Graph): контур объектов с Rendering Layer InteractableOutline.
    /// </summary>
    public sealed class InteractableOutlineRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public sealed class OutlineSettings
        {
            [Tooltip("До отрисовки непрозрачных — основной меш перекроет центр, останется кольцо.")]
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;

            public Material outlineMaterial;

            [Tooltip("Маска Rendering Layer. По умолчанию 2 = bit 1 (InteractableOutline).")]
            public uint renderingLayerMask = InteractableOutlineRendering.LayerMask;
        }

        [SerializeField] private OutlineSettings settings = new();

        private InteractableOutlinePass _pass;

        public override void Create()
        {
            _pass = new InteractableOutlinePass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.outlineMaterial == null || _pass == null)
            {
                return;
            }

            _pass.UpdateSettings(settings);
            renderer.EnqueuePass(_pass);
        }

        private sealed class InteractableOutlinePass : ScriptableRenderPass
        {
            // Lit/прочие URP-шейдеры рисуются с этими LightMode; overrideMaterial подменяет шейдер на outline.
            private static readonly List<ShaderTagId> ShaderTags = new()
            {
                new("UniversalForward"),
                new("UniversalForwardOnly"),
                new("SRPDefaultUnlit"),
            };

            private OutlineSettings _settings;

            public InteractableOutlinePass(OutlineSettings settings)
            {
                _settings = settings;
                renderPassEvent = settings.renderPassEvent;
                profilingSampler = new ProfilingSampler(nameof(InteractableOutlinePass));
            }

            public void UpdateSettings(OutlineSettings settings)
            {
                _settings = settings;
                renderPassEvent = settings.renderPassEvent;
            }

            private sealed class PassData
            {
                public RendererListHandle RendererListHandle;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                if (_settings.outlineMaterial == null)
                {
                    return;
                }

                using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(
                    nameof(InteractableOutlinePass),
                    out PassData passData);

                UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
                UniversalLightData lightData = frameContext.Get<UniversalLightData>();
                UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();

                SortingCriteria sortFlags = cameraData.defaultOpaqueSortFlags;
                FilteringSettings filterSettings = new(
                    RenderQueueRange.opaque,
                    -1,
                    _settings.renderingLayerMask);

                DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(
                    ShaderTags,
                    renderingData,
                    cameraData,
                    lightData,
                    sortFlags);

                drawSettings.overrideMaterial = _settings.outlineMaterial;
                drawSettings.overrideMaterialPassIndex = 0;

                RendererListParams rendererListParameters = new(
                    renderingData.cullResults,
                    drawSettings,
                    filterSettings);

                passData.RendererListHandle = renderGraph.CreateRendererList(rendererListParameters);

                builder.UseRendererList(passData.RendererListHandle);

                // URP 17.4: activeColorTexture / activeDepthTexture (не cameraColorTexture).
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.RendererListHandle);
                });
            }
        }
    }
}
