Shader "Unlit/LocalSurWrapTexture"
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
        #pragma surface surf Lambert vertex:vert   // <-- 指定自定义顶点函数

        sampler2D _MainTex;
        half      _Scale;

        // 自定义顶点→片元数据结构
        struct Input
        {
            float2 localUV;   // 我们用局部坐标算出的 UV
        };

        // 顶点函数：把局部坐标 xy 直接当 UV 传给片元
        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            // 用局部坐标的 xy 作为“天然”UV，不受世界变换影响
            o.localUV = v.vertex.xy * _Scale;
        }

        // 片元函数
        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.localUV);
            o.Albedo = c.rgb;
            o.Alpha  = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
