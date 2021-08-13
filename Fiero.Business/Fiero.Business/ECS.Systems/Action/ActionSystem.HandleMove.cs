﻿using Fiero.Core;
using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Unconcern.Common;

namespace Fiero.Business
{
    public partial class ActionSystem : EcsSystem
    {
        private bool HandleMove(ActorTime t, ref IAction action, ref int? cost)
        {
            var direction = default(Coord);
            if (action is MoveRelativeAction rel)
                direction = rel.Coord;
            else if (action is MoveRandomlyAction ran)
                direction = new(Rng.Random.Next(-1, 2), Rng.Random.Next(-1, 2));
            else if (action is MoveTowardsAction tow)
                direction = tow.Follow.Position() - t.Actor.Position();
            else throw new NotSupportedException();

            var floorId = t.Actor.FloorId();
            var oldPos = t.Actor.Position();
            var newPos = t.Actor.Position() + direction;
            if (newPos == t.Actor.Position()) {
                action = new WaitAction();
                cost = HandleAction(t, ref action);
            }
            else if (_floorSystem.TryGetTileAt(floorId, newPos, out var tile)) {
                if (!tile.Physics.BlocksMovement) {
                    var actorsHere = _floorSystem.GetActorsAt(floorId, newPos);
                    var featuresHere = _floorSystem.GetFeaturesAt(floorId, newPos);
                    if (!actorsHere.Any()) {
                        if (!featuresHere.Any(f => f.Physics.BlocksMovement)) {
                            return ActorMoved.Handle(new(t.Actor, oldPos, newPos));
                        }
                        else {
                            var feature = featuresHere.Single();
                            ActorBumpedObstacle.Raise(new(t.Actor, feature));
                            // you can bump shrines and chests to interact with them
                            action = new InteractWithFeatureAction(feature); 
                            cost = HandleAction(t, ref action);
                        }
                    }
                    else {
                        var target = actorsHere.Single();
                        var relationship = _factionSystem.GetRelationships(t.Actor, target).Left;
                        if (relationship.MayAttack()) {
                            // attack-bump is a free "combo"
                            action = new MeleeAttackOtherAction(target, t.Actor.Equipment.Weapon);
                            cost = HandleAction(t, ref action);
                        }
                        else if (relationship.IsFriendly()) {
                            // you can swap position with allies in twice the amount of time it takes to move
                            cost *= 2;
                            return    ActorMoved.Handle(new(t.Actor, oldPos, newPos))
                                   && ActorMoved.Handle(new(target, newPos, oldPos));
                        }
                    }
                }
                else {
                    ActorBumpedObstacle.Raise(new(t.Actor, tile));
                    return false;
                }
            }
            else {
                // Bumped "nothingness"
                ActorBumpedObstacle.Raise(new(t.Actor, null));
                return false;
            }
            return true;
        }
    }
}