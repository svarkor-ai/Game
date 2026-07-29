#version 330 core

in vec3 vNormal;
in vec2 vTexCoord;

uniform vec4 u_TintColor;
uniform sampler2D u_Texture;
uniform int u_HasTexture;

out vec4 FragColor;

void main()
{
    vec3 lightDir = normalize(vec3(0.5, 1.0, 0.3));
    float diff = max(dot(normalize(vNormal), lightDir), 0.0);
    float ambient = 0.4;
    float lighting = ambient + diff * 0.6;

    vec3 color = u_TintColor.rgb * lighting;

    if (u_HasTexture == 1)
    {
        vec4 texColor = texture(u_Texture, vTexCoord);
        color *= texColor.rgb;
    }

    FragColor = vec4(color, u_TintColor.a);
}