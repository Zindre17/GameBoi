using System;
using System.Diagnostics;
using System.IO;
using Gameboi.Cartridges;
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
    private FilePicker? picker;

    private ImprovedGameboy? gameboy;
    private readonly SystemState state = new();
    private bool hasStartedAGame;

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

    private int pauseTextHandle;
    private int savedTextHandle;
    private int loadedTextHandle;
    private const long snackbarDuration = 2000;
    private int snackbarTextHandle;
    private readonly Stopwatch snackbarTimer = new();

    private void OnLoad()
    {
        gl = GL.GetApi(window);

        gl.Enable(GLEnum.Blend);
        gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        var input = window.CreateInput();
        foreach (var keyboard in input.Keyboards)
        {
            keyboard.KeyDown += OnKeyPressed;
            keyboard.KeyUp += OnKeyReleased;
        }

        uiLayer = new UiLayer(gl);

        pauseTextHandle = uiLayer.CreateText("paused", 8, (20 - 6) / 2, new(Rgb.white), new(Rgb.darkGray));
        savedTextHandle = uiLayer.CreateText("saved", 15, (20 - 5) / 2, new(Rgb.white), new(Rgb.darkGray));
        loadedTextHandle = uiLayer.CreateText("loaded", 15, (20 - 6) / 2, new(Rgb.white), new(Rgb.darkGray));

        picker = new FilePicker(gl);

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
        picker?.OnKeyReleased(key);
    }

    private void OnKeyPressed(IKeyboard _, Key key, int __)
    {
        gameboy?.Joypad.KeyDown(key);

        var wasOpen = picker?.IsOpen;

        if (key is Key.Space)
        {
            if (isPlaying)
            {
                Pause();
            }
            else
            {
                Unpause();
            }
        }
        else if (key is Key.Escape)
        {
            if (picker?.IsOpen is false)
            {
                Pause();
                picker?.SelectFile(rom =>
                {
                    Unpause();
                    var game = RomReader.ReadRom(rom);
                    ChangeGame(game);
                });
            }
        }
        else if (key is Key.Number6)
        {
            LoadSnapshot();
        }
        else if (key is Key.Number9)
        {
            SaveSnapshot();
        }

        if (wasOpen is true)
        {
            picker?.OnKeyPressed(key);
        }
    }

    private void SaveSnapshot()
    {
        ShowSnackbarText(savedTextHandle);

        var dir = $"{Directory.GetCurrentDirectory()}/snapshots";
        Directory.CreateDirectory(dir);

        var gameHeader = new GameHeader(state.CartridgeRom);
        var title = gameHeader.GetTitle();

        var file = $"{dir}/{title}.snpsht";

        File.WriteAllBytes(file, state.ToArray());
    }

    private void LoadSnapshot()
    {
        ShowSnackbarText(loadedTextHandle);

        var dir = $"{Directory.GetCurrentDirectory()}/snapshots";
        Directory.CreateDirectory(dir);

        var gameHeader = new GameHeader(state.CartridgeRom);
        var title = gameHeader.GetTitle();

        var file = $"{dir}/{title}.snpsht";

        state.LoadState(File.ReadAllBytes(file));
    }

    private void ShowSnackbarText(int textHandle)
    {
        if (snackbarTimer.IsRunning)
        {
            snackbarTimer.Reset();
            uiLayer?.HideText(snackbarTextHandle);
        }

        snackbarTimer.Start();
        snackbarTextHandle = textHandle;
        uiLayer?.ShowText(textHandle);
    }

    private string? saveFile;

    public void ChangeGame(RomCartridge game)
    {
        isPlaying = false;
        SaveCurrentGame();

        var gameHeader = new GameHeader(game.Rom);
        var title = gameHeader.GetTitle();
        window.Title = title;

        state.ChangeGame(game.Rom, game.Ram);

        if (gameHeader.HasRamAndBattery)
        {
            var dir = $"{Directory.GetCurrentDirectory()}/saves";
            Directory.CreateDirectory(dir);

            saveFile = $"{dir}/{title}.gameboi";
            if (File.Exists(saveFile))
            {
                var ram = File.ReadAllBytes(saveFile);
                Array.Copy(ram, game.Ram, ram.Length);
            }
        }
        else
        {
            saveFile = null;
        }


        var mbcLogic = MbcFactory.GetMbcLogic(game.Type, state);
        gameboy = new ImprovedGameboy(state, mbcLogic);
        gameboy.OnPixelRowReady += UploadPixelRow;

        isPlaying = true;
        hasStartedAGame = true;
    }

    private void SaveCurrentGame()
    {
        if (gameboy is null || saveFile is null)
        {
            return;
        }
        File.WriteAllBytes(saveFile, state.CartridgeRam);
    }

    public void Pause()
    {
        if (!hasStartedAGame || !isPlaying)
        {
            return;
        }
        isPlaying = false;
        uiLayer?.ShowText(pauseTextHandle);
    }


    public void Unpause()
    {
        if (!hasStartedAGame || isPlaying)
        {
            return;
        }
        isPlaying = true;
        uiLayer?.HideText(pauseTextHandle);
    }

    private void OnUpdate(double obj)
    {
        if (isPlaying)
        {
            OnFrameUpdate?.Invoke();
            gameboy?.PlayFrame();
        }
        picker?.Update();

        if (snackbarTimer.IsRunning && snackbarTimer.ElapsedMilliseconds >= snackbarDuration)
        {
            snackbarTimer.Reset();
            uiLayer?.HideText(snackbarTextHandle);
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

        picker?.Render();
    }

    private void OnClose()
    {
        SaveCurrentGame();

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
