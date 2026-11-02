// THREE.MeshToonMaterial (r147) karşılığı — Built-in render pipeline.
// Işık: yarıküre ışığı (RenderSettings Trilight: unity_AmbientSky/Ground) + yönlü güneş (ForwardBase, gölgeli)
//       + nokta ışıklar (ForwardAdd, Three r147 "decay=2" uzaklık zayıflaması).
// Toon basamakları: JS GRAD dokusu [96,150,205,255] (4 texel, en yakın örnekleme) → global _CfGrad.
// Ton eşleme: ACESFilmic (Three sürümü), pozlama _CfExposure (R.toneMappingExposure = 1.08). Sis tonlamadan sonra (Three sırası).
// Proje "Linear" renk uzayında olmalı (Three outputEncoding = sRGB).
Shader "Ciftlik/Toon"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Map", 2D) = "white" {}
        _EmissionColor ("Emissive", Color) = (0,0,0,1)
        _EmissionIntensity ("Emissive Intensity", Float) = 1
        _Opacity ("Opacity", Range(0,1)) = 1
        _AlphaTest ("Alpha Test", Range(0,1)) = 0
        [HideInInspector] _Transparent ("Transparent", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "AutoLight.cginc"
    #include "UnityLightingCommon.cginc"

    sampler2D _MainTex; float4 _MainTex_ST;
    fixed4 _Color; fixed4 _EmissionColor; float _EmissionIntensity; float _Opacity; float _AlphaTest;
    // yarıküre ışığı: RenderSettings üç renkli ortam (unity_AmbientSky / unity_AmbientGround), Game.Eng_ApplyHemi doğrusal değerlerle kurar
    float4 _CfGrad;                     // toon basamakları
    float _CfExposure;

    float CfGradient(float3 n, float3 l)
    {
        float coord = dot(n, l) * 0.5 + 0.5;
        int i = (int)clamp(floor(coord * 4.0), 0.0, 3.0);
        return i == 0 ? _CfGrad.x : (i == 1 ? _CfGrad.y : (i == 2 ? _CfGrad.z : _CfGrad.w));
    }
    float3 CfRRTAndODTFit(float3 v)
    {
        float3 a = v * (v + 0.0245786) - 0.000090537;
        float3 b = v * (0.983729 * v + 0.4329510) + 0.238081;
        return a / b;
    }
    float3 CfACES(float3 c)
    {
        const float3x3 ACESIn = float3x3(0.59719, 0.35458, 0.04823, 0.07600, 0.90834, 0.01566, 0.02840, 0.13383, 0.83777);
        const float3x3 ACESOut = float3x3(1.60475, -0.53108, -0.07367, -0.10208, 1.10813, -0.00605, -0.00327, -0.07276, 1.07602);
        c *= _CfExposure / 0.6;
        c = mul(ACESIn, c); c = CfRRTAndODTFit(c); c = mul(ACESOut, c);
        return saturate(c);
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }
            Cull [_Cull] ZWrite [_ZWrite] Blend [_SrcBlend] [_DstBlend]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma multi_compile_fog
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 wn : TEXCOORD1;
                float3 wp : TEXCOORD2;
                SHADOW_COORDS(3)
                UNITY_FOG_COORDS(4)
            };
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                o.wn = UnityObjectToWorldNormal(v.normal);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv);
                float4 diff = _Color * tex;
                float alpha = diff.a * _Opacity;
                clip(alpha - _AlphaTest);
                float3 n = normalize(i.wn) * (facing > 0 ? 1.0 : -1.0);
                // yarıküre ışığı (yukarı = +y)
                float hw = 0.5 * n.y + 0.5;
                float3 irr = lerp(unity_AmbientGround.rgb, unity_AmbientSky.rgb, hw);
                // yönlü ışık (gölgeli)
                float3 l = _WorldSpaceLightPos0.xyz; l *= rsqrt(max(dot(l, l), 1e-8));   // yönlü ışık yoksa (0,0,0) → NaN olmasın
                float sh = SHADOW_ATTENUATION(i);
                irr += _LightColor0.rgb * CfGradient(n, l) * sh;
                float3 col = diff.rgb * irr + _EmissionColor.rgb * _EmissionIntensity;
                col = CfACES(col);
                fixed4 c = fixed4(col, alpha);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }

        Pass
        {
            Name "FORWARD_ADD"
            Tags { "LightMode"="ForwardAdd" }
            Cull [_Cull] ZWrite Off Blend [_SrcBlend] One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdadd
            #pragma multi_compile_fog
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 wn : TEXCOORD1;
                float3 wp : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                o.wn = UnityObjectToWorldNormal(v.normal);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                float4 diff = _Color * tex2D(_MainTex, i.uv);
                float alpha = diff.a * _Opacity;
                clip(alpha - _AlphaTest);
                float3 n = normalize(i.wn) * (facing > 0 ? 1.0 : -1.0);
                float3 l; float att = 1;
                #if defined(DIRECTIONAL) || defined(DIRECTIONAL_COOKIE)
                l = normalize(_WorldSpaceLightPos0.xyz);
                #else
                l = normalize(_WorldSpaceLightPos0.xyz - i.wp);
                // Three: pow(saturate(1 - dist/distance), decay=2); unity_WorldToLight noktayı ışık menziline göre ölçekler
                float3 lc = mul(unity_WorldToLight, float4(i.wp, 1)).xyz;
                float t = saturate(1.0 - length(lc));
                att = t * t;
                #endif
                float3 col = diff.rgb * _LightColor0.rgb * CfGradient(n, l) * att;
                col = CfACES(col) - CfACES(float3(0, 0, 0));
                fixed4 c = fixed4(col, alpha);
                UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0, 0, 0, 0));
                return c;
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            struct v2f { V2F_SHADOW_CASTER; float2 uv : TEXCOORD1; };
            v2f vert (appdata_base v) { v2f o; TRANSFER_SHADOW_CASTER_NORMALOFFSET(o) o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex); return o; }
            // Three r147 derinlik malzemesi map/opacity/alphaTest uygular (kesik yapraklar vb. doğru gölge)
            float4 frag (v2f i) : SV_Target { clip(tex2D(_MainTex, i.uv).a * _Opacity - _AlphaTest); SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
    Fallback Off
}
