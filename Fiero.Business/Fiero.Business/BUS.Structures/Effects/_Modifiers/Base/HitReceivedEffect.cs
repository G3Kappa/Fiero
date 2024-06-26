﻿using Unconcern.Common;

namespace Fiero.Business
{
    /// <summary>
    /// On-hit-received effects can be applied to:
    /// - Actors, in which case the effect is applied when the actor is hit 
    /// - Armor, in which case the effect is applied when the wearer is hit 
    /// </summary>
    public abstract class HitReceivedEffect : ModifierEffect
    {
        protected HitReceivedEffect(EffectDef source) : base(source)
        {
        }

        protected abstract void OnApplied(MetaSystem systems, Entity owner, Entity source, Actor target, int damage);

        protected override IEnumerable<Subscription> RouteEvents(MetaSystem systems, Entity owner)
        {
            if (owner.TryCast<Actor>(out var actor))
            {
                yield return systems.Get<ActionSystem>().ActorDamaged.SubscribeHandler(e =>
                {
                    if (e.Victim == actor)
                    {
                        OnApplied(systems, owner, e.Source, e.Victim, e.Damage);
                    }
                });
                // Don't bind to the ActorDespawned event, because it invalidates the owner
                yield return systems.Get<ActionSystem>().ActorDied.SubscribeHandler(e =>
                {
                    if (e.Actor == actor)
                    {
                        End(systems, owner);
                    }
                });
            }
            if (owner.TryCast<Armor>(out var armor))
            {
                yield return systems.Get<ActionSystem>().ActorDamaged.SubscribeHandler(e =>
                {
                    if ((e.Victim.ActorEquipment?.IsEquipped(armor) ?? false))
                    {
                        OnApplied(systems, owner, e.Source, e.Victim, e.Damage);
                    }
                });
                // TODO: End the effect when the item is destroyed, if I end up adding a way to destroy items
            }
        }
    }
}
