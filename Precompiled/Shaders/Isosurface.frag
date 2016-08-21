#version 440

uniform vec4 modelColor;
uniform vec3 cameraDirection;

in VS_OUT
{
	vec3 normal;
} fs_in;

out vec4 color;

void main(void)
{	
	float modulation = abs(dot(fs_in.normal, cameraDirection));
	
	color = vec4(modelColor.rgb * modulation, modelColor.a);
}