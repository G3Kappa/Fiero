﻿using Unconcern.Common;

namespace Fiero.Business
{

    // Unable to act, but you slowly restore your HP. Woken up after several turns; increasing chance of waking up when taking heavy damage.
    public class SleepEffect : TypedEffect<Actor>
    {
        public SleepEffect(Entity source) : base(source) { }
        public override string DisplayName => "$Effect.Sleep.Name$";
        public override string DisplayDescription => "$Effect.Sleep.Desc$";
        public override EffectName Name => EffectName.Sleep;

        protected override void TypedOnStarted(MetaSystem systems, Actor target)
        {
            target.TryRoot();
            Ended += e => target.TryFree();
        }

        protected override IEnumerable<Subscription> RouteEvents(MetaSystem systems, Entity owner)
        {
            if (!owner.TryCast<Actor>(out var actor))
            {
                yield break;
            }

            yield return systems.Get<ActionSystem>().ActorIntentSelected.SubscribeResponse(e =>
            {
                if (e.Actor == owner)
                {
                    systems.Get<ActionSystem>().ActorHealed.Handle(new(e.Actor, e.Actor, e.Actor, Rng.Random.Between(0, 2)));
                    return new(new WaitAction());
                }
                return new();
            });
            yield return systems.Get<ActionSystem>().ActorAttacked.SubscribeHandler(e =>
            {
                if (e.Victims.Contains(owner) && Rng.Random.NChancesIn(e.Damage, 100))
                {
                    End(systems, owner);
                }
            });
        }
    }
}
