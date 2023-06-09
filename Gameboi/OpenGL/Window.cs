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
    private int mutedTextHandle;
    private int unmutedTextHandle;
    private readonly int[] volumeTextHandles = new int[11];
    private readonly int[] speedTextHandles = new int[7];
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
        mutedTextHandle = uiLayer.CreateText("muted", 15, (20 - 5) / 2, new(Rgb.white), new(Rgb.darkGray));
        unmutedTextHandle = uiLayer.CreateText("unmuted", 15, (20 - 7) / 2, new(Rgb.white), new(Rgb.darkGray));
        volumeTextHandles[0] = uiLayer.CreateText("volume: 0%", 15, (20 - 10) / 2, new(Rgb.white), new(Rgb.darkGray));
        volumeTextHandles[1] = uiLayer.CreateText("volume: 10%", 15, (20 - 11) / 2, new(Rgb.white), new(Rgb.darkGray));
        volumeTextHandles[2] = uiLayer.CreateText("volume: 20%", 15, (20 - 11) / 2, new(Rgb.white), new(Rgb.darkGray));
        volumeTextHandles[3] = uiLayer.CreateText("volume: 30%", 15, (20 - 11) / 2, new(Rgb.white), new(Rgb.darkGray));
        volumeTextHandles[4] = uiLayer.CreateText("volume: 40%", 15, (20 - 11) / 2, new(Rgb.white), new(Rgb.darkGray));
        volumeTextHandles[5] = uiLayer.CreateText("volume: 50%", 15, (20 - 11) / 2, new(Rgb.white), new(Rgb.darkGray));
        volumeTextHandles[6] = uiLayer.CreateText("volume: 60%", 15, (20 - 11) / 2, new(Rgb.white), new(Rgb.darkGray));
        volumeTextHandles[7] = uiLayer.CreateText("volume: 70%", 15, (20 - 11) / 2, new(Rgb.white), new(Rgb.darkGray));
        volumeTextHandles[8] = uiLayer.CreateText("volume: 80%", 15, (20 - 11) / 2, new(Rgb.white), new(Rgb.darkGray));
        volumeTextHandles[9] = uiLayer.CreateText("volume: 90%", 15, (20 - 11) / 2, new(Rgb.white), new(Rgb.darkGray));
        volumeTextHandles[10] = uiLayer.CreateText("volume: 100%", 15, (20 - 12) / 2, new(Rgb.white), new(Rgb.darkGray));
        speedTextHandles[0] = uiLayer.CreateText("speed: 0.125x", 15, (20 - 13) / 2, new(Rgb.white), new(Rgb.darkGray));
        speedTextHandles[1] = uiLayer.CreateText("speed: 0.25x", 15, (20 - 12) / 2, new(Rgb.white), new(Rgb.darkGray));
        speedTextHandles[2] = uiLayer.CreateText("speed: 0.5x", 15, (20 - 11) / 2, new(Rgb.white), new(Rgb.darkGray));
        speedTextHandles[3] = uiLayer.CreateText("speed: normal", 15, (20 - 13) / 2, new(Rgb.white), new(Rgb.darkGray));
        speedTextHandles[4] = uiLayer.CreateText("speed: 2x", 15, (20 - 9) / 2, new(Rgb.white), new(Rgb.darkGray));
        speedTextHandles[5] = uiLayer.CreateText("speed: 4x", 15, (20 - 9) / 2, new(Rgb.white), new(Rgb.darkGray));
        speedTextHandles[6] = uiLayer.CreateText("speed: 8x", 15, (20 - 9) / 2, new(Rgb.white), new(Rgb.darkGray));
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
        gameboy?.Controller.KeyUp(key);
        picker?.OnKeyReleased(key);
    }

    private float currentVolume = 1f;
    private bool isMuted = false;

    private void OnKeyPressed(IKeyboard _, Key key, int __)
    {
        gameboy?.Controller.KeyDown(key);

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
        else if (key is Key.M)
        {
            if (isPlaying)
            {
                if (isMuted && currentVolume is 0)
                {
                    currentVolume = 1f;
                }

                isMuted = !isMuted;
                al.SetSourceProperty(soundSource, SourceFloat.Gain, isMuted ? 0 : currentVolume);
                ShowSnackbarText(isMuted ? mutedTextHandle : unmutedTextHandle);
            }
        }
        else if (key is Key.Up)
        {
            if (isPlaying && !isMuted)
            {
                currentVolume += 0.1f;
                currentVolume = Math.Clamp(currentVolume, 0, 1);
                al.SetSourceProperty(soundSource, SourceFloat.Gain, currentVolume);
                ShowSnackbarText(volumeTextHandles[(int)(currentVolume * 10)]);
            }
        }
        else if (key is Key.Down)
        {
            if (isPlaying && !isMuted)
            {
                currentVolume -= 0.1f;
                currentVolume = Math.Clamp(currentVolume, 0, 1);
                al.SetSourceProperty(soundSource, SourceFloat.Gain, currentVolume);
                ShowSnackbarText(volumeTextHandles[(int)(currentVolume * 10)]);
            }
        }
        else if (key is Key.Right)
        {
            if (isPlaying)
            {
                playSpeed *= 2;
                playSpeed = Math.Clamp(playSpeed, 0.125, 8);
                gameboy?.SetPlaySpeed(playSpeed);
                ShowSnackbarText(playSpeed switch
                {
                    0.125 => speedTextHandles[0],
                    0.25 => speedTextHandles[1],
                    0.5 => speedTextHandles[2],
                    1 => speedTextHandles[3],
                    2 => speedTextHandles[4],
                    4 => speedTextHandles[5],
                    8 => speedTextHandles[6],
                    _ => throw new Exception("Invalid play speed")
                });
            }
        }
        else if (key is Key.Left)
        {
            if (isPlaying)
            {
                playSpeed *= 0.5f;
                playSpeed = Math.Clamp(playSpeed, 0.125, 8);
                gameboy?.SetPlaySpeed(playSpeed);
                ShowSnackbarText(playSpeed switch
                {
                    0.125 => speedTextHandles[0],
                    0.25 => speedTextHandles[1],
                    0.5 => speedTextHandles[2],
                    1 => speedTextHandles[3],
                    2 => speedTextHandles[4],
                    4 => speedTextHandles[5],
                    8 => speedTextHandles[6],
                    _ => throw new Exception("Invalid play speed")
                });
            }
        }

        if (wasOpen is true)
        {
            picker?.OnKeyPressed(key);
        }
    }

    private double playSpeed = 1f;

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
        playSpeed = 1f;

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
        oldestBufferInQueue = 0;
    }

    private int nextBufferToQueue = 0;
    private int oldestBufferInQueue = 0;
    private short[] lastSampleBuffer = new short[soundBufferSizeBytes / sizeof(short)];

    private void UpdateSound()
    {
        al.GetSourceProperty(soundSource, GetSourceInteger.BuffersProcessed, out var buffersProcessed);
        if (buffersProcessed > 1)
        {
            Console.WriteLine($"Warning: {buffersProcessed} buffers were processed since last frame. Expected at most {soundBuffers.Length - 1}.");
        }
        var buffersToQueue = buffersProcessed;
        al.GetSourceProperty(soundSource, GetSourceInteger.BuffersQueued, out var buffersQueued);
        if (buffersQueued < soundBuffers.Length)
        {
            Console.WriteLine($"Warning: {buffersQueued} buffers are queued. Expected at least {soundBuffers.Length}.");
            buffersToQueue = soundBuffers.Length - buffersQueued;
        }
        while (buffersToQueue > 0 && state.SampleBufferQueue.Count > buffersToQueue + 1)
        {
            Console.WriteLine("Warning: Producing more buffers than are being played: Dropping buffer.");
            state.SampleBufferQueue.Dequeue();
        }
        for (var i = 0; i < buffersToQueue; i++)
        {
            fixed (uint* ptr = &soundBuffers[oldestBufferInQueue])
            {
                al.SourceUnqueueBuffers(soundSource, 1, ptr);
                oldestBufferInQueue += 1;
                oldestBufferInQueue %= soundBuffers.Length;
            }
            short[] sampleBuffer;
            if (state.SampleBufferQueue.Count is 0)
            {
                sampleBuffer = lastSampleBuffer;
            }
            else
            {
                sampleBuffer = state.SampleBufferQueue.Dequeue();
            }

            fixed (short* data = sampleBuffer)
            {
                al.BufferData(soundBuffers[nextBufferToQueue], soundBufferFormat, data, soundBufferSizeBytes, soundBufferFrequency);
            }

            fixed (uint* ptr = &soundBuffers[nextBufferToQueue])
            {
                al.SourceQueueBuffers(soundSource, 1, ptr);
                nextBufferToQueue += 1;
                nextBufferToQueue %= soundBuffers.Length;
            }

            lastSampleBuffer = sampleBuffer;
        }

        al.GetSourceProperty(soundSource, GetSourceInteger.SourceState, out var sourceState);
        if ((SourceState)sourceState is not SourceState.Playing)
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
