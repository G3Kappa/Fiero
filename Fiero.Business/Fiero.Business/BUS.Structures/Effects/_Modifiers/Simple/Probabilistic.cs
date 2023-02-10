﻿using Fiero.Core;
using System.Collections.Generic;
using Unconcern.Common;

namespace Fiero.Business
{
    public class Probabilistic : ModifierEffect
    {
        public readonly float Probability;

        public Probabilistic(EffectDef source, float chance) : base(source)
        {
            Probability = chance;
        }

        public override string DisplayName => $"$Effect.{Source.Name}$";
        public override string DisplayDescription => $"$Effect.Chance$ ({(int)(Probability * 100)}%)";
        public override EffectName Name => Source.Name;

        protected override void OnStarted(GameSystems systems, Entity owner)
        {
            if (Rng.Random.NextDouble() < Probability)
            {
                Source.Resolve(null).Start(systems, owner);
            }
        }

        protected override IEnumerable<Subscription> RouteEvents(GameSystems systems, Entity owner)
        {
            yield break;
        }
    }
}