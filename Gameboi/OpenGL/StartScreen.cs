using System.Collections.Generic;
using Gameboi.Graphics;
using Silk.NET.OpenGL;

namespace Gameboi.OpenGL;

public class StartScreen
{

    private readonly UiLayer screen;

    public StartScreen(GL gl)
    {
        screen = new(gl);
        Init();
    }

    private readonly List<int> handles = new();

    public void Render()
    {
        screen.Render();
    }

    public void Show()
    {
        screen.ShowText(handles);
    }

    public void Hide()
    {
        screen.HideText(handles);
    }

    private void Init()
    {
        handles.Add(screen.FillScreen(backgroundColor));
        AddLine("Welcome!");
        currentRow += 2;
        AddLine("Load ROM: ESC");
    }


    private int currentRow = 1;
    private Rgba textColor = new Rgba(Rgb.white);
    private Rgba backgroundColor = new Rgba(Rgb.darkGray);

    private void AddLine(string message)
    {
        handles.Add(screen.CreateText(message, currentRow, 2, textColor));
        currentRow += 2;
    }
}