﻿using Ergo.Lang;

namespace Fiero.Business
{
    public class PhysicsComponent : EcsComponent
    {
        [Term(Marshalling = TermMarshalling.Positional)]
        public FloorId FloorId { get; set; }
        [Term(Marshalling = TermMarshalling.Positional, Functor = "p")]
        public Coord Position { get; set; }
        public int Roots { get; set; }
        // Can phase through solid objects
        public bool Phasing { get; set; }
        // Can fly above the ground
        public bool Flying { get; set; }
        public bool CanMove { get; set; }
        public bool BlocksMovement { get; set; }
        public bool BlocksNpcPathing { get; set; }
        public bool BlocksPlayerPathing { get; set; }
        public bool BlocksLight { get; set; }
    }
}
