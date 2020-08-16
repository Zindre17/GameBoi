using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

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
        }

        private void Startup(object _, EventArgs __)
        {
            gameboi.TurnOn();
            new Task(() => gameboi.Play()).Start();
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
            gameboi.TurnOff();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            gameboi.Pause();
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            gameboi.Play();
        }

    }
}
