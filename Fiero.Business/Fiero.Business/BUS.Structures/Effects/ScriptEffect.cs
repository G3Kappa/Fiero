﻿using System.Collections.Generic;
using Unconcern.Common;

namespace Fiero.Business
{

    /// <summary>
    /// Script effects can be applied to:
    /// - Entities:
    ///     - The effect is applied to the entity when the effect starts, and is removed when the effect ends.
    /// </summary>
    public class ScriptEffect : Effect
    {
        public readonly Script Script;
        public readonly string Description;

        public ScriptEffect(Script script, string description = null)
        {
            Script = script;
            Description = description ?? string.Empty;
        }

        public override EffectName Name => EffectName.Script;
        public override string DisplayName => Script.Info.Name;
        public override string DisplayDescription => Description;

        protected override IEnumerable<Subscription> RouteEvents(GameSystems systems, Entity owner)
        {
            /* Ergo scripts can subscribe to Fiero events via the subscribe/2 directive.
               All the directive does is prepare a list for this method, which is
               called whenever an effect that is tied to a script resolves. The list
               contains the signatures of the events that the script is handling.

               By wiring each event to a call to the script's solver, we can interpret
               the result of that call as the EventResult to pass to the owning system.
            */
            var routes = ErgoScriptingSystem.GetScriptRoutes();
            foreach (var sig in Script.ScriptProperties.SubscribedEvents)
            {
                if (routes.TryGetValue(sig, out var sub))
                {
                    yield return sub(this, systems);
                }
                else
                {
                    // TODO: Warn script?
                }
            }
        }
    }
}
