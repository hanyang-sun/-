Shader "Hidden/Brick2Life/SurfaceLight"
{
    SubShader
    {
        Pass
        {
            ZWrite Off ZTest LEqual Cull Back
            Blend One One
            ColorMask RGB
            Offset -1, -1
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _VPImage, _VPDepth, _VPReceiverTex;
            float4x4 _VPMatrix, _VPView;
            float4 _VPPosition, _VPReceiverColor, _VPReceiverST;
            float _VPFar, _VPBias, _VPStrength;
            float _VPBlackLevel;
            float4 _VPBlurStep;
            struct v2f
            {
                float4 position : SV_POSITION;
                float3 world : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };
            v2f vert(appdata_base v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.texcoord.xy * _VPReceiverST.xy + _VPReceiverST.zw;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float4 clipPosition = mul(_VPMatrix, float4(i.world, 1));
                if (clipPosition.w <= 0) discard;
                float2 uv = clipPosition.xy / clipPosition.w * 0.5 + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1 - uv.y;
                #endif
                if (any(uv < 0) || any(uv > 1)) discard;
                float eyeDepth = -mul(_VPView, float4(i.world, 1)).z;
                if (eyeDepth < 0.1 || eyeDepth > _VPFar) discard;
                float closest = tex2D(_VPDepth, uv).r * _VPFar;
                if (eyeDepth > closest + _VPBias) discard;
                float3 toLight = _VPPosition.xyz - i.world;
                float incidence = saturate(dot(normalize(i.normal), normalize(toLight)));
                float attenuation = min(1.5, 196.0 / max(1, dot(toLight, toLight)));
                float3 albedo = saturate(_VPReceiverColor.rgb * tex2D(_VPReceiverTex, i.uv).rgb);
                float3 image = tex2D(_VPImage, uv).rgb * 0.5;
                image += (tex2D(_VPImage, uv + float2(_VPBlurStep.x, 0)).rgb +
                    tex2D(_VPImage, uv - float2(_VPBlurStep.x, 0)).rgb +
                    tex2D(_VPImage, uv + float2(0, _VPBlurStep.y)).rgb +
                    tex2D(_VPImage, uv - float2(0, _VPBlurStep.y)).rgb) * 0.125;
                image = lerp(image, float3(1, 1, 1), _VPBlackLevel);
                // Direct reflected-light approximation: a black output adds no light and never paints the receiver black.
                return float4(albedo * image * incidence * attenuation * _VPStrength, 0);
            }
            ENDCG
        }
    }
}
