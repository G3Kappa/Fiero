﻿namespace Fiero.Business
{
    public partial class ActionSystem : EcsSystem
    {
        private bool HandleInteract(ActorTime t, ref IAction action, ref int? cost)
        {
            if (action is InteractRelativeAction genericUse)
            {
                return HandleGenericUse(genericUse.Coord, ref action, ref cost);
            }
            else if (action is InteractWithFeatureAction useFeature)
            {
                return FeatureInteractedWith.Handle(new(t.Actor, useFeature.Feature));
            }
            else if (action is PickUpItemAction pickUp)
            {
                return ItemPickedUp.Handle(new(t.Actor, pickUp.Item));
            }
            else if (action is InitiateConversationAction conv)
            {
                return ConversationInitiated.Handle(new(t.Actor, conv.Speaker));
            }
            else throw new NotSupportedException();
            bool HandleGenericUse(Coord point, ref IAction action, ref int? cost)
            {
                // Use handles both grabbing items from the ground and using dungeon features
                var floorId = t.Actor.FloorId();
                var usePos = t.Actor.Position() + point;
                var itemsHere = _floorSystem.GetItemsAt(floorId, usePos).ToList();
                var featuresHere = _floorSystem.GetFeaturesAt(floorId, usePos).ToList();
                if (itemsHere.Any() && t.Actor.Inventory != null)
                {
                    var item = itemsHere.First();
                    action = new PickUpItemAction(item);
                    cost = HandleAction(t, ref action);
                }
                else if (featuresHere.Any())
                {
                    var feature = featuresHere.Single();
                    action = new InteractWithFeatureAction(feature);
                    cost = HandleAction(t, ref action);
                }
                return true;
            }
        }
    }
}
