Shader "Unlit/MapShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EyeDist("Eye Distance", Float) = 0.1
        _FOV_Width("Fov Width", Float) = 20
        _FOV_Height("Fov Height", Float) = 20
        _Screen_Width("Screen Width", Float) = 100
        _Screen_Height("Screen Height", Float) = 100
        _K_R("K_R", Vector) = (1, 1, 1)
        _K_G("K_G", Vector) = (1, 1, 1)
        _K_B("K_B", Vector) = (1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float2 _MainTex_TexelSize;
            float _EyeDist;
            float _FOV_Width;
            float _FOV_Height;
            float _Screen_Width;
            float _Screen_Height;
            float3 _K_R;
            float3 _K_G;
            float3 _K_B;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float2 calculate_uv(float2 uv, float2 center, float2 screenSize, float2 fovSize, float3 K)
            {
                float2 duv = (uv - center) * screenSize;
                float r = sqrt(duv.x * duv.x + duv.y * duv.y);
                float R = K.x * pow(r, 3) + K.y * pow(r, 2) + K.z * r;
                return (duv / r * R) / fovSize + center;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float uv_x;
                float2 center;
                // left eye
                if (i.uv.x < 0.5) {
                    uv_x = i.uv.x * (1 - _EyeDist) * 2;
                    center = float2(0.5 * (1 - _EyeDist), 0.5);
                }
                // right eye
                else {
                    uv_x = (i.uv.x - 0.5) * (1 - _EyeDist) * 2 + _EyeDist;
                    center = float2(0.5 * (1 + _EyeDist), 0.5);
                }
                float2 uv_r = calculate_uv(float2(uv_x, i.uv.y), center, float2(_Screen_Width, _Screen_Height), float2(_FOV_Width, _FOV_Height), _K_G);
                float2 uv_g = calculate_uv(float2(uv_x, i.uv.y), center, float2(_Screen_Width, _Screen_Height), float2(_FOV_Width, _FOV_Height), _K_R);
                float2 uv_b = calculate_uv(float2(uv_x, i.uv.y), center, float2(_Screen_Width, _Screen_Height), float2(_FOV_Width, _FOV_Height), _K_B);
                float r = tex2D(_MainTex, uv_r).r;
                float g = tex2D(_MainTex, uv_g).g;
                float b = tex2D(_MainTex, uv_b).b;
                return fixed4(r, g, b, 1);
            }
            ENDCG
        }
    }
}
