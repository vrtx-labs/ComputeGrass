Shader "ComputeGrass/Grass (Graphics)"
{
    Properties
    {
        _Cutoff("Cutoff", Range(0, 1)) = 0.5
        _TopTint("Top Tint", Color) = (1, 1, 1, 1)
        _BottomTint("Bottom Tint", Color) = (1, 1, 1, 1)
        _BottomTintOffset("Bottom Tint Offset", Range(-1,1)) = 1
        _ShadowAdjustment("Shadow Details Blend", Range(0,0.5)) = 0.15
    }
    
    CGINCLUDE
    #include "UnityCG.cginc" 
    #include "Lighting.cginc"
    #include "AutoLight.cginc"
    #include "GrassTypes.hlsl" 
    #include "GrassFunctions.hlsl"
    
    #pragma multi_compile _SHADOWS_SCREEN
    #pragma multi_compile_fwdbase_fullforwardshadows
    #pragma multi_compile_fog

    struct v2f
    {
        float4 pos : SV_POSITION;       // Position in clip space
        float2 uv : TEXCOORD0;          // texcoords and variant index
        float3 positionWS : TEXCOORD1;  // Position in world space
        float3 normalWS : TEXCOORD2;    // Normal vector in world space
        float3 diffuseColor : COLOR;
        int index : TEXCOORD6;
        UNITY_LIGHTING_COORDS(3, 4)
        UNITY_FOG_COORDS(5)
    };
    // Vertex function
    struct unityTransferVertexToFragmentSucksHack
    {
        float3 vertex : POSITION;
    };

    // Properties
    uint _NumOfVariants;
    float4 _TopTint;
    float4 _BottomTint;
    float _BottomTintOffset;
    float _Cutoff;
    float _ShadowAdjustment;

    StructuredBuffer<DrawTriangle> _DrawTriangles;


    sampler2D _Variant0;
    sampler2D _Variant1;
    sampler2D _Variant2;
    sampler2D _Variant3;



    float4 tex2DVariant(uint index, float2 texcoord)
    {
        if (index < 0)
            return RGBA_ERROR;
		
		index = index % _NumOfVariants;
		
        SAMPLE_VARIANT(0, index, texcoord)
        SAMPLE_ELSEVARIANT(1, index, texcoord)
        SAMPLE_ELSEVARIANT(2, index, texcoord)
        SAMPLE_ELSEVARIANT(3, index, texcoord)
        else  return RGBA_WHITE;
    }


    
    // -- retrieve data generated from compute shader
    v2f vert(uint vertexID : SV_VertexID)
    {
        // Initialize the output struct
        v2f output = (v2f)0;
        
        // Get the vertex from the buffer
        // Since the buffer is structured in triangles, we need to divide the vertexID by three
        // to get the triangle, and then modulo by 3 to get the vertex on the triangle
        DrawTriangle tri = _DrawTriangles[vertexID / 3];
        DrawVertex input = tri.vertices[vertexID % 3];
        
        output.pos = UnityObjectToClipPos(input.positionWS);
        output.positionWS = input.positionWS;
        float3 faceNormal = tri.normalOS;
        output.normalWS = faceNormal;
        
        output.uv = input.uv.xy;
        output.index = (int)ceil(input.uv.z);
        output.diffuseColor = input.diffuseColor;
        
        // making pointlights work requires v.vertex
        unityTransferVertexToFragmentSucksHack v;
        v.vertex = output.pos;
        
        TRANSFER_VERTEX_TO_FRAGMENT(output);
        UNITY_TRANSFER_FOG(output, output.pos);
        
        return output;
    }
    
    ENDCG


    SubShader
    {
        Cull Off

        Tags
        {
            "LightMode" = "ForwardBase"
            "RenderType" = "TransparentCutout"
            "Queue" = "Geometry+1"
        }

        Pass // basic color with directional lights
        {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag            
            
            float4 frag(v2f i) : SV_Target
            {
                // fade over the length of the grass
                float verticalFade = saturate(i.uv.y - _BottomTintOffset);
                // colors from the tool with tinting from the grass script
                float4 baseColor = lerp(_BottomTint, _TopTint, verticalFade) * float4(i.diffuseColor, 1);
                
                float4 final = baseColor;
                uint index = i.index;
                float4 diffuseColor = tex2DVariant(index, i.uv.xy);
                // lookup the diffuse alpha value and clip if below cutoff 
                // (DISABLE for full polygon fragment rendering)
                clip(diffuseColor.a - _Cutoff);


                // get ambient color from environment lighting
                float4 ambient = float4(ShadeSH9(float4(i.normalWS, 0)), 0);

#if defined(SHADOWS_SCREEN)
                // use unity light atten macro instead of SAMPLE_DEPTH_TEXTURE_PROJ to avoid compiler errors on mobile platforms
                UNITY_LIGHT_ATTENUATION(shadow, i, i.positionWS)
#else 
                float shadow = 1;
#endif
                final = baseColor * diffuseColor;

                // add in ambient
                final += (ambient * baseColor);

                // if theres a main light, multiply with its color and intensity 
#if defined(SHADOWS_SCREEN)
                final *= _LightColor0;
#endif  
                // add in shadows
                float4 dimAmbientColor = (ambient * final);
                // lift base shadow color by 15% to keep 
                final = lerp(dimAmbientColor, final, clamp(shadow + _ShadowAdjustment, 0, 1));

                
                // add fog
                UNITY_APPLY_FOG(i.fogCoord, final);
                return final;               
            }
            ENDCG
        }

        // point lights
        Pass
        {
            Tags
            {              
                "LightMode" = "ForwardAdd"
            }
            Blend OneMinusDstColor One
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag                                   
            #pragma multi_compile_fwdadd_fullshadows 
            
            float4 frag(v2f i) : SV_Target
            {
                float4 diffuseColor = tex2DVariant(i.index, i.uv.xy);
                clip(diffuseColor.a - _Cutoff);

                UNITY_LIGHT_ATTENUATION(atten, i, i.positionWS);
                
                float3 pointlights = (atten * _LightColor0.rgb);
                return float4(pointlights, 1);
            }
            ENDCG
        }


        Pass // shadow pass
        {

            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster

            float4 frag(v2f i) : SV_Target
            {
                // lookup the diffuse alpha value and clip if below cutoff
                // (DISABLE for simpler full polygon shadow caster)
                float4 diffuseColor = tex2DVariant(i.index, i.uv.xy);
                clip(diffuseColor.a - _Cutoff);

                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
        
        
    }     
    Fallback "Standard"
}
