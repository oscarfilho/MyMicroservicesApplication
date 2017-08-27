﻿using System;

namespace IntegrationEventsContext.Models
{
    public class IntegrationEventInstance : IntegrationEvent
    {
        public string Id { get; }

        public IntegrationEventInstance(string id, string eventType) :
            base(eventType)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }
    }
}