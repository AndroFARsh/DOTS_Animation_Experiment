#ifndef GPU_ANIMATION
#define GPU_ANIMATION
  
    sampler2D _Animations;
    float4 _AnimationsSize;
   	
   	inline float4 indexToUV(in float index) 
   	{
   	    float x = (index % _AnimationsSize.x) / _AnimationsSize.x;
   	    float y = (index / _AnimationsSize.x) / _AnimationsSize.y;
        
        return float4(x, y, 0, 0);
   	}
   	
   	inline float4x4 CreateMatrix(in float frameOffset, in float boneIndex) 
	{
	    float index = frameOffset + boneIndex * 3;

        float4 row0 = tex2Dlod( _Animations, indexToUV(index + 0));
        float4 row1 = tex2Dlod( _Animations, indexToUV(index + 1));
        float4 row2 = tex2Dlod( _Animations, indexToUV(index + 2));
        float4 row3 = float4(0, 0, 0, 1);  

        float4x4 m = float4x4(row0, row1, row2, row3);
		return m;
	}

	inline float4x4 AnimationMatrix(in float2 uv1, in float frameOffset) 
	{
	    float4x4 m = CreateMatrix(frameOffset, uv1.x) * uv1.y;

        return m;
	}
	
	inline float4x4 AnimationMatrix(in float4 uv1, in float frameOffset) 
	{
	    float4x4 m0 = CreateMatrix(frameOffset, uv1.x) * uv1.y;
        float4x4 m1 = CreateMatrix(frameOffset, uv1.z) * uv1.w;
				      
        float4x4 m = m0 + m1;
        return m;
	}
	
	inline float4x4 AnimationMatrix(in float4 uv1, in float4 uv2, in float frameOffset) 
	{
	    float4x4 m0 = CreateMatrix(frameOffset, uv1.x) * uv1.y;
        float4x4 m1 = CreateMatrix(frameOffset, uv1.z) * uv1.w;
        float4x4 m2 = CreateMatrix(frameOffset, uv2.x) * uv2.y;
        float4x4 m3 = CreateMatrix(frameOffset, uv2.z) * uv2.w;
				      
        float4x4 m = m0 + m1 + m2 + m3;
        return m;
	}
	
	
    #if UNITY_ANY_INSTANCING_ENABLED
        #if UNITY_INSTANCING_ANIMATIO_ARRAY
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float,  _SkinMatricesOffset)
            UNITY_INSTANCING_BUFFER_END(Props)    
        #else    
            Buffer<float> _SkinMatricesOffset;
        #endif    
    #endif

    void updateVectors_float(in float4 animtionUV, 
        in float4 positionOS, in float4 normalOS, in float4 tangentOS,
        out float4 positionOS_out, out float4 normalOS_out, out float4 tangentOS_out)
    {
        positionOS_out = positionOS;
        normalOS_out = normalOS;
        tangentOS_out = tangentOS;
        
    #if UNITY_ANY_INSTANCING_ENABLED 
    #if UNITY_INSTANCING_ANIMATIO_ARRAY
        float frameOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _SkinMatricesOffset);
    #else
        float frameOffset = _SkinMatricesOffset[unity_InstanceID];
    #endif
        float4x4 animationMatrix = AnimationMatrix(animtionUV, frameOffset);
        
        positionOS_out = mul(animationMatrix, positionOS);
        normalOS_out = mul(animationMatrix, normalOS);
        tangentOS_out = mul(animationMatrix, tangentOS);
    #endif
    }
    
    void updateVectors_float(in float2 animtionUV, 
        in float4 positionOS, in float4 normalOS, in float4 tangentOS,
        out float4 positionOS_out, out float4 normalOS_out, out float4 tangentOS_out)
    {
        positionOS_out = positionOS;
        normalOS_out = normalOS;
        tangentOS_out = tangentOS;
        
    #if UNITY_ANY_INSTANCING_ENABLED 
    #if UNITY_INSTANCING_ANIMATIO_ARRAY
        float frameOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _SkinMatricesOffset);
    #else
        float frameOffset = _SkinMatricesOffset[unity_InstanceID];
    #endif
        float4x4 animationMatrix = AnimationMatrix(animtionUV, frameOffset);
        
        positionOS_out = mul(animationMatrix, positionOS);
        normalOS_out = mul(animationMatrix, normalOS);
        tangentOS_out = mul(animationMatrix, tangentOS);
    #endif
    }
	
#endif // GPU_ANIMATION