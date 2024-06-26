﻿using Unconcern.Common;

namespace Fiero.Business
{
    /// <summary>
    /// On-hit-dealt effects can be applied to:
    /// - Actors, in which case the effect is applied when an enemy is hit by the actor
    /// - Weapons, in which case the effect is applied when an enemy is hit by the weapon
    /// </summary>
    public abstract class HitDealtEffect : ModifierEffect
    {
        protected HitDealtEffect(EffectDef source) : base(source)
        {
        }

        protected abstract void OnApplied(MetaSystem systems, Entity owner, Actor source, Actor target, int damage);

        protected override IEnumerable<Subscription> RouteEvents(MetaSystem systems, Entity owner)
        {
            var action = systems.Get<ActionSystem>();
            if (owner.TryCast<Actor>(out var actor))
            {
                yield return action.ActorDamaged.SubscribeHandler(e =>
                {
                    if (e.Source == actor)
                    {
                        OnApplied(systems, owner, actor, e.Victim, e.Damage);
                    }
                });
                // Don't bind to the ActorDespawned event, because it invalidates the owner
                yield return action.ActorDied.SubscribeHandler(e =>
                {
                    if (e.Actor == actor)
                    {
                        End(systems, owner);
                    }
                });
            }
            else if (owner.TryCast<Weapon>(out var weapon))
            {
                yield return action.ActorDamaged.SubscribeHandler(e =>
                {
                    if (e.Weapons.Contains(weapon) && e.Source.TryCast<Actor>(out var source))
                    {
                        OnApplied(systems, owner, source, e.Victim, e.Damage);
                    }
                });
                // TODO: End the effect when the item is destroyed, if I end up adding a way to destroy items
            }
        }
    }
}
