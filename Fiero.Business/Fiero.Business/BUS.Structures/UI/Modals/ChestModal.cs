﻿using Fiero.Core;
using System.Collections.Generic;

namespace Fiero.Business
{
    public class ChestModal : ContainerModal<Feature, ChestActionName>
    {
        public readonly bool CanTake;

        public ChestModal(GameUI ui, GameResources resources, Feature ft, bool canTake)
            : base(ui, resources, ft, new ModalWindowButton[] { })
        {
            CanTake = canTake;
        }

        protected override bool ShouldRemoveItem(Item i, ChestActionName a)
        {
            var shouldRemoveMenuItem = a == ChestActionName.Take || a == ChestActionName.Drop;
            return shouldRemoveMenuItem;
        }

        protected override IEnumerable<ChestActionName> GetAvailableActions(Item i)
        {
            if (CanTake)
            {
                yield return ChestActionName.Take;
            }
            yield return ChestActionName.Drop;
        }
    }
}
