Shader "Hidden/Evo/UI/Soft Mask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        // Soft Mask Parameters
        [HideInInspector] _SoftMaskSupport ("Soft Mask Support", Float) = 1
        [HideInInspector] _SoftMask_Count ("Soft Mask Count", Float) = 0
        [HideInInspector] _SoftMaskTex0 ("Soft Mask 0", 2D) = "white" {}
        [HideInInspector] _SoftMaskTex1 ("Soft Mask 1", 2D) = "white" {}
        [HideInInspector] _SoftMaskTex2 ("Soft Mask 2", 2D) = "white" {}
        [HideInInspector] _SoftMaskTex3 ("Soft Mask 3", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "SoftMask.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ ETC1_EXTERNAL_ALPHA

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex    : SV_POSITION;
                float4 color     : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 canvasPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            float _EnableExternalAlpha;
            float4 _Color;
            float4 _TextureSampleAdd;
            float4 _ClipRect;

            // Engine Polynomial Sync
            float _UIVertexColorAlwaysGammaSpace;

            inline float3 UI_GammaToLinear(float3 color)
            {
                float3 linearPath = color * 0.084971 - 0.000163;
                float3 gammaPath = color * (color * (color * 0.265885 + 0.736584) - 0.009802) + 0.003197;
                float3 comparison = step(0.072549, color);
                return lerp(linearPath, gammaPath, comparison);
            }

            inline float4 UI_ColorSpaceSync(float4 color)
            {
            #if !defined(UNITY_COLORSPACE_GAMMA)
                if (_UIVertexColorAlwaysGammaSpace > 0.5)
                    color.rgb = UI_GammaToLinear(color.rgb);
            #endif
                return color;
            }

            inline half4 SampleSpriteTexture(float2 uv)
            {
                half4 color = tex2D(_MainTex, uv);

            #if ETC1_EXTERNAL_ALPHA
                half4 alpha = tex2D(_AlphaTex, uv);
                color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);
            #endif

                return color;
            }

            v2f vert(appdata_t vertex)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(vertex);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.canvasPos = vertex.vertex;
                output.vertex = UnityObjectToClipPos(vertex.vertex);
                output.texcoord = vertex.texcoord;
                output.color = UI_ColorSpaceSync(vertex.color) * _Color;
                return output;
            }

            float4 frag(v2f input) : SV_Target
            {
                half4 color = (SampleSpriteTexture(input.texcoord) + _TextureSampleAdd) * input.color;

                SoftMask_Apply(color, input.canvasPos);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.canvasPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}