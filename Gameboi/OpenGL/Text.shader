#shader vertex
#version 330 core

layout (location = 0) in vec4 Position;
layout (location = 1) in vec2 TextureCoordinates;

out vec2 TexCoord;

void main()
{
    gl_Position = Position;
    TexCoord = TextureCoordinates;
}


#shader fragment
#version 330 core

out vec4 FragColor;

in vec2 TexCoord;

uniform sampler2D Font;

void main()
{
    FragColor = texture(Font, TexCoord);
}
