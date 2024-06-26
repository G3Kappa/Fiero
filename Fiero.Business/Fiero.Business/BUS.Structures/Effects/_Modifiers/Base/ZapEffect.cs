﻿using Unconcern.Common;

namespace Fiero.Business
{
    /// <summary>
    /// Use effects can be applied to:
    /// - Wands:
    ///     - The effect is applied to the actor that is zapped by the wand.
    /// </summary>
    public abstract class ZapEffect : ModifierEffect
    {
        protected ZapEffect(EffectDef source) : base(source)
        {
        }

        protected abstract void OnApplied(MetaSystem systems, Entity owner, Actor source, Actor target);
        protected abstract void OnApplied(MetaSystem systems, Entity owner, Actor source, Coord location);

        protected override IEnumerable<Subscription> RouteEvents(MetaSystem systems, Entity owner)
        {
            if (owner.TryCast<Wand>(out var wand))
            {
                yield return systems.Get<ActionSystem>().WandZapped.SubscribeHandler(e =>
                {
                    if (e.Wand == owner)
                    {
                        if (e.Victim != null)
                        {
                            OnApplied(systems, owner, e.Actor, e.Victim);
                        }
                        else
                        {
                            OnApplied(systems, owner, e.Actor, e.Position);
                        }
                    }
                });
            }
        }
    }
}
