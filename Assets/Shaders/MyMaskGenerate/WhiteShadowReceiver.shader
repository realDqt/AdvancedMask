Shader "Hidden/WhiteShadowReceiver"
{
    SubShader
    {
        Tags{ "LightMode" = "ShadowCaster" }

        Pass
        {
            ZWrite On ZTest LEqual Cull Off
            ColorMask R

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            // 声明内置的阴影贴图和矩阵
            UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);
            float4x4 _ShadowMapTexture_ST;      // 用不到，但编译器需要
            //float4x4 unity_WorldToShadow[4];   // 0 号即主方向光

            struct v2f
            {
                V2F_SHADOW_CASTER;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 世界坐标 → 主光源阴影空间
                float4 shadowCoord = mul(unity_WorldToShadow[0], float4(i.worldPos, 1.0));
                // 采样阴影
                half shadow = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, shadowCoord.xyz);
                // 有阴影就画白，否则丢弃
                if (shadow < 0.1)       // 有阴影 -> 画白
                    return fixed4(1, 0, 0, 0);
                else
                    return fixed4(0, 0, 0, 0);// 无阴影 -> 不画
            }
            ENDCG
        }
    }
}