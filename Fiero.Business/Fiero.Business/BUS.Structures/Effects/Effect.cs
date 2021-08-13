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

        public abstract EffectName Type { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }


        protected abstract IEnumerable<Subscription> RouteEvents(GameSystems systems, Entity owner);
        protected virtual void OnStarted(GameSystems systems, Entity owner) { }
        public void Start(GameSystems systems, Entity owner)
        {
            Subscriptions.Add(systems.Action.GameStarted.SubscribeHandler(e => {
                // Force cleanup of old effects when a new game is started
                try {
                    End();
                }
                catch (ObjectDisposedException) { }
            }));
            Started += e => owner.Effects?.Active.Add(e);
            Ended += e => owner.Effects?.Active.Remove(e);
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
