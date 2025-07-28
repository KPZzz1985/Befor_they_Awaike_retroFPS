Shader "Custom/PixelGlass" {
    Properties {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _PixelSize ("Pixel Size", Range(1, 100)) = 10
        _RefractionIndex ("Refraction Index", Range(0, 1)) = 0.1
        _ChromaticAberration ("Chromatic Aberration", Range(0, 1)) = 0.1
        _Color ("Color", Color) = (1, 1, 1, 1)
        _HueShift ("Hue Shift", Range(-1, 1)) = 0
        _Cube ("Environment Reflection", Cube) = "" {}
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.5
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        GrabPass { }

        Pass {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _GrabTexture;
            samplerCUBE _Cube;
            float _PixelSize;
            float _RefractionIndex;
            float _ChromaticAberration;
            float4 _Color;
            float _HueShift;
            float _ReflectionStrength;

            float3 rgb2hsv(float3 c) {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 hsv2rgb(float3 c) {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Pixelation
                float2 pixelatedUV = i.screenPos.xy / i.screenPos.w;
                pixelatedUV = floor(pixelatedUV * _PixelSize) / _PixelSize;

                // Refraction
                float2 offset = _RefractionIndex * 2.0 * (i.uv - 0.5);
                
                // Chromatic aberration
                float red = tex2D(_GrabTexture, pixelatedUV + offset + float2(-_ChromaticAberration, 0)).r;
                float green = tex2D(_GrabTexture, pixelatedUV + offset).g;
                float blue = tex2D(_GrabTexture, pixelatedUV + offset + float2(_ChromaticAberration, 0)).b;

                fixed4 col = fixed4(red, green, blue, 1);

                // Hue shift
                float3 hsv = rgb2hsv(col.rgb);
                hsv.x += _HueShift;
                hsv.x = frac(hsv.x); // make sure hue is [0,1]
                float3 rgb = hsv2rgb(hsv);

                // Color adjustment
                col.rgb = rgb * _Color.rgb;
                col.a *= _Color.a;

                // Environment reflection
                float3 viewDir = normalize(i.screenPos.xyz);
                float4 envColor = texCUBE(_Cube, reflect(viewDir, normalize(float3(0, 0, 1))));
                
                // Blend the environment color with the pixel color
                col = lerp(col, envColor, _ReflectionStrength);
                
                return col;
            }
            ENDCG
        }
    }
}
