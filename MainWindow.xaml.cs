using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace GB_Emulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GameBoy g;
        private ulong i = 10;
        private static double speed = 60;
        private static double durationInS = 1 / speed;
        private static double durationInMs = (1 / speed) * 1000;

        private double durationSinceLastFrame;
        private Stopwatch stopwatch;
        public MainWindow()
        {
            InitializeComponent();
            g = new GameBoy();
            stopwatch = new Stopwatch();
            this.DataContext = g.lcd;

            CompositionTarget.Rendering += CountDown;
        }

        private void CountDown(object _, EventArgs __)
        {
            if (i-- == 0)
            {
                CompositionTarget.Rendering -= CountDown;
                CompositionTarget.Rendering += Start;
                stopwatch.Start();
            }
        }
        private void Start(object _, EventArgs __)
        {

            stopwatch.Stop();
            durationSinceLastFrame += stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();
            stopwatch.Start();
            if (durationSinceLastFrame >= durationInMs)
            {
                g.Play();
                durationSinceLastFrame = 0;
            }
        }

    }
}
