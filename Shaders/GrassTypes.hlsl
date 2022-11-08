#ifndef COMPUTEGRASSTYPES_INCLUDED
#define COMPUTEGRASSTYPES_INCLUDED


#define RGBA_BLACK float4(0, 0, 0, 1)
#define RGBA_WHITE float4(1, 1, 1, 1)
#define RGBA_ERROR float4(1, 0, 0.5f, 1)


// This describes the blade variant parameters used for compute and graphics shader (excluding Textures)
struct BladeArgs
{
	float height;
	float randomHeight;
	float aspectRatio;
	float radius;
	float forward;
	float curve;
	float rootWidth;
};

// This describes a vertex on the source mesh
struct SourceVertex
{
	float3 positionOS; // position in object space
	float3 normalOS;
	float2 uv;
};


// The indirect draw call args, as described in the renderer script
struct IndirectDrawArgs
{
	uint numVerticesPerInstance;
	uint numInstances;
	uint startVertexIndex;
	uint startInstanceIndex;
};


// This describes a vertex on the computed mesh (passed to graphics shader as part of DrawTriangle)
struct DrawVertex
{
	float3 positionWS; // The position in world space 
	float3 uv;
	float3 diffuseColor;
};

// A triangle on the generated mesh
struct DrawTriangle
{
	float3 normalOS;
	DrawVertex vertices[3]; // The three points on the triangle
};


#endif // COMPUTEGRASSTYPES_INCLUDED
