Shader "Brush/Line2S"
{
    Properties
    {
        _Angle ("Centre Angle (degrees)", Range(0, 3600)) = 0
        _AngleWidth ("Angle Width (degrees)", Range(0, 360)) = 360
        _CentreRoom ("Centre Room", Vector) = (0, 0, 0, 0)

        _Speed ("Speed of waves", Float) = 1.0

    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Angle;
            float _AngleWidth;
            float4 _CentreRoom;


            float _Speed;

            float pi = 3.14159265359;
            float tau = 6.28318530718;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float tri(float x) {
                return 1.0 - abs(2.0 * x - 1.0);
            }

            float star(float2 uv, float points, float sharpness)
            {
                uv = uv * 2.0 - 1.0;
                float r = length(uv);
                float angle = atan2(uv.y, uv.x);
                angle /= tau;
                float spikes = points * 2.0;
                float wave = cos(angle * spikes) * 0.5 + 0.5;
                float radius = pow(wave, sharpness);
                return step(r, radius);
            }

            float border(float2 uv, float width) {
                if (uv.x < width || uv.x > 1.0 - width || uv.y < width || uv.y > 1.0-width) {
                    return 1.0;
                }
                return 0.0;
            }

            float slope(float x, float start, float end) {
                return clamp((x-start)/(end-start), 0.0, 1.0);
            }

            float plateau(float x, float start, float end) {
                return slope(x, start, end) * slope(x, 1.0 - start, 1.0 - end);
            }

            float plateau2d(float2 uv, float start, float end) {
                return min(plateau(uv.x, start, end), plateau(uv.y, start, end));
            }

            float4 hsl_to_rgb(float h, float s, float l)
            {
                h = frac(h) * 6.0;
                float c = (1.0 - abs(2.0 * l - 1.0)) * s;
                float x = c * (1.0 - abs(fmod(h, 2.0) - 1.0));
                float3 rgb = (h < 1.0) ? float3(c, x, 0.0) :
                            (h < 2.0) ? float3(x, c, 0.0) :
                            (h < 3.0) ? float3(0.0, c, x) :
                            (h < 4.0) ? float3(0.0, x, c) :
                            (h < 5.0) ? float3(x, 0.0, c) :
                                        float3(c, 0.0, x);
                return float4(rgb + (l - c * 0.5), 1.0);
            }

            float wave(float2 uv, float time, float amp, float thickness, float offset, float freq, float velocity) {
                float y = amp * 0.5 * cos(uv.x * freq + time * _Speed * velocity) + 0.5 + offset;
                if (uv.y < y + thickness && uv.y > y - thickness) {
                    return 1.0;
                }
                return 0.0;
                // float mid = 0.5 - 0.5 * thickness;
                // return plateau(uv.y, mid - 0.05, mid + 0.05);
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }


            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)
                float angle_frag = atan2(i.worldPos.z - _CentreRoom.z, i.worldPos.x - _CentreRoom.x);
                angle_frag = degrees(angle_frag) + 180.0;
                float angle_start = fmod(_Angle, 360.0);
                float angle_end = angle_start + _AngleWidth;
                bool angle_in_slice = angle_start < angle_frag && angle_frag < angle_end;
                if (angle_end > 360.0 && angle_frag + 360.0 < angle_end) {
                    angle_in_slice = true;
                }
                float mask_soft = 0.0;
                float mask_hard = 0.0;
                if (angle_in_slice) {
                    mask_soft = (angle_frag - angle_start) / _AngleWidth;
                    mask_soft = tri(mask_soft);
                    mask_hard = 1.0;
                }
                
                // uv, time, amp, thickness, offset, freq, velocity
                float w1 = wave(i.uv, _Time.y, 0.9, 0.1, 0.1, 200.0, 1.0);
                float w2 = wave(i.uv, _Time.y, 0.5, 0.15, 0.1, 300.0, 10.0);
                float w3 = wave(i.uv, _Time.y, 0.5, 0.15, -0.2, 400.0, 2.0);
                float4 colour = float4(w1, w2, w3, w1 + w2 + w3);

                colour.rgb += (abs(mask_soft) * 0.5);
                // colour.a *= mask_hard;
                return colour;

            }
            ENDCG
        }
    }
}
