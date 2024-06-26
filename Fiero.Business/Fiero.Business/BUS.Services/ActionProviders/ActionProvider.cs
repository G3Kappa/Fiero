﻿using Ergo.Lang;

namespace Fiero.Business
{
    public abstract class ActionProvider
    {
        protected readonly List<IAISensor> Sensors;

        protected readonly AiSensor<Stat> MyHealth;
        protected readonly AiSensor<Weapon> MyWeapons;
        protected readonly AiSensor<Consumable> MyConsumables;
        protected readonly AiSensor<Consumable> MyHelpfulConsumables;
        protected readonly AiSensor<Consumable> MyHarmfulConsumables;
        protected readonly AiSensor<Consumable> MyUnidentifiedConsumables;
        protected readonly AiSensor<Consumable> MyPanicButtons;

        protected readonly AiSensor<Item> NearbyItems;
        protected readonly AiSensor<Actor> NearbyEnemies;
        protected readonly AiSensor<Actor> NearbyAllies;

        protected bool Panic => MyHealth.RaisingAlert && TurnsSinceSightingHostile < 10;
        protected int TurnsSinceSightingHostile { get; private set; }
        protected int TurnsSinceSightingFriendly { get; private set; }

        public readonly MetaSystem Systems;

        /// <summary>
        /// This should be true whenever the ActionProvider should be monitored in the UI, 
        /// otherwise it will be too fast for the renderer to catch up. One example is when
        /// autoexploring with the player's action provider, another is the autoplayer.
        /// </summary>
        public abstract bool RequestDelay { get; }

        public ActionProvider(MetaSystem sys)
        {
            Systems = sys;
            Sensors = new() {
                (MyHealth = new((sys, a) => new[] { a.ActorProperties.Health })),
                (MyWeapons = new((sys, a) => a.Inventory.GetWeapons()
                    .OrderByDescending(w => w.WeaponProperties.DamagePerTurn))),
                (MyConsumables = new((sys, a) => a.Inventory.GetConsumables()
                    .Where(v => v.ItemProperties.Identified))),
                (MyUnidentifiedConsumables = new((sys, a) => a.Inventory.GetConsumables()
                    .Where(v => !v.ItemProperties.Identified))),
                (MyHelpfulConsumables = new((sys, a) => MyConsumables.AlertingValues
                    .Where(v => v.GetEffectFlags().IsDefensive))),
                (MyHarmfulConsumables = new((sys, a) => MyConsumables.AlertingValues
                    .Where(v => v.TryCast<Projectile>(out var t) && t.ProjectileProperties.BaseDamage > 0
                             || v.GetEffectFlags().IsOffensive))),
                (MyPanicButtons = new((sys, a) => MyConsumables.AlertingValues
                    .Where(v => v.GetEffectFlags().IsPanicButton))),
                (NearbyAllies = new((sys, a) => {
                    return a.Fov.VisibleTiles.TryGetValue(a.FloorId(), out var set)
                    ? set
                     .SelectMany(p => sys.Get<DungeonSystem>().GetActorsAt(a.FloorId(), p))
                     .Where(b => a != b && sys.Get<FactionSystem>().GetRelations(a, b).Right.IsFriendly() && a.CanSee(b))
                    : Enumerable.Empty<Actor>();
                })),
                (NearbyEnemies = new((sys, a) => {
                    return a.Fov.VisibleTiles.TryGetValue(a.FloorId(), out var set)
                    ? set
                     .SelectMany(p => sys.Get<DungeonSystem>().GetActorsAt(a.FloorId(), p))
                     .Where(b => a != b && sys.Get<FactionSystem>().GetRelations(a, b).Right.IsHostile() && a.CanSee(b)
                        && NearbyAllies.Values.Count(v => v.Ai != null && v.Ai.Target == b) < 3)
                    : Enumerable.Empty<Actor>();
                })),
                (NearbyItems = new((sys, a) => {
                    return a.Fov.VisibleTiles.TryGetValue(a.FloorId(), out var set)
                    ? set
                     .SelectMany(p => sys.Get<DungeonSystem>().GetItemsAt(a.FloorId(), p))
                     .OrderBy(i => i.SquaredDistanceFrom(a))
                    : Enumerable.Empty<Item>();
                }))
            };

            MyHealth.ConfigureAlert((s, a, v) => v.Percentage <= 0.25f);
            MyConsumables.ConfigureAlert((s, a, v) => v.ConsumableProperties.RemainingUses > 0);
            MyWeapons.ConfigureAlert((s, a, v) => !a.ActorEquipment.Weapons.Any() || a.ActorEquipment.Weapons.Any(w => v.WeaponProperties.DamagePerTurn > w.WeaponProperties.DamagePerTurn));
            NearbyItems.ConfigureAlert((s, a, v) => !a.Inventory.Full && a.Ai.LikedItems.Any(f => f(v)) && !a.Ai.DislikedItems.Any(f => f(v)));
        }

        public virtual IAction GetIntent(Actor actor)
        {
            foreach (var sensor in Sensors)
            {
                sensor.Update(Systems, actor);
            }
            UpdateCounters();
            return new FailAction();
        }
        public abstract bool TryTarget(Actor a, TargetingShape shape, bool autotargetSuccesful);

        protected virtual void UpdateCounters()
        {
            if (NearbyEnemies.Values.Count == 0)
            {
                TurnsSinceSightingHostile++;
            }
            else
            {
                TurnsSinceSightingHostile = 0;
            }
            if (NearbyAllies.Values.Count == 0)
            {
                TurnsSinceSightingFriendly++;
            }
            else
            {
                TurnsSinceSightingFriendly = 0;
            }
        }

        protected virtual bool TryPushObjective(Actor a, PhysicalEntity target, Func<IAction> goal = null)
        {
            a.Ai.Objectives.Push(new(target, goal));
            return TryRecalculatePath(a);
        }

        protected virtual bool TryGetUnexploredCandidate(Actor a, out Tile tile)
        {
            tile = default;
            var floor = Systems.Get<DungeonSystem>();
            var floorId = a.FloorId();
            if (!a.Fov.KnownTiles.TryGetValue(floorId, out var knownTiles))
                return false;
            var unknownTiles = floor.GetAllTiles(floorId)
                .Where(x => !knownTiles.Contains(x.Physics.Position))
                .Select(x => x.Physics.Position)
                .ToHashSet();
            var bestCandidate = knownTiles
                .Select(x => (P: x, N: Shapes.Neighborhood(x, 3).Sum(x =>
                    (unknownTiles.Contains(x) ? 1 : 0) +
                    (a.Fov.VisibleTiles[floorId].Contains(x) ? -.121f : 0)
                )))
                .Where(t => t.N > 0)
                .OrderByDescending(t => t.N)
                .ThenBy(t => t.P.DistSq(a.Physics.Position))
                .TrySelect(t => floor.TryGetCellAt(floorId, t.P, out var mapCell) ? (true, mapCell) : (false, default))
                .Where(c => c.IsWalkable(a))
                .Select(t => Maybe.Some(t.Tile))
                .FirstOrDefault();
            return bestCandidate.TryGetValue(out tile);
        }

        protected virtual bool TryFollowPath(Actor a, out IAction action)
        {
            action = default;
            if (a.Ai.Path != null && a.Ai.Path.First != null)
            {
                var pos = a.Ai.Path.First.Value.Tile.Position();
                var dir = new Coord(pos.X - a.Position().X, pos.Y - a.Position().Y);
                var diff = Math.Abs(dir.X) + Math.Abs(dir.Y);
                a.Ai.Path.RemoveFirst();
                if (diff > 0 && diff <= 2)
                {
                    // one tile ahead
                    if (Systems.Get<DungeonSystem>().TryGetCellAt(a.FloorId(), pos, out var cell)
                        && cell.IsWalkable(a) && !cell.Actors.Any())
                    {
                        action = new MoveRelativeAction(dir, a.Physics.MoveDelay);
                        return true;
                    }
                }
            }
            // Destination was reached
            else if (a.Ai.Path != null && a.Ai.Path.First == null)
            {
                a.Ai.Path = null;
                if (a.Ai.Objectives.TryPop(out var objective) && objective.Goal != null)
                    action = objective.Goal();
                else action = new FailAction();
                return true;
            }
            // Path recalculation is necessary
            a.Ai.Path = null;
            return false;
        }

        protected virtual bool TryRecalculatePath(Actor a)
        {
            if (a.Ai.Path != null && a.Ai.Path.Last != null && a.Ai.Path.Last.Value.Tile.Position() == a.Position())
                return false;
            var currentObjective = a.Ai.Objectives.Peek();
            var floor = Systems.Get<DungeonSystem>().GetFloor(a.FloorId());
            a.Ai.Path = floor.Pathfinder.Search(a.Position(), currentObjective.Target.Position(), a);
            a.Ai.Path?.RemoveFirst();
            var ret = a.Ai.Path != null && a.Ai.Path.Count > 0;
            if (!ret)
            {
                a.Ai.Objectives.Pop();
                a.Ai.Path = null;
            }
            return ret;
        }

        protected Actor GetClosestHostile(Actor a) => NearbyEnemies.Values
            .OrderBy(a.SquaredDistanceFrom)
            .FirstOrDefault();


        protected Actor GetClosestFriendly(Actor a) => NearbyAllies.Values
            .OrderBy(a.SquaredDistanceFrom)
            .FirstOrDefault();

        protected virtual bool TryUseItem(Actor a, Item item, out IAction action)
        {
            action = default;
            var flags = item.GetEffectFlags();
            if (item.TryCast<Potion>(out var potion) && flags.IsDefensive && Panic)
            {
                action = new QuaffPotionAction(potion);
                return true;
            }
            if (item.TryCast<Scroll>(out var scroll))
            {
                action = new ReadScrollAction(scroll);
                return true;
            }
            if (item.TryCast<Wand>(out var wand))
            {
                return TryZap(a, wand, out action);
            }
            if (item.TryCast<Launcher>(out var Launcher))
            {
                return TryShoot(a, Launcher, out action);
            }
            if (item.TryCast<Projectile>(out var Projectile))
            {
                return TryThrow(a, Projectile, out action);
            }
            return false;
        }

        protected bool TryZap(Actor a, Wand wand, out IAction action)
        {
            var floorId = a.FloorId();
            var flags = wand.GetEffectFlags();
            // All wands use the same targeting shape and have "infinite" range
            var line = Shapes.Line(new(0, 0), new(0, 100)).Skip(1).ToArray();
            var zapShape = new RayTargetingShape(a.Position(), 100);
            var autoTarget = zapShape.TryAutoTarget(
                p => Systems.Get<DungeonSystem>().GetActorsAt(floorId, p).Any(b =>
                {
                    var rel = Systems.Get<FactionSystem>().GetRelations(a, b);
                    if (!wand.ItemProperties.Identified && rel.Right.IsHostile())
                        return true;
                    if (wand.Effects != null && wand.Effects.Intrinsic.All(e => b.Effects.Active.Any(f => f.Name == e.Name)))
                        return false;
                    if (rel.Right.IsFriendly() && flags.IsDefensive)
                        return true;
                    if (rel.Right.IsHostile() && flags.IsOffensive)
                        return true;
                    return false;
                }),
                p => !Systems.Get<DungeonSystem>().GetCellAt(floorId, p)?.BlocksMovement(true) ?? true
            );
            if (TryTarget(a, zapShape, autoTarget))
            {
                var points = zapShape.GetPoints().ToArray();
                foreach (var p in points)
                {
                    var target = Systems.Get<DungeonSystem>().GetActorsAt(floorId, p)
                        .FirstOrDefault();
                    if (target != null)
                    {
                        action = new ZapWandAtOtherAction(wand, target);
                        return true;
                    }
                }
                // Okay, then
                action = new ZapWandAtPointAction(wand, points.Last() - a.Position());
                return true;
            }
            action = default;
            return false;
        }

        protected bool TryShoot(Actor a, Launcher launcher, out IAction action)
        {
            action = default;
            if (!a.ActorEquipment.IsEquipped(launcher))
                return false;
            var clone = (Projectile)launcher.LauncherProperties.Projectile.Clone();
            if (TryThrow(a, clone, out var throwAction))
            {
                action = throwAction switch
                {
                    ThrowItemAtOtherAction { Victim: var v } => new ShootLauncherAtOtherAction(launcher, v),
                    ThrowItemAtPointAction { Point: var p } => new ShootLauncherAtPointAction(launcher, p),
                    _ => throw new NotSupportedException()
                };
                return true;
            }
            return false;
        }


        protected bool TryThrow(Actor a, Projectile proj, out IAction action)
        {
            var floorId = a.FloorId();
            var len = proj.ProjectileProperties.MaximumRange;
            var flags = proj.GetEffectFlags();
            TargetingShape throwShape = proj.ProjectileProperties.Trajectory switch
            {
                TrajectoryName.Line => new LineTargetingShape(a.Position(), proj.ProjectileProperties.MinimumRange, len),
                TrajectoryName.Arc => new PointTargetingShape(a.Position(), proj.ProjectileProperties.MaximumRange),
                _ => throw new NotSupportedException()
            };
            var autoTarget = throwShape.TryAutoTarget(
                p => Systems.Get<DungeonSystem>().GetActorsAt(floorId, p).Any(b =>
                {
                    if (!proj.ItemProperties.Identified)
                        return true;
                    var rel = Systems.Get<FactionSystem>().GetRelations(a, b);
                    if (proj.Effects != null && b.Effects != null && proj.Effects.Intrinsic.All(e => b.Effects.Active.Any(f => f.Name == e.Name)))
                        return false;
                    if (rel.Left.IsFriendly() && flags.IsDefensive)
                        return true;
                    if (rel.Left.IsHostile() && flags.IsOffensive)
                        return true;
                    if (rel.Left.IsHostile() && proj.ProjectileProperties.BaseDamage > 0)
                        return true;
                    return false;
                }),
                p =>
                {
                    if (!Systems.Get<DungeonSystem>().TryGetCellAt(floorId, p, out var cell))
                        return true;
                    if (proj.ProjectileProperties.Trajectory == TrajectoryName.Line && cell.BlocksMovement(excludeFlat: true))
                        return true;
                    if (proj.ProjectileProperties.Trajectory == TrajectoryName.Arc && cell.BlocksMovement(excludeFlat: true, excludeFeatures: true))
                        return true;
                    return false;
                }
            );
            if (TryTarget(a, throwShape, autoTarget))
            {
                var points = throwShape.GetPoints().ToArray();
                if (points.Length == 0)
                {
                    action = default;
                    return false;
                }
                // Okay, then
                action = new ThrowItemAtPointAction(points.Last() - a.Position(), proj);
                return true;
            }
            action = default;
            return false;
        }
    }
}
