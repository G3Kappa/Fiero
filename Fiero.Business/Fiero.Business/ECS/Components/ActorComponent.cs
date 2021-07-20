﻿using Fiero.Core;
using System.Collections.Generic;

namespace Fiero.Business
{
    public class ActorComponent : EcsComponent
    {
        public int MaximumHealth { get; set; } = 5;
        public int Health { get; set; } = 5;

        public ActorName Type { get; set; }
        public MonsterTierName Tier { get; set; }
        public FloorId FloorId { get; set; }
        public ActorRelationships Relationships { get; set; } = new();
    }
}
