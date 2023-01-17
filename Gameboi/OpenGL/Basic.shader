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

uniform sampler2D Background;
uniform sampler2D Window;
uniform sampler2D Sprites;

void main()
{
    vec4 st, wt, bt;
    st = texture(Sprites, TexCoord);
    wt = texture(Window, TexCoord);
    bt = texture(Background, TexCoord);
    // FragColor = vec4(1,1,1,1);
    FragColor = bt;
    // FragColor = mix(bt, mix(wt, st, st.a), max(wt.a, st.a));
}
