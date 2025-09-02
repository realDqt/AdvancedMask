Shader "Unlit/Sofa"
{
   Properties
    {
        _MainTex    ("Albedo (RGB)", 2D)   = "white" {}
        _NormalMap    ("Normal Map",   2D)   = "bump" {}
        [HDR] _EmissionMap ("Emission", 2D) = "black" {}
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Lighting Off        // 关闭所有光照
            Cull Back
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent: TANGENT;
            };

            struct v2f
            {
                float2 uv        : TEXCOORD0;
                float4 pos       : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _NormalMap;
            sampler2D _EmissionMap;
            half4     _EmissionColor;
            float4    _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 读取 Albedo
                fixed4 col = tex2D(_MainTex, i.uv);

                col = pow(col, 0.86);

                // 读取 Emission（如果有）
                fixed3 emission = tex2D(_EmissionMap, i.uv).rgb * _EmissionColor.rgb;

                // 最终颜色：Albedo + Emission
                return fixed4(col.rgb + emission, col.a);
            }
            ENDCG
        }

        //----------------------------------------------------
        // 阴影投射 Pass（Built-in 管线）
        //----------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack Off
}
