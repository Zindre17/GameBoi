using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Gameboi.Graphics;
using Silk.NET.Input;
using Silk.NET.OpenGL;

namespace Gameboi.OpenGL;

public class FilePicker
{
    private enum DirectoryItemType
    {
        File,
        Directory,
        Parent
    }

    private readonly UiLayer itemLayer;
    private readonly UiLayer selectorLayer;
    private readonly UiLayer backgroundLayer;

    private readonly List<int> itemHandles = new();
    private readonly List<int> selectorHandles = new();

    private readonly int backgroundHandle;
    private readonly List<(DirectoryItemType, string)> dirItems = new();

    private int currentIndex;
    private int scrollOffset;
    private bool isSelecting = false;
    public bool IsOpen => isSelecting;

    public FilePicker(GL gl, string startDirectory)
    {
        itemLayer = new(gl);
        selectorLayer = new(gl);
        backgroundLayer = new(gl);
        backgroundHandle = backgroundLayer.FillScreen(new(100, 100, 255, 0xff));

        for (var i = 0; i < 18; i++)
        {
            selectorHandles.Add(selectorLayer.CreateText("*                   ", i, 0, new(0, 0, 0, 128), new(0, 0, 0, 50)));
        }
        LoadDirectory(startDirectory);
    }

    private Action<string> onFileChosen = null!;
    private Action? onDialogCancelled;

    public void SelectFile(Action<string> onFileChosen, Action? onDialogCancelled = null)
    {
        this.onFileChosen = onFileChosen;
        this.onDialogCancelled = onDialogCancelled;

        isSelecting = true;

        backgroundLayer.ShowText(backgroundHandle);
        itemLayer.ShowText(itemHandles);
        UpdateSelectionIndex(currentIndex);
    }

    private void LoadDirectory(string directory)
    {
        itemLayer.RemoveText(itemHandles);
        itemHandles.Clear();
        dirItems.Clear();

        UpdateSelectionIndex(0);

        var parent = Directory.GetParent(directory)?.FullName;
        if (parent is not null)
        {
            dirItems.Add((DirectoryItemType.Parent, parent));
        }

        foreach (var subDir in Directory.GetDirectories(directory))
        {
            dirItems.Add((DirectoryItemType.Directory, subDir));
        }
        foreach (var file in Directory.GetFiles(directory))
        {
            if (file.EndsWith(".gb") || file.EndsWith(".gbc"))
            {
                dirItems.Add((DirectoryItemType.File, file));
            }
        }

        var row = 0;
        foreach (var (type, path) in dirItems)
        {
            if (type is DirectoryItemType.Parent)
            {
                itemHandles.Add(itemLayer.ShowText("..", row, 1, new(Rgb.white)));
            }
            else
            {
                var color = type is DirectoryItemType.File ? new Rgba(40, 200, 40, 0xff) : new Rgba(Rgb.white);
                itemHandles.Add(itemLayer.ShowText(path[(directory.Length + 1)..], row, 1, color));
            }
            row++;
        }
    }

    private readonly Stopwatch upPressedTimer = new();
    private bool upPressedPassedInitialThreshold;
    private readonly Stopwatch downPressedTimer = new();
    private bool downPressedPassedInitialThreshold;

    public void OnKeyPressed(Key key)
    {
        if (isSelecting)
        {
            switch (key)
            {
                case Key.Escape:
                    isSelecting = false;
                    selectorLayer.HideText(selectorHandles);
                    itemLayer.HideText(itemHandles);
                    backgroundLayer.HideText(backgroundHandle);

                    onDialogCancelled?.Invoke();
                    break;
                case Key.Up:
                    upPressedTimer.Restart();
                    upPressedPassedInitialThreshold = false;
                    UpdateSelectionIndex(currentIndex - 1);
                    break;
                case Key.Down:
                    downPressedTimer.Restart();
                    downPressedPassedInitialThreshold = false;
                    UpdateSelectionIndex(currentIndex + 1);
                    break;
                case Key.Enter:
                    var (type, item) = dirItems[currentIndex];
                    if (type is DirectoryItemType.File)
                    {
                        isSelecting = false;
                        selectorLayer.HideText(selectorHandles);
                        itemLayer.HideText(itemHandles);
                        backgroundLayer.HideText(backgroundHandle);

                        onFileChosen(item);
                    }
                    else
                    {
                        LoadDirectory(item);
                    }
                    break;
            }
        }
    }

    public void OnKeyReleased(Key key)
    {
        switch (key)
        {
            case Key.Up:
                upPressedTimer.Stop();
                break;
            case Key.Down:
                downPressedTimer.Stop();
                break;
        }
    }

    public void Update()
    {
        const long initialThreshold = 300;
        const long threshold = 75;
        if (upPressedTimer.IsRunning)
        {
            if (upPressedPassedInitialThreshold)
            {
                if (upPressedTimer.ElapsedMilliseconds >= threshold)
                {
                    upPressedTimer.Restart();
                    UpdateSelectionIndex(currentIndex - 1);
                }
            }
            else
            {
                if (upPressedTimer.ElapsedMilliseconds >= initialThreshold)
                {
                    upPressedPassedInitialThreshold = true;
                    UpdateSelectionIndex(currentIndex - 1);
                }
            }
        }
        else if (downPressedTimer.IsRunning)
        {
            if (downPressedPassedInitialThreshold)
            {
                if (downPressedTimer.ElapsedMilliseconds >= threshold)
                {
                    downPressedTimer.Restart();
                    UpdateSelectionIndex(currentIndex + 1);
                }
            }
            else
            {
                if (downPressedTimer.ElapsedMilliseconds >= initialThreshold)
                {
                    downPressedPassedInitialThreshold = true;
                    UpdateSelectionIndex(currentIndex + 1);
                }
            }
        }
    }

    private void UpdateSelectionIndex(int newIndex)
    {
        selectorLayer.HideText(selectorHandles[currentIndex - scrollOffset]);

        if (newIndex < 0)
        {
            currentIndex = dirItems.Count - 1;
            if (dirItems.Count > selectorHandles.Count)
            {
                scrollOffset = currentIndex - (selectorHandles.Count - 1);
            }
        }
        else if (newIndex >= dirItems.Count)
        {
            currentIndex = 0;
            scrollOffset = 0;
        }
        else
        {
            currentIndex = newIndex;
            if (currentIndex >= selectorHandles.Count - 1)
            {
                scrollOffset = currentIndex - (selectorHandles.Count - 1);
            }
            if (currentIndex < scrollOffset)
            {
                scrollOffset = 0;
            }
        }

        itemLayer.Translate(0, scrollOffset);
        selectorLayer.ShowText(selectorHandles[currentIndex - scrollOffset]);
    }

    public void Render()
    {
        if (!isSelecting)
        {
            return;
        }
        backgroundLayer.Render();
        itemLayer.Render();
        selectorLayer.Render();
    }
}
