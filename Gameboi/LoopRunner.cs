using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GB_Emulator.Gameboi
{
    public class LoopRunner
    {

        private bool isRunning = false;
        private Task runner;
        private readonly Action<long> task;
        private readonly Stopwatch stopwatch = new();

        private readonly float millisecondsPerLoop;

        public LoopRunner(Action<long> task, float millisecondsPerLoop)
        {
            this.task = task;
            this.millisecondsPerLoop = millisecondsPerLoop;
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
            var lastMilliseconds = 0L;
            var loopCount = 1L;
            stopwatch.Start();
            while (isRunning)
            {
                var currentMilliseconds = stopwatch.ElapsedMilliseconds;
                task(currentMilliseconds);

                var millisecondsAfterCurrentLoop = (long)(millisecondsPerLoop * loopCount);
                var millisecondsToSleep = millisecondsAfterCurrentLoop - currentMilliseconds;

                if (millisecondsToSleep > 0)
                    Thread.Sleep((int)millisecondsToSleep);

                lastMilliseconds = currentMilliseconds;
                loopCount++;
            }
            stopwatch.Reset();
        }

    }
}