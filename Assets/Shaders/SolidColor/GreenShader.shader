Shader "Unlit/GreenShader"
{
    Properties {
        // 没有任何可调参数
    }

    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return pow(fixed4(0.458564,0.8018868,0.4122908,1), 2.2);  
            }
            ENDCG
        }
    }
}
