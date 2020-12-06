using System;
using System.Threading.Tasks;
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
        private Gameboi gameboi;

        public MainWindow()
        {
            InitializeComponent();
            gameboi = new Gameboi();
            this.DataContext = gameboi.GetScreen();

            CompositionTarget.Rendering += Startup;

            Deactivated += (_, __) => gameboi.Pause();
            Activated += (_, __) => gameboi.Play();

            KeyDown += CheckKeyPressed;
        }

        private void CheckKeyPressed(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Escape)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Gb roms (*.gb, *.gbc)|*.gb;*.gbc|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == true) // returns bool? => == true
                {
                    gameboi.LoadGame(openFileDialog.FileName);
                }
            }
            if (args.Key == Key.Space)
                gameboi.PausePlayToggle();
        }

        private void Startup(object _, EventArgs __)
        {
            gameboi.TurnOn();
            gameboi.Play();
            CompositionTarget.Rendering -= Startup;
            CompositionTarget.Rendering += FrameUpdate;
        }

        private void FrameUpdate(object _, EventArgs __)
        {
            gameboi.CheckController();
            gameboi.Render();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            gameboi.Pause();
            gameboi.TurnOff();
        }
    }
}
