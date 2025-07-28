Shader "Custom/SimpleFogShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _FogColor("Fog Color", Color) = (0.5,0.5,0.5,1)
        _FogDensity("Fog Density", Float) = 0.05
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 100

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                // Убедитесь, что ваша камера генерирует текстуру глубины.
                #pragma multi_compile _ _REQUIRE_DEPTH_TEXTURE

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                    float4 screenPos : TEXCOORD1; // Для доступа к глубине
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                sampler2D _CameraDepthTexture; // Системная текстура глубины
                float4 _FogColor;
                float _FogDensity;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.uv = v.uv;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.screenPos = ComputeScreenPos(o.vertex); // Вычислите координаты экрана для доступа к глубине
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    fixed4 col = tex2D(_MainTex, i.uv);
                    float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos))); // Изменено для доступа к глубине
                    float fogFactor = 1.0 - exp(-depth * _FogDensity);
                    col = lerp(col, _FogColor, fogFactor);
                    return col;
                }
                ENDCG
            }
        }
            Fallback "Diffuse"
}
