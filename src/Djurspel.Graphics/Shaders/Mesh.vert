#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord;

uniform mat4 u_Model;
uniform mat4 u_Projection;

out vec3 vNormal;
out vec2 vTexCoord;
out vec3 vPosition;

void main()
{
    vec4 worldPos = u_Model * vec4(aPosition, 1.0);
    gl_Position = u_Projection * worldPos;
    vNormal = aNormal;
    vTexCoord = aTexCoord;
    vPosition = worldPos.xyz;
}