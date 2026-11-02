// JS OUT (ShaderMaterial, BackSide) karşılığı: ters gövde (inverted hull) dış çizgi.
// Köşe, yumuşatılmış normal (TEXCOORD3 = JS "onormal") boyunca görüş uzayında kalınlık × uzaklık kadar itilir → ekranda sabit kalınlık;
// kalınlık konuma göre hafif titrer (el çizimi görünüm). Sis ve ton eşleme yok (JS'teki gibi).
Shader "Ciftlik/Outline"
{
    Properties
    {
        _Thick ("Thickness", Float) = 0.0024
        _Col ("Color", Color) = (0.0232, 0.0103, 0.0052, 1)
        _Jit ("Jitter", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Front ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Thick; fixed4 _Col; float _Jit;
            struct appdata { float4 vertex : POSITION; float3 onormal : TEXCOORD3; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert (appdata v)
            {
                v2f o;
                float4 mv = float4(UnityObjectToViewPos(v.vertex.xyz), 1.0);
                float3 n = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.onormal));
                float3 p = float3(v.vertex.x, v.vertex.y, -v.vertex.z);   // JS ile aynı titreşim deseni (Three koordinatı)
                float w = _Thick * (1.0 + 0.12 * sin(_Jit + p.x * 9.0 + p.y * 7.0 + p.z * 5.0));
                mv.xyz += n * w * (-mv.z);
                o.pos = mul(UNITY_MATRIX_P, mv);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target { return fixed4(_Col.rgb, 1.0); }
            ENDCG
        }
    }
    Fallback Off
}
