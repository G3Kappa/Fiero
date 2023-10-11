﻿using System;
using System.Collections.Generic;
using Unconcern.Common;

namespace Unconcern.Delegation
{
    public class DelegateExpression : IDelegateExpression
    {
        public EventBus Bus { get; private set; }

        protected List<Func<EventBus.Message, bool>> TriggersList;
        public IEnumerable<Func<EventBus.Message, bool>> Triggers => TriggersList;
        protected List<Func<EventBus.Message, bool>> PreTriggersList;

        protected List<Func<EventBus.Message, EventBus.Message>> RepliesList;
        public IEnumerable<Func<EventBus.Message, EventBus.Message>> Replies => RepliesList;

        protected List<Action<EventBus.Message>> HandlersList;
        public IEnumerable<Action<EventBus.Message>> Handlers => HandlersList;

        protected List<Action<EventBus.Message>> PreHandlersList;
        public IEnumerable<Action<EventBus.Message>> PreHandlers => PreHandlersList;

        protected List<Action<EventBus.Message>> PostHandlersList;
        public IEnumerable<Action<EventBus.Message>> PostHandlers => PostHandlersList;

        protected List<IDelegateExpression> SiblingsList;
        public IEnumerable<IDelegateExpression> Siblings => SiblingsList;


        internal DelegateExpression(
            EventBus bus,
            IEnumerable<Func<EventBus.Message, bool>> triggers,
            IEnumerable<Func<EventBus.Message, EventBus.Message>> replies,
            IEnumerable<Action<EventBus.Message>> preHandlers,
            IEnumerable<Action<EventBus.Message>> handlers,
            IEnumerable<Action<EventBus.Message>> postHandlers,
            IEnumerable<IDelegateExpression> siblings)
        {
            Bus = bus;
            TriggersList = new List<Func<EventBus.Message, bool>>(triggers);
            RepliesList = new List<Func<EventBus.Message, EventBus.Message>>(replies);
            PreHandlersList = new List<Action<EventBus.Message>>(preHandlers);
            HandlersList = new List<Action<EventBus.Message>>(handlers);
            PostHandlersList = new List<Action<EventBus.Message>>(postHandlers);
            SiblingsList = new List<IDelegateExpression>(siblings);
        }
    }
}