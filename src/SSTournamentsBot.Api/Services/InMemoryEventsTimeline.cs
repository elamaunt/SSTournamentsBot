using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class InMemoryEventsTimeline : IEventsTimeline
    {
        const int MaxSimultaneousEvents = 16;

        readonly ScheduledEventInfo[] _events = new ScheduledEventInfo[MaxSimultaneousEvents];
        readonly static Event[] ZeroEvents = new Event[0];

        public void AddOneTimeEventAfterTime(string contextName, Event ev, TimeSpan time)
        {
            Add(new ScheduledEventInfo()
            {
                Event = ev,
                Type = EventType.AfterTime,
                Time = time,
                Date = GetMoscowTime(),
                ContextName = contextName
            });
        }

        public void AddOneTimeEventOnDate(string contextName, Event ev, DateTime date)
        {
            Add(new ScheduledEventInfo()
            { 
                Event = ev,
                Type = EventType.ExactTime,
                Date = date,
                ContextName = contextName
            });
        }

        public void AddPeriodicalEventOnSingleDayTime(string contextName, Event ev, DateTime date)
        {
            Add(new ScheduledEventInfo()
            {
                Event = ev,
                Type = EventType.ExactTime,
                Date = date,
                Time = TimeSpan.FromDays(1),
                Periodic = true,
                ContextName = contextName
            });
        }

        public void AddPeriodicalEventWithPeriod(string contextName, Event ev, TimeSpan time)
        {
            Add(new ScheduledEventInfo()
            {
                Event = ev,
                Type = EventType.AfterTime,
                Time = time,
                Periodic = true,
                Date = GetMoscowTime(),
                ContextName = contextName
            });
        }

        private void Add(ScheduledEventInfo scheduledEventInfo)
        {
            for (int i = 0; i < _events.Length; i++)
            {
                if (Interlocked.CompareExchange(ref _events[i], scheduledEventInfo, null) == null)
                    return;
            }

            throw new InvalidOperationException("Event is lost");
        }

        
        public void RemoveEventInfo(EventInfo ev)
        {
            for (int i = 0; i < _events.Length; i++)
            {
                var info = _events[i];
                
                if (info.Event == ev.Event && GetEventsDate(info) == ev.StartDate)
                    Interlocked.Exchange(ref _events[i], null);
            }
        }

        public void RemoveAllEvents(string contextName)
        {
            for (int i = 0; i < _events.Length; i++)
            {
                var info = _events[i];
                if (info?.ContextName == contextName)
                    Interlocked.Exchange(ref _events[i], null);
            }
        }

        public Event[] GetEventsRaisedInPeriod(DateTime time, TimeSpan period, bool removeIfOneTime = false)
        {
            List<Event> list = null;
            for (int i = 0; i < _events.Length; i++)
            {
                var info = _events[i];

                if (info == null)
                    continue;

                switch (info.Type)
                {
                    case EventType.AfterTime:
                        {
                            var raiseTime = info.Date + info.Time + info.TimeExtension;

                            if (raiseTime > time && raiseTime <= time + period)
                            {
                                if (list == null) list = new List<Event>();
                                list.Add(info.Event);
                                info.TimeExtension = TimeSpan.Zero;

                                if (info.Periodic)
                                    info.Date += info.Time;
                                else if (removeIfOneTime)
                                    Interlocked.CompareExchange(ref _events[i], null, info);
                            }
                            break;
                        }
                    case EventType.ExactTime:
                        {
                            var date = info.Date + info.TimeExtension;
                             
                            if (date > time && date <= time + period)
                            {
                                if (list == null) list = new List<Event>();
                                list.Add(info.Event);
                                info.TimeExtension = TimeSpan.Zero;

                                if (info.Periodic)
                                    info.Date += info.Time;
                                else if (removeIfOneTime)
                                    Interlocked.CompareExchange(ref _events[i], null, info);
                            }
                            break;
                        }

                    default:
                        throw new NotSupportedException();
                }
            }

            if (list == null)
                return ZeroEvents;

            return list.ToArray();
        }

        public EventInfo GetNextEventInfoForContext(string contextName)
        {
            return _events.Where(x => x != null && x.ContextName == contextName).OrderBy(GetEventsDate)
            .Select(x =>
            {
                var period = x.Periodic ? new FSharpOption<TimeSpan>(x.Time) : FSharpOption<TimeSpan>.None;

                switch (x.Type)
                {
                    case EventType.AfterTime:
                        return new EventInfo(x.ContextName, x.Event, x.Date + x.Time + x.TimeExtension, period);
                    case EventType.ExactTime:
                        return new EventInfo(x.ContextName, x.Event, x.Date + x.TimeExtension, period);
                    default:
                        return new EventInfo(x.ContextName, x.Event, x.Date + x.TimeExtension, period);
                }
            })
            .FirstOrDefault();
        }

        public EventInfo[] GetAllScheduledEvents()
        {
            return _events.Where(x => x != null).OrderBy(GetEventsDate)
            .Select(x =>
            {
                var period = x.Periodic ? new FSharpOption<TimeSpan>(x.Time) : FSharpOption<TimeSpan>.None;

                switch (x.Type)
                {
                    case EventType.AfterTime:
                        return new EventInfo(x.ContextName, x.Event, x.Date + x.Time + x.TimeExtension, period);
                    case EventType.ExactTime:
                        return new EventInfo(x.ContextName, x.Event, x.Date + x.TimeExtension, period);
                    default:
                        return new EventInfo(x.ContextName, x.Event, x.Date + x.TimeExtension, period);
                }
            }).ToArray();
        }

        private DateTime GetEventsDate(ScheduledEventInfo info)
        {
            switch (info.Type)
            {
                case EventType.AfterTime:
                    return info.Date + info.Time + info.TimeExtension;
                case EventType.ExactTime:
                    return info.Date + info.TimeExtension;
                default:
                    return info.Date + info.TimeExtension;
            }
        }

        public void RemoveAllEventsWithType(string contextName, Event ev)
        {
            for (int i = 0; i < _events.Length; i++)
            {
                var info = _events[i];

                if (info?.Event == ev && info?.ContextName == contextName)
                    Interlocked.CompareExchange(ref _events[i], null, info);
            }
        }

        public void AddTimeToNextEventWithType(string contextName, Event ev, TimeSpan time)
        {
            for (int i = 0; i < _events.Length; i++)
            {
                var info = _events[i];

                if (info?.Event == ev && info?.ContextName == contextName)
                {
                    info.TimeExtension += time;
                    break;
                }
            }
        }

        public bool HasEventToday(string contextName, Event ev)
        {
            for (int i = 0; i < _events.Length; i++)
            {
                var info = _events[i];

                if (info?.Event == ev && info?.ContextName == contextName)
                {
                    var date = GetEventsDate(info);

                    if (date.Date == GetMoscowTime().Date)
                        return true;
                }
            }

            return false;
        }

        private enum EventType
        {
            AfterTime,
            ExactTime
        }

        private class ScheduledEventInfo
        {
            public EventType Type;
            public TimeSpan Time;
            public DateTime Date;
            public Event Event;
            public bool Periodic;
            public TimeSpan TimeExtension;
            public string ContextName;
        }
    }
}
