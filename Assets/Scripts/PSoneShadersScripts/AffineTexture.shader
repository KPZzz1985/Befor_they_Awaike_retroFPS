Shader "Custom/PSOneShaderGI_JitterWithTextureFrameRate" {
    Properties{
        _MainTex("Texture", 2D) = "white" {}
        _AOTexture("AO Texture", 2D) = "white" {}
        _Tiling("Tiling", Range(1,1024)) = 64
        _PixelationAmount("Pixelation Amount", Range(1,1024)) = 64
        _ColorPrecision("Color Precision", Range(1,256)) = 32
            // ��� ������� ����������� ������� (���������) � ������� �����������:
            _JitterAmount("Jitter Amount (World Units)", Range(0.001,10)) = 1
            // ����� �������� ��� �������� ���������� ������� ���������� �������:
            _TextureFPS("Texture FPS", Range(1,60)) = 10
            _TextureJitterAmplitude("Texture Jitter Amplitude", Range(0,1)) = 0.01
    }
        SubShader{
            Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
            LOD 100

            CGPROGRAM
            // ���������� surface-������� � ��������� ���������� ���������� �������
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
                float3 worldPos;  // �������� ������� ������� (����� �������������� ��� ���������)
                float2 uv_AOTexture;
                float4 pos : SV_POSITION;
            };

            // ��������� ������: �������� ������� ������� ������������ ������
            void vert(inout appdata_full v, out Input o) {
                UNITY_INITIALIZE_OUTPUT(Input, o);
                // ������� UV � ������� �������
                o.uv_MainTex = v.texcoord;
                o.uv_AOTexture = v.texcoord1;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldPos = worldPos;

                /*
                   ��������� ��������� ������������ ������, ����� �������� ��� � ����� _JitterAmount.
                   ��� ��������� ������ ������������ �������� ����������, ����� ������� �����������.
                */
                float3 camRelPos = worldPos - _WorldSpaceCameraPos;
                float3 quantizedCamRelPos = floor(camRelPos / _JitterAmount + 0.5) * _JitterAmount;
                float3 quantizedWorldPos = quantizedCamRelPos + _WorldSpaceCameraPos;

                // ��������� ������������ ������� ������� � clip-������������
                float4 clipPos = UnityWorldToClipPos(quantizedWorldPos);
                o.pos = clipPos;
            }

            // ������� �������������� ������� (�����������)
            void surf(Input IN, inout SurfaceOutput o) {
                /*
                   ������ ����������� ������� ����������� ��������:
                   - ��������� ���������� �����: floor(_Time.y * _TextureFPS) / _TextureFPS
                     (�� ����, ����� ����������� ��� � 1/_TextureFPS ������)
                   - �� ������ ����� �������� ��������� ��������������� ��������.
                   - ��������� ��� �������� � UV-����������� �� ������������.
                */
                float discreteTime = floor(_Time.y * _TextureFPS) / _TextureFPS;
                // ���������� ��������������� ����� (seed ������� �� discreteTime)
                float randomValue = frac(sin(discreteTime * 12.9898) * 43758.5453);
                // ����������� ��� � ���� (0 .. 2*pi)
                float angle = randomValue * 6.28318530718;
                // ��������� ������ ��������; _TextureJitterAmplitude ���������� ��������� ������
                float2 textureOffset = float2(cos(angle), sin(angle)) * _TextureJitterAmplitude;

                // ��������� �������� � UV � �����������
                float2 pixelatedUV = floor((IN.uv_MainTex + textureOffset) * _PixelationAmount) / _PixelationAmount;

                fixed4 c = tex2D(_MainTex, pixelatedUV);
                o.Albedo = c.rgb;
                o.Alpha = c.a;
                // ��������� ���� ��� �������� ������������ �������� �������� (��� �� PS1)
                o.Albedo = round(o.Albedo * _ColorPrecision) / _ColorPrecision;
            }
            ENDCG
        }
            FallBack "Diffuse"
}
