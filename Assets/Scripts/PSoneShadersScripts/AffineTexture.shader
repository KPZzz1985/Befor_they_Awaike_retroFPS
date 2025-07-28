Shader "Custom/PSOneShaderGI_JitterWithTextureFrameRate" {
    Properties{
        _MainTex("Texture", 2D) = "white" {}
        _AOTexture("AO Texture", 2D) = "white" {}
        _Tiling("Tiling", Range(1,1024)) = 64
        _PixelationAmount("Pixelation Amount", Range(1,1024)) = 64
        _ColorPrecision("Color Precision", Range(1,256)) = 32
            // Для эффекта квантования позиции (геометрии) в мировых координатах:
            _JitterAmount("Jitter Amount (World Units)", Range(0.001,10)) = 1
            // Новые свойства для имитации пониженной частоты обновления текстур:
            _TextureFPS("Texture FPS", Range(1,60)) = 10
            _TextureJitterAmplitude("Texture Jitter Amplitude", Range(0,1)) = 0.01
    }
        SubShader{
            Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
            LOD 100

            CGPROGRAM
            // Используем surface-функцию с указанием кастомного вершинного шейдера
            #pragma surface surf BlinnPhong vertex:vert addshadow
            #pragma target 3.0
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _AOTexture;
            float _Tiling;
            float _PixelationAmount;
            float _ColorPrecision;
            float _JitterAmount;
            float _TextureFPS;
            float _TextureJitterAmplitude;

            struct Input {
                float2 uv_MainTex;
                float3 worldPos;  // исходная мировая позиция (может использоваться для освещения)
                float2 uv_AOTexture;
                float4 pos : SV_POSITION;
            };

            // Вершинный шейдер: квантуем мировую позицию относительно камеры
            void vert(inout appdata_full v, out Input o) {
                UNITY_INITIALIZE_OUTPUT(Input, o);
                // Передаём UV и мировую позицию
                o.uv_MainTex = v.texcoord;
                o.uv_AOTexture = v.texcoord1;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldPos = worldPos;

                /*
                   Вычисляем положение относительно камеры, затем квантуем его с шагом _JitterAmount.
                   Это имитирует эффект ограниченной точности вычислений, когда позиция округляется.
                */
                float3 camRelPos = worldPos - _WorldSpaceCameraPos;
                float3 quantizedCamRelPos = floor(camRelPos / _JitterAmount + 0.5) * _JitterAmount;
                float3 quantizedWorldPos = quantizedCamRelPos + _WorldSpaceCameraPos;

                // Переводим квантованную мировую позицию в clip-пространство
                float4 clipPos = UnityWorldToClipPos(quantizedWorldPos);
                o.pos = clipPos;
            }

            // Функция поверхностного шейдера (фрагментная)
            void surf(Input IN, inout SurfaceOutput o) {
                /*
                   Эффект «пониженной частоты обновления» текстуры:
                   - Вычисляем дискретное время: floor(_Time.y * _TextureFPS) / _TextureFPS
                     (то есть, время обновляется раз в 1/_TextureFPS секунд)
                   - На основе этого значения вычисляем псевдослучайное смещение.
                   - Добавляем это смещение к UV-координатам до пикселизации.
                */
                float discreteTime = floor(_Time.y * _TextureFPS) / _TextureFPS;
                // Генерируем псевдослучайное число (seed зависит от discreteTime)
                float randomValue = frac(sin(discreteTime * 12.9898) * 43758.5453);
                // Преобразуем его в угол (0 .. 2*pi)
                float angle = randomValue * 6.28318530718;
                // Вычисляем вектор смещения; _TextureJitterAmplitude определяет «величину прыжка»
                float2 textureOffset = float2(cos(angle), sin(angle)) * _TextureJitterAmplitude;

                // Применяем смещение к UV и пикселизуем
                float2 pixelatedUV = floor((IN.uv_MainTex + textureOffset) * _PixelationAmount) / _PixelationAmount;

                fixed4 c = tex2D(_MainTex, pixelatedUV);
                o.Albedo = c.rgb;
                o.Alpha = c.a;
                // Округляем цвет для имитации ограниченной цветовой точности (как на PS1)
                o.Albedo = round(o.Albedo * _ColorPrecision) / _ColorPrecision;
            }
            ENDCG
        }
            FallBack "Diffuse"
}
