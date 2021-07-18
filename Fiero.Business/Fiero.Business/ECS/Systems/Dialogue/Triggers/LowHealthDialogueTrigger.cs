﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Fiero.Business
{
    public class LowHealthDialogueTrigger<TDialogue> : PlayerInSightDialogueTrigger<TDialogue>
        where TDialogue : struct, Enum
    {
        public float PercentageThreshold { get; set; } = 0.5f;

        public LowHealthDialogueTrigger(GameSystems sys, TDialogue node, bool repeatable)
            : base(sys, node, repeatable)
        {

        }

        public override bool TryTrigger(FloorId floorId, Drawable speaker, out IEnumerable<Drawable> listeners)
        {
            listeners = default;
            if(speaker is Actor a) {
                var healthPercentage = (a.ActorProperties.Health / (float)a.ActorProperties.MaximumHealth);
                if (healthPercentage <= PercentageThreshold && base.TryTrigger(floorId, speaker, out listeners)) {
                    return listeners.Any();
                }
            }
            return false;
        }
    }
}
