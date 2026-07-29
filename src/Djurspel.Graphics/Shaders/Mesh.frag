#version 330 core

in vec3 vNormal;
in vec2 vTexCoord;
in vec3 vPosition;

uniform vec3 u_Color;
uniform vec4 u_TintColor;
uniform sampler2D u_Texture;
uniform int u_HasTexture;

out vec4 FragColor;

void main()
{
    // Simple directional lighting
    vec3 lightDir = normalize(vec3(0.5, 1.0, 0.3));
    float diff = max(dot(normalize(vNormal), lightDir), 0.0);
    float ambient = 0.3;
    float lighting = ambient + diff * 0.7;

    vec3 color = u_Color * lighting;

    if (u_HasTexture == 1)
    {
        vec4 texColor = texture(u_Texture, vTexCoord);
        color *= texColor.rgb * u_TintColor.rgb;
    }
    else
    {
        color *= u_TintColor.rgb;
    }

    FragColor = vec4(color, u_TintColor.a);
}