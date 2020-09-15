﻿using System;
using System.Diagnostics;
using System.Threading;

namespace MinecraftServerSharp
{
    public class Ticker
    {
        public delegate void TickEvent(Ticker ticker);

        public event TickEvent? Tick;

        public TimeSpan TargetTime { get; }

        public TimeSpan ElapsedTime { get; private set; }
        public TimeSpan FreeTime => TargetTime - ElapsedTime;

        public Ticker(TimeSpan targetTickTime)
        {
            TargetTime = targetTickTime;
        }

        public void Run()
        {
            long lastTicks = Stopwatch.GetTimestamp();
            long targetSleepTicks = 0;

            while (true)
            {
                long currentTicks = Stopwatch.GetTimestamp();
                long sleepTicks = currentTicks - lastTicks;
                Tick?.Invoke(this);
                lastTicks = Stopwatch.GetTimestamp();
                ElapsedTime = TimeSpan.FromTicks(lastTicks - currentTicks);

                // Try to sleep for as long as possible without overshooting the target time.
                var preciseSleepTime = TargetTime - ElapsedTime;
                long sleepOverheadTicks = Math.Max(0, sleepTicks - targetSleepTicks);
                targetSleepTicks = preciseSleepTime.Ticks - sleepOverheadTicks;

                int sleepMillis = (int)(targetSleepTicks / TimeSpan.TicksPerMillisecond);
                if (sleepMillis > 0)
                    Thread.Sleep(sleepMillis);
            }
        }
    }
}
