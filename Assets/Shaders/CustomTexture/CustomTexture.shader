Shader "Custom/CustomTexture"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Scale   ("Texture Scale", Range(0.01, 10)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert

        sampler2D _MainTex;
        half      _Scale;

        struct Input
        {
            float3 worldPos;   // 世界空间坐标，由 Unity 自动生成
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            // 用世界坐标 xy 平面做投影
            half2 uv = IN.worldPos.xy * _Scale;
            fixed4 c = tex2D(_MainTex, uv);
            o.Albedo = c.rgb;
            o.Alpha  = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
