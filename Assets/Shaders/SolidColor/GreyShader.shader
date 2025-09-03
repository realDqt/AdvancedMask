Shader "Unlit/GreyShader"
{
     Properties { /* 无参数 */ }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        // ---------- 正常渲染 Pass ----------
        Pass
        {
            Tags { "LightMode"="ForwardBase" }   // 必须，才能收到主光源阴影
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase        // 让 Unity 生成阴影宏
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                SHADOW_COORDS(1)   // 声明阴影坐标插槽
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_SHADOW(o);   // 计算阴影坐标
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(0.71, 0.73, 0.75, 1);
                col.rgb = pow(col.rgb, 3);          // Gamma 修正
                fixed shadow = SHADOW_ATTENUATION(i); // 采样阴影 0~1
                col.rgb *= shadow;                    // 把阴影乘到纯色上
                return col;
            }
            ENDCG
        }

        // ---------- 投射阴影用的 Pass ----------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f {
                V2F_SHADOW_CASTER;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
