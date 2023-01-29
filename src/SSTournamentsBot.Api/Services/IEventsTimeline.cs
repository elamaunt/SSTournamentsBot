using System;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public interface IEventsTimeline
    {
        void AddPeriodicalEventOnSingleDayTime(string contextName, Event ev, DateTime time);
        void AddPeriodicalEventWithPeriod(string contextName, Event ev, TimeSpan time);
        void AddOneTimeEventAfterTime(string contextName, Event ev, TimeSpan time);
        void AddOneTimeEventOnDate(string contextName, Event ev, DateTime date);
        void RemoveAllEventsWithType(string contextName, Event ev);
        void RemoveAllEvents(string contextName);

        Event[] GetEventsRaisedInPeriod(DateTime time, TimeSpan period, bool removeIfOneTime = false);
        EventInfo GetNextEventInfoForContext(string contextName);
        EventInfo[] GetAllScheduledEvents();
        void AddTimeToNextEventWithType(string contextName, Event ev, TimeSpan time);
        bool HasEventToday(string contextName, Event ev);
        void RemoveEventInfo(EventInfo ev);
    }
}