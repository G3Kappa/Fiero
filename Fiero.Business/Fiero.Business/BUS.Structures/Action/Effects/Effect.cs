﻿using System;
using System.Collections.Generic;
using Unconcern.Common;

namespace Fiero.Business
{
    public abstract class Effect
    {
        protected readonly HashSet<Subscription> Subscriptions = new();

        public event Action<Effect> Started;
        public event Action<Effect> Ended;

        protected abstract IEnumerable<Subscription> RouteEvents(GameSystems systems, Entity owner);
        protected virtual void OnStarted(GameSystems systems, Entity owner) { }
        public void Start(GameSystems systems, Entity owner)
        {
            if(owner.TryCast<Actor>(out var actor)) {
                Started += e => actor.Effects?.Active.Add(e);
                Ended += e => actor.Effects?.Active.Remove(e);
            }
            Subscriptions.UnionWith(RouteEvents(systems, owner));
            Started?.Invoke(this);
            OnStarted(systems, owner);
        }

        protected virtual void OnEnded() { }
        public void End()
        {
            foreach (var sub in Subscriptions) {
                sub.Dispose();
            }
            OnEnded();
            Ended?.Invoke(this);
        }
    }
}
