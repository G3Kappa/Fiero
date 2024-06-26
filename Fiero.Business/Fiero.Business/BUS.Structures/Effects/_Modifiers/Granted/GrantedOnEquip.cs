﻿namespace Fiero.Business
{
    public class GrantedOnEquip : EquipmentEffect
    {
        protected Effect Instance { get; private set; }

        public GrantedOnEquip(EffectDef source) : base(source)
        {
        }

        public override EffectName Name => Source.Name;
        public override string DisplayName => $"$Effect.{Source.Name}$";
        public override string DisplayDescription => "$Effect.GrantedOnEquip$";
        protected override void OnApplied(MetaSystem systems, Actor target)
        {
            (Instance = Source.Resolve(target)).Start(systems, target, null);
        }
        protected override void OnRemoved(MetaSystem systems, Actor target)
        {
            if (Instance != null)
                Instance.End(systems, target);
            Instance = null;
        }
    }
}
