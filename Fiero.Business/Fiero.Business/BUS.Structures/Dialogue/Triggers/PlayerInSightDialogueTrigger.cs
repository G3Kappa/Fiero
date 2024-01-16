﻿namespace Fiero.Business
{
    public class PlayerInSightDialogueTrigger<TDialogue> : DialogueTrigger<TDialogue>
        where TDialogue : struct, Enum
    {
        public float DistanceThreshold { get; set; } = 5;

        public PlayerInSightDialogueTrigger(MetaSystem sys, bool repeatable, string path, params TDialogue[] nodeChoices)
            : base(sys, repeatable, path, nodeChoices)
        {

        }

        public override bool TryTrigger(FloorId floorId, PhysicalEntity speaker, out IEnumerable<DrawableEntity> listeners)
        {
            listeners = Systems.Get<DungeonSystem>().GetAllActors(floorId)
                .Where(a => a.IsPlayer()
                    && a.DistanceFrom(speaker) < DistanceThreshold
                    && a.CanSee(speaker));
            return listeners.Any();
        }
    }
}
