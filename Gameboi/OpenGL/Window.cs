using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Gameboi.Cartridges;
using Gameboi.Graphics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenAL;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Gameboi.OpenGL;

public unsafe class Window
{
    private readonly IWindow window;
    private GL? gl;
    private VertexArray? vertexArray;
    private VertexBuffer? vertexBuffer;
    private IndexBuffer? indexBuffer;
    private Texture? gameTexture;
    private Shaders? shaders;

    private UiLayer? uiLayer;
    private StartScreen? startScreen;
    private FilePicker? picker;

    private ImprovedGameboy? gameboy;
    private readonly SystemState state = new();
    private bool hasStartedAGame;

    private bool isPlaying;

    private readonly ALContext alc;
    private readonly AL al;
    private readonly Device* device;

    private readonly uint soundSource;
    private readonly uint[] soundBuffers = new uint[2];

    private readonly BufferFormat soundBufferFormat;
    private const int soundBufferSizeBytes = soundBufferFrequency * sizeof(short) * 2 * soundLatencyMs / 1000;
    private const int soundBufferFrequency = 44100;
    private const int soundLatencyMs = 50;


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
        window = Silk.NET.Windowing.Window.Create(options);

        window.Load += OnLoad;
        window.Update += OnUpdate;
        window.Render += OnRender;
        window.Closing += OnClose;
        window.Resize += OnResize;

        configFile = $"{Directory.GetCurrentDirectory()}/gameboi.ini";

        alc = ALContext.GetApi();
        al = AL.GetApi();
        device = alc.OpenDevice("");
        if (device is null)
        {
            throw new Exception("Failed to open audio device");
        }

        var context = alc.CreateContext(device, null);
        alc.MakeContextCurrent(context);

        var error = al.GetError();
        if (error != AudioError.NoError)
        {
            throw new Exception($"Failed to create audio context: {error}");
        }

        soundSource = al.GenSource();
        al.SetSourceProperty(soundSource, SourceBoolean.Looping, false);

        fixed (uint* sbuffs = soundBuffers)
        {
            al.GenBuffers(soundBuffers.Length, sbuffs);
        }

        soundBufferFormat = BufferFormat.Stereo16;
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
    private readonly string configFile;

    private readonly Stopwatch fpsStopWatch = new();

    private string currentTitle = "Gameboi";

    private void SetTitle(string title)
    {
        currentTitle = title;
        window.Title = title;
    }

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

        startScreen = new StartScreen(gl);
        startScreen.Show();

        uiLayer = new UiLayer(gl);

        pauseTextHandle = uiLayer.CreateText("paused", 8, (20 - 6) / 2, new(Rgb.white), new(Rgb.darkGray));
        savedTextHandle = uiLayer.CreateText("saved", 15, (20 - 5) / 2, new(Rgb.white), new(Rgb.darkGray));
        loadedTextHandle = uiLayer.CreateText("loaded", 15, (20 - 6) / 2, new(Rgb.white), new(Rgb.darkGray));

        var startDir = Directory.GetCurrentDirectory();

        if (File.Exists(configFile))
        {
            startDir = File.ReadLines(configFile).First().Split("=")[1];
        }

        picker = new FilePicker(gl, startDir);

        vertexArray = new(gl);
        vertexBuffer = new(gl, vertices);
        indexBuffer = new(gl, indices);

        vertexArray.AddBuffer(vertexBuffer);

        gameTexture = new Texture(gl, 0, null, 160, 144);

        shaders = new Shaders(gl, "OpenGL.Basic.shader");
        shaders.SetUniform("Game", 0);

        SetTitle(currentTitle);

        fpsStopWatch.Start();
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
                    startScreen?.Hide();

                    Unpause();

                    File.WriteAllLines(configFile, new string[] { $"lastRomDir={Directory.GetParent(rom)?.FullName ?? Directory.GetCurrentDirectory()}" });
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
        SetTitle(title);

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

        InitSound();
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
        al.SourcePause(soundSource);
    }


    public void Unpause()
    {
        if (!hasStartedAGame || isPlaying)
        {
            return;
        }
        isPlaying = true;
        uiLayer?.HideText(pauseTextHandle);
        al.SourcePlay(soundSource);
    }

    private void OnUpdate(double obj)
    {
        UpdateFpsInTitle();

        if (isPlaying)
        {
            OnFrameUpdate?.Invoke();
            gameboy?.PlayFrame();
            UpdateSound();
        }
        picker?.Update();

        if (snackbarTimer.IsRunning && snackbarTimer.ElapsedMilliseconds >= snackbarDuration)
        {
            snackbarTimer.Reset();
            uiLayer?.HideText(snackbarTextHandle);
        }
    }

    private void UpdateFpsInTitle()
    {
        var elapsedTime = fpsStopWatch.ElapsedTicks / (double)Stopwatch.Frequency;
        fpsStopWatch.Restart();
        var fps = 1 / elapsedTime;
        window.Title = $"{currentTitle} - {fps:F0} FPS";
    }

    private void InitSound()
    {
        // Remove any buffers that might be queued
        al.SourceStop(soundSource);
        fixed (uint* sbuffs = soundBuffers)
        {
            al.SourceUnqueueBuffers(soundSource, soundBuffers.Length, sbuffs);
        }

        // Queue silent buffers
        var bufferData = new short[soundBufferSizeBytes / sizeof(short)];
        fixed (short* buffData = bufferData)
        {
            al.BufferData(soundBuffers[0], soundBufferFormat, buffData, soundBufferSizeBytes, soundBufferFrequency);
            al.BufferData(soundBuffers[1], soundBufferFormat, buffData, soundBufferSizeBytes, soundBufferFrequency);
        }
        fixed (uint* sbuffs = soundBuffers)
        {
            al.SourceQueueBuffers(soundSource, 2, sbuffs);
        }

        // Start playing
        al.SourcePlay(soundSource);
        nextBufferToQueue = 2 % soundBuffers.Length;
    }

    private int nextBufferToQueue = 0;
    private int oldestBufferInQueue = 0;
    private int nextStartIndex = 0;

    private void UpdateSound()
    {
        al.GetSourceProperty(soundSource, GetSourceInteger.BuffersProcessed, out var buffersProcessed);
        if (buffersProcessed > 1)
        {
            Console.WriteLine($"Warning: {buffersProcessed} buffers were processed since last frame. Expected at most {soundBuffers.Length - 1}.");
        }
        for (var i = 0; i < buffersProcessed; i++)
        {
            fixed (uint* ptr = &soundBuffers[oldestBufferInQueue])
            {
                al.SourceUnqueueBuffers(soundSource, 1, ptr);
                oldestBufferInQueue += 1;
                oldestBufferInQueue %= soundBuffers.Length;
            }

            fixed (short* data = &state.SampleBuffer[nextStartIndex])
            {
                al.BufferData(soundBuffers[nextBufferToQueue], soundBufferFormat, data, soundBufferSizeBytes, soundBufferFrequency);
                nextStartIndex += soundBufferSizeBytes / sizeof(short);
                nextStartIndex %= state.SampleBuffer.Length;
            }

            fixed (uint* ptr = &soundBuffers[nextBufferToQueue])
            {
                al.SourceQueueBuffers(soundSource, 1, ptr);
                nextBufferToQueue += 1;
                nextBufferToQueue %= soundBuffers.Length;
            }
        }

        al.GetSourceProperty(soundSource, GetSourceInteger.SourceState, out var sourceState);
        if ((SourceState)sourceState is SourceState.Stopped)
        {
            al.SourcePlay(soundSource);
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

        startScreen?.Render();

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

        al.SourceStop(soundSource);

        al.DeleteSource(soundSource);
        al.DeleteBuffers(soundBuffers);

        alc.CloseDevice(device);

        al.Dispose();
    }

    private void OnResize(Vector2D<int> newScreenSize)
    {
        gl!.Viewport(0, 0, (uint)newScreenSize.X, (uint)newScreenSize.Y);
    }
}
