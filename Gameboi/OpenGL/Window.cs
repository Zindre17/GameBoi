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
    private VertexBuffer? vertexBuffer;
    private IndexBuffer? indexBuffer;
    private Texture? gameTexture;
    private Shaders? shaders;

    private UiLayer? uiLayer;

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

        uiLayer = new UiLayer(gl);
        uiLayer.ShowText("Hello World", 0, 0);

        vertexArray = new(gl);
        vertexBuffer = new(gl, vertices);
        indexBuffer = new(gl, indices);

        vertexArray.AddBuffer(vertexBuffer);

        gameTexture = new Texture(gl, 0, null, 160, 144);

        shaders = new Shaders(gl, "OpenGL.Basic.shader");
        shaders.SetUniform("Game", 0);
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

        uiLayer?.Render();
    }

    private void OnClose()
    {
        vertexArray?.Dispose();
        indexBuffer?.Dispose();
        vertexBuffer?.Dispose();
        shaders?.Dispose();
        gameTexture?.Dispose();

        uiLayer?.Dispose();
    }

    private void OnResize(Vector2D<int> newScreenSize)
    {
        gl!.Viewport(0, 0, (uint)newScreenSize.X, (uint)newScreenSize.Y);
    }
}
