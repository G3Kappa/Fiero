﻿using System;
using System.Diagnostics;
using System.Threading;

namespace Fiero.Core
{

    public class GameLoop
    {
        public float TimeStep { get; set; }
        public event Action<float, float> Tick;
        public event Action<float, float> Update;
        public event Action<float, float> Render;
        public float T { get; private set; }

        public GameLoop()
        {
            TimeStep = 1f / 10000f;
        }

        public virtual void Wait(TimeSpan time, float timestep)
        {
            var innerLoop = new GameLoop() {  TimeStep = timestep }; 
            innerLoop.Run(time);

        }

        public virtual void WaitAndDraw(TimeSpan time, float timestep)
        {
            var innerLoop = new GameLoop() { TimeStep = timestep };
            innerLoop.Render += (t, ts) => Render?.Invoke(T, TimeStep);
            innerLoop.Run(time);
        }

        public virtual void Run(TimeSpan duration = default, CancellationToken ct = default)
        {
            var time = new Stopwatch();
            time.Start();
            T = 0f;
            var accumulator = 0f;
            var currentTime = (float)time.Elapsed.TotalSeconds;
            while ((duration.TotalSeconds == 0 || time.Elapsed < duration) && !ct.IsCancellationRequested) {
                var newTime = (float)time.Elapsed.TotalSeconds;
                var frameTime = newTime - currentTime;
                if (frameTime > 0.25f) {
                    frameTime = 0.25f;
                }
                currentTime = newTime;
                Tick?.Invoke(T, TimeStep);
                accumulator += frameTime;
                while (accumulator >= TimeStep) {
                    Update?.Invoke(T, TimeStep);
                    T += TimeStep;
                    accumulator -= TimeStep;
                }
                Render?.Invoke(T, TimeStep);
            }
        }
    }
}
