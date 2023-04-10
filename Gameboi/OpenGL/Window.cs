using System;
using Gameboi.Cartridges;
using Gameboi.Extensions;
using Gameboi.Graphics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Gameboi.OpenGL;

public class Window
{
    private readonly IWindow window;
    private GL? gl;
    private VertexArray? vertexArray;
    private VertexArray? textVertexArray;
    private VertexBuffer? vertexBuffer;
    private VertexBuffer? textVertexBuffer;
    private IndexBuffer? indexBuffer;
    private IndexBuffer? textIndexBuffer;
    private Texture? gameTexture;
    private Texture? fontTexture;
    private Shaders? shaders;
    private Shaders? fontShaders;

    private ImprovedGameboy? gameboy;
    private readonly SystemState state = new();

    private bool isPlaying;

    private static readonly float[] vertices = new float[]{
    //    x,   y, tx, ty,
        -1f, -1f, 0f, 1f,
         1f, -1f, 1f, 1f,
         1f,  1f, 1f, 0f,
        -1f,  1f, 0f, 0f
    };

    private static readonly uint[] indices = new uint[]{
        0, 1, 2, 2, 3, 0
    };

    private readonly byte[] fontTextureData = new byte[]{
        0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000,
        0b_00011000, 0b_00111000, 0b_00011100, 0b_00111000, 0b_00111100, 0b_00111100, 0b_00011100, 0b_00100100, 0b_00011100, 0b_00001110, 0b_00100100, 0b_00100000, 0b_00100010, 0b_00100010, 0b_00011100, 0b_00111000, 0b_00011100, 0b_00111000, 0b_00011100, 0b_00111100, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00111110,
        0b_00100100, 0b_00100100, 0b_00100010, 0b_00100100, 0b_00100000, 0b_00100000, 0b_00100010, 0b_00100100, 0b_00001000, 0b_00000100, 0b_00101000, 0b_00100000, 0b_00110110, 0b_00110010, 0b_00100010, 0b_00100100, 0b_00100010, 0b_00100100, 0b_00100010, 0b_00001000, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00010100, 0b_00100010, 0b_00000100,
        0b_00100100, 0b_00111000, 0b_00100000, 0b_00100100, 0b_00111100, 0b_00111100, 0b_00100000, 0b_00111100, 0b_00001000, 0b_00000100, 0b_00110000, 0b_00100000, 0b_00101010, 0b_00101010, 0b_00100010, 0b_00111000, 0b_00100010, 0b_00111000, 0b_00010000, 0b_00001000, 0b_00100010, 0b_00100010, 0b_00101010, 0b_00001000, 0b_00010100, 0b_00001000,
        0b_00111100, 0b_00100100, 0b_00100000, 0b_00100100, 0b_00100000, 0b_00100000, 0b_00101110, 0b_00100100, 0b_00001000, 0b_00000100, 0b_00101000, 0b_00100000, 0b_00100010, 0b_00100110, 0b_00100010, 0b_00100000, 0b_00100010, 0b_00101000, 0b_00001100, 0b_00001000, 0b_00100010, 0b_00010100, 0b_00101010, 0b_00010100, 0b_00001000, 0b_00010000,
        0b_00100100, 0b_00100100, 0b_00100010, 0b_00100100, 0b_00100000, 0b_00100000, 0b_00100010, 0b_00100100, 0b_00001000, 0b_00100100, 0b_00100100, 0b_00100000, 0b_00100010, 0b_00100010, 0b_00100010, 0b_00100000, 0b_00100110, 0b_00100100, 0b_00100010, 0b_00001000, 0b_00100010, 0b_00010100, 0b_00101010, 0b_00100010, 0b_00001000, 0b_00100000,
        0b_00100100, 0b_00111000, 0b_00011100, 0b_00111000, 0b_00111100, 0b_00100000, 0b_00011100, 0b_00100100, 0b_00011100, 0b_00011000, 0b_00100100, 0b_00111100, 0b_00100010, 0b_00100010, 0b_00011100, 0b_00100000, 0b_00011110, 0b_00100100, 0b_00011100, 0b_00001000, 0b_00011100, 0b_00001000, 0b_00010100, 0b_00100010, 0b_00001000, 0b_00111110,
        0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000001, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000, 0b_00000000,
    };

    private readonly float[] fontVertices = new float[]{
    //    x,   y, tx, ty,
        -0.5f, -0.5f, 0f, 1f,
         0.5f, -0.5f, 0.5f, 1f,
         0.5f,  0.5f, 0.5f, 0f,
        -0.5f,  0.5f, 0f, 0f
    };

    public Window()
    {
        var options = WindowOptions.Default;
        options.VSync = false; // if true the fps is ignored.
        options.FramesPerSecond = 60;
        options.UpdatesPerSecond = 60;
        options.Size = new Vector2D<int>(800, 600);
        options.Title = "Gameboi";
        window = Silk.NET.Windowing.Window.Create(options);

        window.Load += OnLoad;
        window.Update += OnUpdate;
        window.Render += OnRender;
        window.Closing += OnClose;
        window.Resize += OnResize;
    }

    public SystemState State => state;

    public Action? OnFrameUpdate { get; set; }

    public void Run() => window.Run();

    private void OnLoad()
    {
        gl = GL.GetApi(window);

        var input = window.CreateInput();
        foreach (var keyboard in input.Keyboards)
        {
            keyboard.KeyDown += OnKeyPressed;
            keyboard.KeyUp += OnKeyReleased;
        }

        vertexArray = new(gl);
        vertexBuffer = new(gl, vertices);
        indexBuffer = new(gl, indices);

        vertexArray.AddBuffer(vertexBuffer);

        gameTexture = new Texture(gl, 0, null, 160, 144);

        shaders = new Shaders(gl, "OpenGL.Basic.shader");
        shaders.SetUniform("Game", 0);

        fontTexture = new Texture(gl, 1, null, 8 * 26, 8);
        fontTexture.FeedData(ProcessFontData());

        textVertexArray = new VertexArray(gl);
        textVertexBuffer = new VertexBuffer(gl, fontVertices);
        textIndexBuffer = new IndexBuffer(gl, indices);

        textVertexArray.AddBuffer(textVertexBuffer);

        fontShaders = new Shaders(gl, "OpenGL.Text.shader");
        fontShaders.SetUniform("Font", 1);
    }

    private Rgba[] ProcessFontData()
    {
        var colors = new Rgba[8 * 8 * 26];
        for (var i = 0; i < fontTextureData.Length; i++)
        {
            var data = fontTextureData[i];
            for (var j = 0; j < 8; j++)
            {
                var index = 8 * i + j;
                colors[index] = new(data.IsBitSet(7 - j) ? Rgb.white : Rgb.darkGray);
            }
        }
        return colors;
    }

    private void UploadPixelRow(byte line, Rgba[] pixelRow)
    {
        gameTexture?.FeedData<Rgba>(pixelRow, 0, line, (uint)pixelRow.Length, 1);
    }

    private void OnKeyReleased(IKeyboard _, Key key, int __)
    {
        gameboy?.Joypad.KeyUp(key);
    }

    private void OnKeyPressed(IKeyboard _, Key key, int __)
    {
        gameboy?.Joypad.KeyDown(key);
        if (key is Key.Space)
        {
            isPlaying = !isPlaying;
        }
        if (key is Key.Escape)
        {
            //TODO add file picker.
            var game = RomReader.ReadRom("./roms/Pokemon Red.gb");
            ChangeGame(game);
        }
    }

    public void ChangeGame(RomCartridge game)
    {
        isPlaying = false;

        var gameHeader = new GameHeader(game.Rom);
        window.Title = gameHeader.GetTitle();

        state.ChangeGame(game.Rom, game.Ram, gameHeader.IsColorGame);

        var mbcLogic = MbcFactory.GetMbcLogic(game.Type, state);
        gameboy = new ImprovedGameboy(state, mbcLogic);
        gameboy.OnPixelRowReady += UploadPixelRow;

        isPlaying = true;
    }

    private void OnUpdate(double obj)
    {
        if (isPlaying)
        {
            OnFrameUpdate?.Invoke();
            gameboy?.PlayFrame();
        }
    }

    unsafe private void OnRender(double obj)
    {
        gl!.Clear(ClearBufferMask.ColorBufferBit);

        // Draw gameboy screen
        vertexArray!.Bind();
        indexBuffer!.Bind();
        shaders!.Bind();
        gl!.DrawElements(GLEnum.Triangles, indexBuffer!.Count, GLEnum.UnsignedInt, null);

        // Draw emulator UI
        textVertexArray!.Bind();
        textIndexBuffer!.Bind();
        fontShaders!.Bind();
        gl!.DrawElements(GLEnum.Triangles, textIndexBuffer!.Count, GLEnum.UnsignedInt, null);
    }

    private void OnClose()
    {
        vertexArray?.Dispose();
        indexBuffer?.Dispose();
        vertexBuffer?.Dispose();
        shaders?.Dispose();
        gameTexture?.Dispose();

        textVertexArray?.Dispose();
        textIndexBuffer?.Dispose();
        textVertexBuffer?.Dispose();
        fontShaders?.Dispose();
        fontTexture?.Dispose();
    }

    private void OnResize(Vector2D<int> newScreenSize)
    {
        gl!.Viewport(0, 0, (uint)newScreenSize.X, (uint)newScreenSize.Y);
    }
}
