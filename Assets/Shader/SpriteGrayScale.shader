Shader "Custom/SpriteGrayscale"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // 0 = 컬러, 1 = 흑백
        _Grayscale ("Grayscale", Range(0, 1)) = 1

        // 이 값보다 어두운 색은 검정색으로
        _BlackPoint ("Black Point", Range(0, 0.95)) = 0

        // 전체 흑백 결과의 밝기
        _Brightness ("Brightness", Range(0, 2)) = 1

        // 명암 대비
        _Contrast ("Contrast", Range(0.1, 3)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;

            fixed4 _Color;

            float _Grayscale;
            float _BlackPoint;
            float _Brightness;
            float _Contrast;

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv) * i.color;

                // 원본 색 → 밝기(Grayscale)
                float gray =
                    color.r * 0.299 +
                    color.g * 0.587 +
                    color.b * 0.114;

                // -------------------------
                // Black Point
                // -------------------------
                // BlackPoint 이하의 밝기를 검정으로 만든다.
                gray = saturate(
                    (gray - _BlackPoint) /
                    max(0.001, 1.0 - _BlackPoint)
                );

                // -------------------------
                // Contrast
                // -------------------------
                gray = (gray - 0.5) * _Contrast + 0.5;
                gray = saturate(gray);

                // -------------------------
                // Brightness
                // -------------------------
                gray *= _Brightness;
                gray = saturate(gray);

                fixed3 grayscaleColor = fixed3(gray, gray, gray);

                // Grayscale가 0이면 원본
                // Grayscale가 1이면 흑백
                color.rgb = lerp(
                    color.rgb,
                    grayscaleColor,
                    saturate(_Grayscale)
                );

                return color;
            }

            ENDCG
        }
    }
}