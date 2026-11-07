Shader "Hidden/Brick2Life/ProjectorDepth"
{
    SubShader
    {
        Pass
        {
            ZWrite On ZTest LEqual Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4x4 _VPMatrix, _VPView;
            float _VPFar;
            struct v2f { float4 position : SV_POSITION; float depth : TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o;
                float4 world = mul(unity_ObjectToWorld, v.vertex);
                o.position = mul(_VPMatrix, world);
                o.depth = -mul(_VPView, world).z / _VPFar;
                return o;
            }
            float4 frag(v2f i) : SV_Target { return i.depth; }
            ENDCG
        }
    }
}
