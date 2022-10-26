using GB_Emulator.Gameboi;
using GB_Emulator.Gameboi.Graphics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Gameboy;

public class Window
{
    private readonly IWindow window;
    private GL? gl;
    private VertexArray? vertexArray;
    private VertexBuffer? vertexBuffer;
    private IndexBuffer? indexBuffer;
    private Texture? backgroundTexture;
    private Texture? windowTexture;
    private Texture? spriteTexture;
    private Shaders? shaders;

    private readonly Gameboi gameboy = new();


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

        backgroundTexture = new Texture(gl, 0, null, 160, 144);
        windowTexture = new Texture(gl, 1, null, 160, 144);
        spriteTexture = new Texture(gl, 2, null, 160, 144);

        shaders = new Shaders(gl);
        shaders.Bind();

        shaders.SetUniform("Background", 0);
        shaders.SetUniform("Window", 1);
        shaders.SetUniform("Sprites", 2);

        gameboy.OnPixelRowReady += UploadPixelRow;
    }

    private void UploadPixelRow(byte line, Rgba[] pixelRow)
    {
        backgroundTexture?.FeedData<Rgba>(pixelRow, 0, line, (uint)(pixelRow.Length), 1);
    }

    private void OnKeyReleased(IKeyboard _, Key key, int __)
    {
        gameboy.Controller.KeyUp(key);
    }

    private void OnKeyPressed(IKeyboard _, Key key, int __)
    {
        gameboy.Controller.KeyDown(key);
        if (key is Key.Space)
        {
            gameboy.PausePlayToggle();
        }
        if (key is Key.Escape)
        {
            //TODO add file picker.
            gameboy.LoadGame("./roms/Pokemon Red.gb");
        }
    }

    private void OnUpdate(double obj)
    {
        if (gameboy.IsPlaying)
        {
            gameboy.PlayForOneFrame();
        }
    }

    unsafe private void OnRender(double obj)
    {
        gl!.Clear(ClearBufferMask.ColorBufferBit);
        vertexArray!.Bind();
        indexBuffer!.Bind();
        shaders!.Bind();
        gl!.DrawElements(GLEnum.Triangles, indexBuffer!.Count, GLEnum.UnsignedInt, null);
    }

    private void OnClose()
    {
        vertexArray!.Dispose();

        indexBuffer!.Dispose();
        vertexBuffer!.Dispose();

        shaders!.Dispose();

        backgroundTexture!.Dispose();
        windowTexture!.Dispose();
        spriteTexture!.Dispose();
    }

    private void OnResize(Vector2D<int> newScreenSize)
    {
        gl!.Viewport(0, 0, (uint)newScreenSize.X, (uint)newScreenSize.Y);
    }

}
