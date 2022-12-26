using System;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public interface IEventsTimeline
    {
        void AddPeriodicalEventOnSingleDayTime(Event ev, DateTime time);
        void AddPeriodicalEventWithPeriod(Event ev, TimeSpan time);
        void AddOneTimeEventAfterTime(Event ev, TimeSpan time);
        void AddOneTimeEventOnDate(Event ev, DateTime date);
        void RemoveAllEvents();

        void ForceNextEvent();

        Event[] GetEventsRaisedInPeriod(DateTime time, TimeSpan period, bool removeIfOneTime = false);
        (Event Event, DateTime Date, TimeSpan? Period)? GetNextEventInfo();
        (Event Event, DateTime Date, TimeSpan? Period)[] GetAllScheduledEvents();
    }
}