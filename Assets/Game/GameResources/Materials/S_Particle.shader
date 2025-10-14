Shader "NorseFX/Particles/HDR Particle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR]_Color("Color", color) = (1,1,1,1)
        [Toggle]_AlphaMode("Premultiplied alpha", int) = 1
        [Toggle]_SoftParticles("Soft Particles", int) = 0
        _InvFade("Soft Particles Factor", Range(0.01,3.0)) = 1.0

            [Space(50)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source blend mode", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination blend mode", Float) = 10
        [Toggle] _ZWrite("Z-write", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)]_CullMode("Culling mode", int) = 0

             [Space(50)]
        [Enum(UnityEngine.Rendering.CompareFunction)]_StencilComp("Stencil Comparison", Float) = 0
        _Stencil("Stencil ID", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)]_StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
             [Space(50)]
        _FogBlendColor("Fog blend color", color) = (0,0,0,1)

    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "IgnoreProjector" = "True" "Queue" = "Transparent" "PreviewType" = "Plane"}
        Blend[_SrcBlend][_DstBlend]
        Cull[_CullMode]
        ZWrite[_ZWrite]

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                UNITY_FOG_COORDS(1)
                float4 projPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST, _Color, _FogBlendColor;
            int _AlphaMode, _SoftParticles;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float _InvFade;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.projPos = ComputeScreenPos(o.vertex);
                COMPUTE_EYEDEPTH(o.projPos.z);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
                float partZ = i.projPos.z;
                float fade = saturate(_InvFade * (sceneZ - partZ));
                i.color.a *= lerp(1, fade, _SoftParticles);

                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                col = lerp(col, col * col.a, _AlphaMode);
                UNITY_APPLY_FOG_COLOR(i.fogCoord, col, _FogBlendColor);
                return col;
            }
            ENDCG
        }
    }
}
