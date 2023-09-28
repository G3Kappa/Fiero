﻿namespace Fiero.Business
{
    /// <summary>
    /// Typed effects can be applied to:
    /// - T:
    ///     - The effect is applied to the T when the effect starts, and is removed when the effect ends.
    /// </summary>
    public abstract class TypedEffect<T> : Effect
        where T : Entity
    {
        public readonly Entity Source;

        public TypedEffect(Entity source)
        {
            Source = source;
        }

        protected virtual void ApplyOnStarted(GameSystems systems, T target) { }
        protected virtual void ApplyOnEnded(GameSystems systems, T target) { }

        protected override void OnStarted(GameSystems systems, Entity owner)
        {
            base.OnStarted(systems, owner);
            if (owner.TryCast<T>(out var target))
            {
                ApplyOnStarted(systems, target);
            }
        }

        protected override void OnEnded(GameSystems systems, Entity owner)
        {
            base.OnEnded(systems, owner);
            if (owner.TryCast<T>(out var target))
            {
                ApplyOnEnded(systems, target);
            }
        }
    }
}
