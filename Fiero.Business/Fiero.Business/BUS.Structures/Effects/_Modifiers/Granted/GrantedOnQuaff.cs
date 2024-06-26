﻿namespace Fiero.Business
{
    public class GrantedOnQuaff : QuaffEffect
    {
        public override string DisplayName => $"$Effect.{Source.Name}$";
        public override string DisplayDescription => "$Effect.GrantedOnQuaff$";
        public override EffectName Name => Source.Name;

        public GrantedOnQuaff(EffectDef source) : base(source)
        {
        }

        protected override void OnApplied(MetaSystem systems, Entity owner, Actor target)
        {
            Source.Resolve(owner).Start(systems, target, owner);
        }
    }
}
