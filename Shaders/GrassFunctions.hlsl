#ifndef COMPUTEGRASSFUNCTIONS_INCLUDED
#define COMPUTEGRASSFUNCTIONS_INCLUDED


// macros used in graphics and compute shader

#define SAMPLE_VARIANT(id, indexName, texcoords) if (indexName == id) return tex2D(_Variant##id, texcoords);
#define SAMPLE_ELSEVARIANT(id, indexName, texcoords) else if (indexName == id) return tex2D(_Variant##id, texcoords);


// Helper Functions
float rand(float3 co)
{
	return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
}

// A function to compute an rotation matrix which rotates a point
// by angle radians around the given axis
// By Keijiro Takahashi
float3x3 AngleAxis3x3(float angle, float3 axis)
{
	float c, s;
	sincos(angle, s, c);

	float t = 1 - c;
	float x = axis.x;
	float y = axis.y;
	float z = axis.z;

	return float3x3(
		t * x * x + c, t * x * y - s * z, t * x * z + s * y,
		t * x * y + s * z, t * y * y + c, t * y * z - s * x,
		t * x * z - s * y, t * y * z + s * x, t * z * z + c);
}


#endif // COMPUTEGRASSFUNCTIONS_INCLUDED
