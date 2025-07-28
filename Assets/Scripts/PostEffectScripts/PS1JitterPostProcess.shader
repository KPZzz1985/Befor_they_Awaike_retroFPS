Shader "Custom/PS1JitterPostProcess" {
    Properties{
        _MainTex("Base (RGB)", 2D) = "white" {}
    // Интенсивность сдвига – чем больше значение, тем сильнее сдвиг
    _JitterIntensity("Jitter Intensity", Range(0,10)) = 1.0
        // Размер сетки (сколько ячеек на экране по одной оси)
        _JitterGrid("Jitter Grid", Float) = 50.0
        // Смещение, вычисляемое на основе поворота камеры (задаётся из скрипта)
        _CameraJitterOffset("Camera Jitter Offset", Vector) = (0,0,0,0)
    }
        SubShader{
            Tags { "RenderType" = "Opaque" "Queue" = "Overlay" }
            Pass {
                ZTest Always Cull Off ZWrite Off
                CGPROGRAM
                #pragma vertex vert_img
                #pragma fragment frag
                #include "UnityCG.cginc"

                sampler2D _MainTex;
                float _JitterIntensity;
                float _JitterGrid;
                float4 _CameraJitterOffset;

                fixed4 frag(v2f_img i) : SV_Target {
                    // Берём исходные UV
                    float2 uv = i.uv;
                    // Вычисляем смещение по сетке:
                    // floor(uv * grid + offset) даёт дискретные шаги,
                    // затем делим обратно, получая «квантованные» UV
                    float2 quantizedUV = floor(uv * _JitterGrid + _CameraJitterOffset.xy) / _JitterGrid;
                    // Разница между квантованными UV и исходными – это величина сдвига
                    float2 jitterOffset = (quantizedUV - uv) * _JitterIntensity;
                    // Применяем сдвиг к UV для выборки итогового цвета
                    fixed4 col = tex2D(_MainTex, uv + jitterOffset);
                    return col;
                }
                ENDCG
            }
    }
        FallBack "Hidden/BlitCopy"
}
