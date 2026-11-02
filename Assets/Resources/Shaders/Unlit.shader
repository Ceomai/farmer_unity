// THREE.MeshBasicMaterial karşılığı (ışıksız, dokulu, alphaTest, iki yüzlü seçilebilir, sisli).
// toneMapped:false olan tabelalar için ton eşleme yapılmaz; _ToneMap=1 verilirse ACES uygulanır.
Shader "Ciftlik/Unlit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Map", 2D) = "white" {}
        _Opacity ("Opacity", Range(0,1)) = 1
        _AlphaTest ("Alpha Test", Range(0,1)) = 0
        _ToneMap ("Tone Map", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "IgnoreProjector"="True" }
        Pass
        {
            Cull [_Cull] ZWrite [_ZWrite] Blend [_SrcBlend] [_DstBlend]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; float _Opacity; float _AlphaTest; float _ToneMap; float _CfExposure;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert (appdata_base v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex); UNITY_TRANSFER_FOG(o, o.pos); return o; }
            float3 Fit(float3 v) { float3 a = v * (v + 0.0245786) - 0.000090537; float3 b = v * (0.983729 * v + 0.4329510) + 0.238081; return a / b; }
            float3 Aces(float3 c)
            {
                const float3x3 I = float3x3(0.59719, 0.35458, 0.04823, 0.07600, 0.90834, 0.01566, 0.02840, 0.13383, 0.83777);
                const float3x3 O = float3x3(1.60475, -0.53108, -0.07367, -0.10208, 1.10813, -0.00605, -0.00327, -0.07276, 1.07602);
                c *= _CfExposure / 0.6; return saturate(mul(O, Fit(mul(I, c))));
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                c.a *= _Opacity;
                clip(c.a - _AlphaTest);
                if (_ToneMap > 0.5) c.rgb = Aces(c.rgb);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
