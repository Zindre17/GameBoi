using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GB_Emulator.Gameboi
{
    public interface ILoop
    {
        float MillisecondsPerLoop { get; set; }
        Action<long> Loop { get; }
    }

    public class LoopRunner
    {

        private bool isRunning = false;
        private Task runner;
        private readonly Stopwatch stopwatch = new();

        private readonly ILoop loop;

        public LoopRunner(ILoop loop)
        {
            this.loop = loop;
        }

        public void Start()
        {
            if (isRunning) return;

            isRunning = true;
            if (runner == null)
            {
                runner = new Task(Loop);
                runner.Start();
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            while (runner.Status != TaskStatus.RanToCompletion)
                Thread.Sleep(1);
            runner.Dispose();
            runner = null;
        }

        public void Toggle()
        {
            if (isRunning)
                Stop();
            else
                Start();
        }

        private void Loop()
        {
            stopwatch.Start();
            while (isRunning)
            {
                var currentMilliseconds = stopwatch.ElapsedMilliseconds;
                var millisecondsAfterCurrentLoop = currentMilliseconds + loop.MillisecondsPerLoop;

                loop.Loop((long)millisecondsAfterCurrentLoop);
                var actualMillisecondsAfterLoop = stopwatch.ElapsedMilliseconds;

                var millisecondsToSleep = millisecondsAfterCurrentLoop - actualMillisecondsAfterLoop;

                if (millisecondsToSleep > 0 && millisecondsToSleep <= loop.MillisecondsPerLoop)
                    Thread.Sleep((int)millisecondsToSleep);
            }
            stopwatch.Reset();
        }

    }
}