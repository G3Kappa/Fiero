﻿using Unconcern.Common;

namespace Fiero.Business
{
    public abstract class IncreaseMaxStatEffect : TypedEffect<Actor>
    {
        public override string DisplayName => "$Effect.IncreaseMaxStatEffect.Name$";
        public override string DisplayDescription => "$Effect.IncreaseMaxStatEffect.Desc$";

        public readonly int Amount;
        public Func<Actor, Stat> GetStat;

        private readonly List<Action> _onEnded = new();

        public IncreaseMaxStatEffect(Entity source, int amount, Func<Actor, Stat> getStat)
            : base(source)
        {
            Amount = amount;
            GetStat = getStat;
        }

        protected override void OnEnded(MetaSystem systems, Entity owner)
        {
            base.OnEnded(systems, owner);
            foreach (var onEnded in _onEnded)
                onEnded();
            _onEnded.Clear();
        }

        protected override void TypedOnStarted(MetaSystem systems, Actor target)
        {

            if (GetStat(target) is not { } stat)
                return;
            stat.Max += Amount;
            if (stat.Max < 0)
                stat.Max = 0;
            if (stat.V > stat.Max)
                stat.V = stat.Max;
            _onEnded.Add(() => stat.Max -= Amount);
        }

        protected override IEnumerable<Subscription> RouteEvents(MetaSystem systems, Entity owner)
        {
            yield break;
        }
    }
}
