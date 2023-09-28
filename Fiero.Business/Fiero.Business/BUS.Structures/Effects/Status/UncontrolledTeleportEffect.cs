﻿using Unconcern.Common;

namespace Fiero.Business
{
    public class UncontrolledTeleportEffect : TypedEffect<Actor>
    {
        public UncontrolledTeleportEffect(Entity source) : base(source) { }
        public override string DisplayName => "$Effect.UncontrolledTeleport.Name$";
        public override string DisplayDescription => "$Effect.UncontrolledTeleport.Desc$";
        public override EffectName Name => EffectName.UncontrolledTeleport;

        protected override void ApplyOnStarted(GameSystems systems, Actor target)
        {
            var randomPos = systems.Dungeon.GetFloor(target.FloorId())
                .Cells.Shuffle(Rng.Random)
                .Where(x => x.Key.Dist(target.Position()) < 10)
                .First(x => x.Value.IsWalkable(target))
                .Key;
            systems.Action.ActorTeleporting.HandleOrThrow(new(target, target.Position(), randomPos));
            End(systems, target);
        }

        protected override IEnumerable<Subscription> RouteEvents(GameSystems systems, Entity owner)
        {
            yield break;
        }
    }
}
