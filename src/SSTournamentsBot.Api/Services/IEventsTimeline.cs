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
        void RemoveAllEventsWithType(Event ev);
        void RemoveAllEvents();

        Event[] GetEventsRaisedInPeriod(DateTime time, TimeSpan period, bool removeIfOneTime = false);
        (Event Event, DateTime Date, TimeSpan? Period)? GetNextEventInfo();
        (Event Event, DateTime Date, TimeSpan? Period)[] GetAllScheduledEvents();
        void AddTimeToNextEventWithType(Event completeStage, TimeSpan time);
    }
}