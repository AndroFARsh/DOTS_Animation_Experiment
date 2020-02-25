#ifndef GPU_ANIMATION
#define GPU_ANIMATION
  
    StructuredBuffer<float4> _AnimationsBuffer;
   	
	inline float4x4 CreateMatrix(float frameOffset, float boneIndex) 
	{
	    int index = frameOffset + boneIndex * 3;

		float4 row0 = _AnimationsBuffer[index + 0];
		float4 row1 = _AnimationsBuffer[index + 1];
		float4 row2 = _AnimationsBuffer[index + 2];
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
	
#endif // GPU_ANIMATION