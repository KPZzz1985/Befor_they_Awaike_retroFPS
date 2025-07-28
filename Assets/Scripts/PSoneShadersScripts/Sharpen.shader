Shader "Hidden/Sharpen" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Sharpness ("Sharpness", Range(0, 5)) = 0.5
    }

    SubShader {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float2 uv : TEXCOORD0;
                float4 vertex : POSITION;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _Sharpness;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv;
                float2 uvSize = float2(1.0 / _MainTex_TexelSize.x, 1.0 / _MainTex_TexelSize.y);

                float4 sum = tex2D(_MainTex, uv);
                sum *= 5.0;

                sum += tex2D(_MainTex, uv + float2(uvSize.x, 0)) * -1.0;
                sum += tex2D(_MainTex, uv + float2(-uvSize.x, 0)) * -1.0;
                sum += tex2D(_MainTex, uv + float2(0, uvSize.y)) * -1.0;
                sum += tex2D(_MainTex, uv + float2(0, -uvSize.y)) * -1.0;

                sum *= _Sharpness;

                float4 col = tex2D(_MainTex, uv);
                col.rgb += sum.rgb;

                return col;
            }
            ENDCG
        }
    }
}