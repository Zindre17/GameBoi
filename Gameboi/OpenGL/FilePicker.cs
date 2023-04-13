using System;
using System.Collections.Generic;
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

    private readonly UiLayer ui;
    private readonly List<int> itemHandles = new();
    private readonly List<int> selectorHandles = new();

    private readonly int backgroundHandle;
    private readonly List<(DirectoryItemType, string)> dirItems = new();

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

    private Action<string> onFileChosen = null!;
    private Action? onDialogCancelled;

    public void SelectFile(Action<string> onFileChosen, Action? onDialogCancelled = null)
    {
        this.onFileChosen = onFileChosen;
        this.onDialogCancelled = onDialogCancelled;

        isSelecting = true;

        ui.ShowText(backgroundHandle);
        ui.ShowText(itemHandles);
        UpdateSelectionIndex(0);
    }

    private void LoadDirectory(string directory)
    {
        ui.RemoveText(itemHandles);
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
                itemHandles.Add(ui.ShowText("..", row, 1, new(Rgb.white)));
            }
            else
            {
                var color = type is DirectoryItemType.File ? new Rgba(40, 200, 40, 0xff) : new Rgba(Rgb.white);
                itemHandles.Add(ui.ShowText(path[(directory.Length + 1)..], row, 1, color));
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

                    onDialogCancelled?.Invoke();
                    break;
                case Key.Up:
                    UpdateSelectionIndex(Math.Max(0, currentIndex - 1));
                    break;
                case Key.Down:
                    UpdateSelectionIndex(Math.Min(selectorHandles.Count - 1, Math.Min(itemHandles.Count - 1, currentIndex + 1)));
                    break;
                case Key.Enter:
                    var (type, item) = dirItems[currentIndex];
                    if (type is DirectoryItemType.File)
                    {
                        isSelecting = false;
                        ui.HideText(selectorHandles);
                        ui.HideText(itemHandles);
                        ui.HideText(backgroundHandle);

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
