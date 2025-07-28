Shader "Custom/BlurShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur size", Range(0, 10)) = 1
    }
    SubShader
    {
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
            float _BlurSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                fixed4 color = fixed4(0, 0, 0, 0);
                int count = 0;
                for (float x = -_BlurSize; x <= _BlurSize; x++)
                {
                    for (float y = -_BlurSize; y <= _BlurSize; y++)
                    {
                        color += tex2D(_MainTex, uv + float2(x, y) * _BlurSize * 0.0025);
                        count++;
                    }
                }
                return color / count;
            }
            ENDCG
        }
    }
}
