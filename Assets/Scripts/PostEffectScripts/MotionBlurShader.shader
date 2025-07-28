Shader "Custom/MotionBlurShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PrevTex ("Previous Frame", 2D) = "white" {}
        _Blend ("Blend", Range(0, 1)) = 0.5
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
            sampler2D _PrevTex;
            float _Blend;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 curColor = tex2D(_MainTex, i.uv);
                fixed4 prevColor = tex2D(_PrevTex, i.uv);
                return lerp(curColor, prevColor, _Blend);
            }
            ENDCG
        }
    }
}
