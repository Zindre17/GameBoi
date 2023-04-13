#shader vertex
#version 330 core

layout (location = 0) in vec4 Position;
layout (location = 1) in vec2 TextureCoordinates;
layout (location = 2) in vec4 ForegroundColor;
layout (location = 3) in vec4 BackgroundColor;

out vec2 TexCoord;
out vec4 ForColor;
out vec4 BackColor;

void main()
{
    gl_Position = Position;
    TexCoord = TextureCoordinates;
    ForColor = ForegroundColor;
    BackColor = BackgroundColor;
}


#shader fragment
#version 330 core

out vec4 FragColor;

in vec2 TexCoord;
in vec4 ForColor;
in vec4 BackColor;

uniform sampler2D Font;

void main()
{
    float texValue = texture(Font, TexCoord).r;
    
    if(texValue == 1){
        FragColor = ForColor;
    }
    else{
        FragColor = BackColor;
    }
}
