using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace GB_Emulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Gameboi.Gameboi gameboi;

        public MainWindow()
        {
            InitializeComponent();

            CompositionTarget.Rendering += FrameUpdate;

            Deactivated += (_, __) => gameboi?.Pause();
            Activated += (_, __) => gameboi?.Play();

            KeyDown += CheckKeyPressed;
        }

        private void CheckKeyPressed(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Escape)
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "Gb roms (*.gb, *.gbc)|*.gb;*.gbc|All files (*.*)|*.*"
                };
                if (openFileDialog.ShowDialog() ?? false)
                {
                    if (gameboi is not null)
                    {
                        gameboi.Pause();
                        gameboi.TurnOff();
                    }
                    gameboi = new();
                    Title = gameboi.LoadGame(openFileDialog.FileName) ?? "Gameboi";
                    gameboi.TurnOn();
                    gameboi.Play();
                    DataContext = gameboi.GetScreen();
                }
            }

            if (args.Key == Key.D1)
                gameboi?.ToggleBackground();
            if (args.Key == Key.D2)
                gameboi?.ToggleWindow();
            if (args.Key == Key.D3)
                gameboi?.ToggleSprites();

            if (args.Key == Key.Right)
                gameboi?.ChangeSpeed(true);
            if (args.Key == Key.Left)
                gameboi?.ChangeSpeed(false);

            if (args.Key == Key.Space)
                gameboi?.PausePlayToggle();
        }

        private void FrameUpdate(object _, EventArgs __)
        {
            if (gameboi is not null)
            {
                gameboi.CheckController();
                gameboi.Render();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (gameboi is not null)
            {
                gameboi.Pause();
                gameboi.TurnOff();
            }
        }
    }
}
