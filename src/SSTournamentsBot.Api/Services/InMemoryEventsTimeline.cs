using System;
using System.Collections.Generic;
using System.Linq;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class InMemoryEventsTimeline : IEventsTimeline
    {
        readonly List<EventInfo> _events = new List<EventInfo>();
        private readonly static Event[] ZeroEvents = new Event[0];

        public void AddOneTimeEventAfterTime(Event ev, TimeSpan time)
        {
            _events.Add(new EventInfo()
            {
                Event = ev,
                Type = EventType.AfterTime,
                TimeSpan = time,
                Date = GetMoscowTime(),
            });
        }

        public void AddOneTimeEventOnDate(Event ev, DateTime date)
        {
            _events.Add(new EventInfo()
            { 
                Event = ev,
                Type = EventType.ExactTime,
                Date = date
            });
        }

        public void AddPeriodicalEventOnSingleDayTime(Event ev, DateTime date)
        {
            _events.Add(new EventInfo()
            {
                Event = ev,
                Type = EventType.ExactTime,
                Date = date,
                TimeSpan = TimeSpan.FromDays(1),
                Periodic = true
            });
        }

        public void AddPeriodicalEventWithPeriod(Event ev, TimeSpan time)
        {
            _events.Add(new EventInfo()
            {
                Event = ev,
                Type = EventType.AfterTime,
                TimeSpan = time,
                Periodic = true,
                Date = GetMoscowTime()
            });
        }

        public void RemoveAllEvents()
        {
            _events.Clear();
        }

        public Event[] GetEventsRaisedInPeriod(DateTime time, TimeSpan period, bool removeIfOneTime = false)
        {
            List<Event> list = null;
            List<EventInfo> toRemove = null;
            for (int i = 0; i < _events.Count; i++)
            {
                var info = _events[i];

                switch (info.Type)
                {
                    case EventType.AfterTime:
                        {
                            var raiseTime = info.Date + info.TimeSpan;

                            if (raiseTime > time && raiseTime <= time + period)
                            {
                                if (list == null) list = new List<Event>();
                                list.Add(info.Event);
                                if (info.Periodic)
                                    info.Date += info.TimeSpan;
                                else if (removeIfOneTime)
                                {
                                    if (toRemove == null) toRemove = new List<EventInfo>();
                                    toRemove.Add(info);
                                }
                            }
                            break;
                        }
                    case EventType.ExactTime:
                        {
                            var date = info.Date;

                            if (date > time && date <= time + period)
                            {
                                if (list == null) list = new List<Event>();
                                list.Add(info.Event);
                                if (info.Periodic)
                                    info.Date += info.TimeSpan;
                                else if (removeIfOneTime)
                                {
                                    if (toRemove == null) toRemove = new List<EventInfo>();
                                    toRemove.Add(info);
                                }
                            }
                            break;
                        }

                    default:
                        throw new NotSupportedException();
                }
            }

            if (list == null)
                return ZeroEvents;

            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                    _events.Remove(toRemove[i]);
            }

            return list.ToArray();
        }

        public (Event Event, DateTime Date, TimeSpan? Period)? GetNextEventInfo()
        {
            return _events.OrderBy(x =>
            {
                switch (x.Type)
                {
                    case EventType.AfterTime:
                        return x.Date + x.TimeSpan;
                    case EventType.ExactTime:
                        return x.Date;
                    default:
                        return x.Date;
                }
            })
            .Select<EventInfo, (Event, DateTime, TimeSpan?)?>(x =>
            {
                switch (x.Type)
                {
                    case EventType.AfterTime:
                        return (x.Event, x.Date + x.TimeSpan, x.Periodic ? new TimeSpan?(x.TimeSpan) : new TimeSpan?());
                    case EventType.ExactTime:
                        return (x.Event, x.Date, x.Periodic ? new TimeSpan?(x.TimeSpan) : new TimeSpan?());
                    default:
                        return (x.Event, x.Date, x.Periodic ? new TimeSpan?(x.TimeSpan) : new TimeSpan?());
                }
            })
            .FirstOrDefault();
        }

        public (Event Event, DateTime Date, TimeSpan? Period)[] GetAllScheduledEvents()
        {
            return _events.OrderBy(x =>
            {
                switch (x.Type)
                {
                    case EventType.AfterTime:
                        return x.Date + x.TimeSpan;
                    case EventType.ExactTime:
                        return x.Date;
                    default:
                        return x.Date;
                }
            })
            .Select(x =>
            {
                switch (x.Type)
                {
                    case EventType.AfterTime:
                        return (x.Event, x.Date + x.TimeSpan, x.Periodic ? new TimeSpan?(x.TimeSpan) : new TimeSpan?());
                    case EventType.ExactTime:
                        return (x.Event, x.Date, x.Periodic ? new TimeSpan?(x.TimeSpan) : new TimeSpan?());
                    default:
                        return (x.Event, x.Date, x.Periodic ? new TimeSpan?(x.TimeSpan) : new TimeSpan?());
                }
            }).ToArray();
        }

        public void ForceNextEvent()
        {
            throw new NotImplementedException();
        }

        private enum EventType
        {
            AfterTime,
            ExactTime
        }

        private class EventInfo
        {
            public EventType Type;
            public TimeSpan TimeSpan;
            public DateTime Date;
            public Event Event;
            public bool Periodic;
        }
    }
}
