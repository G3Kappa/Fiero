﻿using Unconcern.Common;

namespace Fiero.Core
{
    public static class EventsExtensions
    {
        public static bool Handle<TSys, TArgs>(this SystemRequest<TSys, TArgs, EventResult> req, TArgs payload)
            where TSys : EcsSystem
        {
            var ret = req.Request(payload)
                .ToEnumerable()
                .All(x => x);
            return ret;
        }
        public static ValueTask<bool> HandleAsync<TSys, TArgs>(this SystemRequest<TSys, TArgs, EventResult> req, TArgs payload, CancellationToken ct = default)
            where TSys : EcsSystem
        {
            return req.Request(payload)
                .AllAsync(x => x, ct);
        }

        public static void HandleOrThrow<TSys, TArgs>(this SystemRequest<TSys, TArgs, EventResult> req, TArgs payload)
            where TSys : EcsSystem
        {
            if (!req.Handle(payload))
                throw new InvalidOperationException();
        }

        public static async ValueTask HandleOrThrowAsync<TSys, TArgs>(this SystemRequest<TSys, TArgs, EventResult> req, TArgs payload, CancellationToken ct = default)
            where TSys : EcsSystem
        {
            if (!await req.HandleAsync(payload, ct))
                throw new InvalidOperationException();
        }

        public static void SubscribeUntil<TSys, TArgs>(this SystemEvent<TSys, TArgs> evt, Func<TArgs, bool> until)
            where TSys : EcsSystem
        {
            var sub = default(Subscription);
            sub = evt.SubscribeHandler(msg =>
            {
                if (until(msg))
                {
                    sub.Dispose();
                }
            });
        }
    }
}
