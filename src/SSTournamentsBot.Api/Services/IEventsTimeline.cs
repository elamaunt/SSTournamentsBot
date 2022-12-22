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

        Event[] GetEventsRaisedInPeriod(DateTime time, TimeSpan period, bool removeIfOneTime = false);
        (Event, DateTime, TimeSpan?) GetNextEventInfo();
        (Event, DateTime, TimeSpan?)[] GetAllScheduledEvents();
    }
}