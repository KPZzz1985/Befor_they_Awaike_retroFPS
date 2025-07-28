Shader "Custom/PS1JitterPostProcess" {
    Properties{
        _MainTex("Base (RGB)", 2D) = "white" {}
    // ������������� ������ � ��� ������ ��������, ��� ������� �����
    _JitterIntensity("Jitter Intensity", Range(0,10)) = 1.0
        // ������ ����� (������� ����� �� ������ �� ����� ���)
        _JitterGrid("Jitter Grid", Float) = 50.0
        // ��������, ����������� �� ������ �������� ������ (������� �� �������)
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
                    // ���� �������� UV
                    float2 uv = i.uv;
                    // ��������� �������� �� �����:
                    // floor(uv * grid + offset) ��� ���������� ����,
                    // ����� ����� �������, ������� ������������� UV
                    float2 quantizedUV = floor(uv * _JitterGrid + _CameraJitterOffset.xy) / _JitterGrid;
                    // ������� ����� ������������� UV � ��������� � ��� �������� ������
                    float2 jitterOffset = (quantizedUV - uv) * _JitterIntensity;
                    // ��������� ����� � UV ��� ������� ��������� �����
                    fixed4 col = tex2D(_MainTex, uv + jitterOffset);
                    return col;
                }
                ENDCG
            }
    }
        FallBack "Hidden/BlitCopy"
}
