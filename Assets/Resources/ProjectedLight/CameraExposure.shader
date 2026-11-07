Shader "Hidden/Brick2Life/CameraExposure"
{
    Properties { _MainTex ("Camera", 2D) = "white" {} }
    SubShader
    {
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _CaptureExposure;
            float4 frag(v2f_img i) : SV_Target
            { float4 color = tex2D(_MainTex, i.uv); return float4(color.rgb * _CaptureExposure, color.a); }
            ENDCG
        }
    }
}
