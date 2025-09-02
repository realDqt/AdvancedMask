Shader "Custom/GreyMeta"
{
    Properties
    {
        _Color      ("Base Color (Gray)", Color) = (0.71, 0.73, 0.75, 1)
        _Specular   ("Specular Level", Range(0,1)) = 16
        _Gloss      ("Glossiness",     Range(2,256)) = 45
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // 自定义光照函数，不再用 Standard
        #pragma surface surf Custom noambient

        half4 _Color;
        half  _Specular;
        half  _Gloss;

        struct Input
        {
            half2 uv_MainTex;   // 占位
        };

        // 自定义光照函数：只计算直接光，不采样反射探针 / 天空盒
        half4 LightingCustom (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half3 n = normalize(s.Normal);
            half3 l = normalize(lightDir);
            half  ndotl = saturate(dot(n, l));

            // 半角向量
            half3 h = normalize(l + viewDir);
            half  ndoth = saturate(dot(n, h));

            // 漫反射
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * ndotl * atten;

            // 高光（Blinn-Phong）
            half spec = pow(ndoth, _Gloss) * _Specular;
            c.rgb += _LightColor0.rgb * spec * atten;
            c.a = s.Alpha;
            return c;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = _Color.rgb;
            o.Alpha  = _Color.a;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
