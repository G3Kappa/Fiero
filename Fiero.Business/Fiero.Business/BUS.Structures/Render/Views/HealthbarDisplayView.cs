﻿using Fiero.Core;
using LightInject;
using SFML.Graphics;
using SFML.System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Fiero.Business
{
    public class HealthbarDisplayView : View
    {
        public Coord Position { get; set; }
        public Actor Following { get; set; }

        protected ProgressBar EnemyBar { get; private set; }
        protected ProgressBar BossBar { get; private set; }

        public HealthbarDisplayView(LayoutBuilder layoutBuilder)
        {
            layoutBuilder.Build(new(), grid => grid
                .Row()
                    .Cell<ProgressBar>(x => {
                        EnemyBar = x;
                        x.Center.V = true;
                        x.Length.V = 1;
                        x.Capped.V = false;
                        x.Origin.V = new Vec(0.5f, 0);
                    })
                .End()
            );
            layoutBuilder.Build(new(), grid => grid
                .Row()
                    .Cell<ProgressBar>(x => {
                        BossBar = x;
                        x.Center.V = true;
                        x.Length.V = 5;
                        x.Origin.V = new Vec(-0.25f, 0);
                    })
                .End()
            );
        }

        public override void Draw(RenderTarget target)
        {
            if (Following is null || Following.Id == 0)
                return;
            if (Following.ActorProperties.Stats.Health == Following.ActorProperties.Stats.MaximumHealth)
                return;
            var bar = (Following.Npc != null)
                ? BossBar : EnemyBar;
            bar.Position.V = Position;
            bar.Progress.V = Following.ActorProperties.Stats.MaximumHealth > 0
                ? Following.ActorProperties.Stats.Health / (float)Following.ActorProperties.Stats.MaximumHealth 
                : 0;
            if (bar.Progress > 1) {
                bar.Foreground.V = new(255, 0, 255);
            }
            else if (bar.Progress >= 0.75) {
                bar.Foreground.V = new(0, 255, 0);
            }
            else if (bar.Progress >= 0.66) {
                bar.Foreground.V = new(200, 255, 0);
            }
            else if (bar.Progress >= 0.50) {
                bar.Foreground.V = new(200, 200, 0);
            }
            else if (bar.Progress >= 0.33) {
                bar.Foreground.V = new(255, 0, 0);
            }
            else if(bar.Progress >= 0.0) {
                bar.Foreground.V = new(128, 0, 0);
            }
            target.Draw(bar);
        }

        public override void OnWindowResized(Coord newSize) => throw new System.NotImplementedException();
    }
}