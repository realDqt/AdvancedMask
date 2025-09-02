Shader "Custom/MetallicSurface"
{
     Properties
    {
        _MainTex      ("Albedo (RGB)",     2D) = "white" {}
        _BumpMap      ("Normal Map",       2D) = "bump" {}
        _EmissionMap  ("Emission (RGB)",   2D) = "black" {}
        _EmissionColor("Emission Color",   Color) = (0,0,0)
        _Metallic     ("Metallic (keep 1)", Range(0,1)) = 1
        _Smoothness   ("Smoothness",       Range(0,1)) = 0.9
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        #pragma multi_compile_instancing

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _EmissionMap;

        half4 _EmissionColor;
        half  _Metallic;
        half  _Smoothness;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float2 uv_EmissionMap;
        };

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo     = c.rgb;
            o.Normal     = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            o.Emission   = tex2D(_EmissionMap, IN.uv_EmissionMap).rgb * _EmissionColor.rgb;
            o.Metallic   = _Metallic;   // 强制金属
            o.Smoothness = _Smoothness; // 高光收敛
            o.Alpha      = c.a;
        }
        ENDCG
    }

    FallBack "Standard"
}
