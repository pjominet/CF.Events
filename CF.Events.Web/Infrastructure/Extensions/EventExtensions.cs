using CF.Events.Web.Models;

namespace CF.Events.Web.Infrastructure.Extensions;

public static class EventExtensions
{
    extension(Event @event)
    {
        public List<string> ExtractEventImageFileNames()
        {
            return @event.Description.ExtractImageFileNamesFromJsonString()
                .Concat(@event.TravelInstructions?.ExtractImageFileNamesFromJsonString() ?? [])
                .Distinct()
                .ToList();
        }

        public string GetDonationReference() => $"{@event.Name}{@event.StartDate.Month}{@event.StartDate.Year}";
    }
}
