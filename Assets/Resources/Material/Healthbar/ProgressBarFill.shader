Shader "UI/ProgressBarFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Fill ("Fill Percent", Range(0, 1)) = 1
        [Enum(Horizontal, 0, Vertical, 1)] _Mode ("Mode", Float) = 0
        [Toggle] _Invert ("Invert Direction", Float) = 0

        // Local-space bounds of the graphic: xy = min corner, zw = size.
        // Set by ProgressBarFillDriver. When zw is 0, UVs are used instead.
        _LocalBounds ("Local Bounds (xy min, zw size)", Vector) = (0, 0, 0, 0)

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "ProgressBarFill"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Fill;
                half _Mode;
                half _Invert;
                float4 _LocalBounds;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 localPos : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                OUT.localPos = IN.positionOS.xy;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Normalize local position to 0..1 across the rect on the chosen axis.
                // Falls back to UVs when no local bounds were provided.
                bool hasBounds = _LocalBounds.z > 0.0 && _LocalBounds.w > 0.0;
                float2 normalized = hasBounds
                    ? (IN.localPos - _LocalBounds.xy) / _LocalBounds.zw
                    : IN.uv;
                float t = (_Mode < 0.5) ? normalized.x : normalized.y;

                if (_Invert > 0.5)
                {
                    t = 1.0 - t;
                }

                // Wipe: keep pixels from 0 up to the fill percentage
                if (_Fill <= 0.0 || t > _Fill)
                {
                    discard;
                }

                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
            }
            ENDHLSL
        }
    }
}
