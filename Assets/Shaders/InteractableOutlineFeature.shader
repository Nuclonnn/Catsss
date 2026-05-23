Shader "Catsss/InteractableOutlineFeature"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.9, 0.1, 1)
        _OutlineWidth ("Outline Width (object space)", Range(0.001, 0.15)) = 0.025
        [HideInInspector] _ExtrusionCenterOS ("Extrusion Center OS", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry-1"
        }

        Pass
        {
            Name "InteractableOutline"
            Tags { "LightMode" = "InteractableOutline" }

            Cull Front
            ZWrite On
            ZTest LEqual
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float4 _ExtrusionCenterOS;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                // Не по normalOS: на боксе грани разъезжаются, углы пустые, крышка «висит» выше боков.
                // Радиально от центра bounds — углы замыкаются (как «надутый» меш).
                float3 fromCenter = input.positionOS.xyz - _ExtrusionCenterOS.xyz;
                float dist = length(fromCenter);
                float3 extrudeDir = dist > 1e-4 ? fromCenter / dist : normalize(input.normalOS);
                float3 extrudedPositionOS = input.positionOS.xyz + extrudeDir * _OutlineWidth;

                Varyings output;
                output.positionCS = TransformObjectToHClip(extrudedPositionOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
