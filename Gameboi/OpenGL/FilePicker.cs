using System;
using System.Collections.Generic;
using System.IO;
using Gameboi.Graphics;
using Silk.NET.Input;
using Silk.NET.OpenGL;

namespace Gameboi.OpenGL;

public class FilePicker
{
    private readonly UiLayer ui;
    private readonly List<int> itemHandles = new();
    private readonly List<int> selectorHandles = new();

    private readonly int backgroundHandle;
    private string currentDirectory = null!;
    private string[] subDirs = null!;
    private int currentIndex;
    private bool isSelecting = false;
    public bool IsOpen => isSelecting;

    public FilePicker(GL gl)
    {
        ui = new(gl);
        backgroundHandle = ui.FillScreen(new(100, 100, 255, 0xff));
        for (var i = 0; i < 18; i++)
        {
            selectorHandles.Add(ui.CreateText("*                   ", i, 0, new(0, 0, 0, 128), new(0, 0, 0, 50)));
        }
        LoadDirectory(Directory.GetCurrentDirectory());
    }

    public void SelectFile()
    {
        currentIndex = 0;
        isSelecting = true;

        ui.ShowText(backgroundHandle);
        var dir = Directory.GetCurrentDirectory();
        if (currentDirectory != dir)
        {
            LoadDirectory(dir);
        }
        else
        {
            ui.ShowText(itemHandles);
        }
        ui.ShowText(selectorHandles[currentIndex]);
    }

    private void LoadDirectory(string directory)
    {
        ui.RemoveText(itemHandles);
        itemHandles.Clear();

        UpdateSelectionIndex(0);
        currentDirectory = directory;

        var parent = Directory.GetParent(directory)?.FullName;
        subDirs = Directory.GetDirectories(directory);
        if (parent is not null)
        {
            var newSubDirs = new string[subDirs.Length + 1];
            newSubDirs[0] = parent;
            Array.Copy(subDirs, 0, newSubDirs, 1, subDirs.Length);
            subDirs = newSubDirs;
        }
        var row = 0;
        foreach (var subDir in subDirs)
        {
            if (subDir.Length < directory.Length)
            {
                itemHandles.Add(ui.ShowText("..", row, 1, new(Rgb.white)));
            }
            else
            {
                itemHandles.Add(ui.ShowText(subDir[(directory.Length + 1)..], row, 1, new(Rgb.white)));
            }
            row++;
        }
    }

    public void OnKeyPressed(Key key)
    {
        if (isSelecting)
        {
            switch (key)
            {
                case Key.Escape:
                    isSelecting = false;
                    ui.HideText(selectorHandles);
                    ui.HideText(itemHandles);
                    ui.HideText(backgroundHandle);
                    break;
                case Key.Up:
                    UpdateSelectionIndex(Math.Max(0, currentIndex - 1));
                    break;
                case Key.Down:
                    UpdateSelectionIndex(Math.Min(selectorHandles.Count - 1, Math.Min(itemHandles.Count - 1, currentIndex + 1)));
                    break;
                case Key.Enter:
                    LoadDirectory(subDirs[currentIndex]);
                    break;
            }
        }
    }

    private void UpdateSelectionIndex(int newIndex)
    {
        ui.HideText(selectorHandles[currentIndex]);
        currentIndex = newIndex;
        ui.ShowText(selectorHandles[currentIndex]);
    }

    public void Render()
    {
        if (!isSelecting)
        {
            return;
        }
        ui.Render();
    }
}
