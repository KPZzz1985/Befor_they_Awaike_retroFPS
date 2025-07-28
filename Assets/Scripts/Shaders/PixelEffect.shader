Shader "Custom/PixelEffect"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _PixelSize("Pixel Size", Range(1, 100)) = 1
        _Color0("Color 0", Color) = (1, 0, 0, 1)
        _Color1("Color 1", Color) = (0, 1, 0, 1)
        _Color2("Color 2", Color) = (0, 0, 1, 1)
        _Color3("Color 3", Color) = (1, 1, 0, 1)
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
            float _PixelSize;
            float4 _Color0;
            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            half4 frag(v2f i) : SV_Target
            {
                i.uv = floor(i.uv * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;
                half4 col = tex2D(_MainTex, i.uv);
                
                if (i.uv.y < 0.25) return _Color0;
                if (i.uv.y < 0.5) return _Color1;
                if (i.uv.y < 0.75) return _Color2;
                return _Color3;
            }
            ENDCG
        }
    }
}
