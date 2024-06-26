﻿using Unconcern.Common;

namespace Fiero.Core
{
    public abstract class Script(string name)
    {
        public readonly string Name = name;

        public readonly record struct EventHook(string System, string Event);
        public readonly record struct DataHook(string Module, string Name);
        public abstract IEnumerable<EventHook> EventHooks { get; }
        public abstract IEnumerable<DataHook> DataHooks { get; }
        public abstract Subscription Run(ScriptEventRoutes eventRoutes, ScriptDataRoutes dataRoutes);


    }
}
