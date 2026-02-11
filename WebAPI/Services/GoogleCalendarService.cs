using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Services;

public class GoogleCalendarService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public GoogleCalendarService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    private async Task<CalendarService> GetCalendarServiceAsync(int userId)
    {
        var cred = await _context.GoogleCredentials.FirstOrDefaultAsync(g => g.UserId == userId);
        if (cred == null) throw new Exception("Google account not connected");

        var clientSecret = _configuration["Google:ClientSecret"];
        var clientId = _configuration["Google:ClientId"];

        var tokenResponse = new TokenResponse
        {
            AccessToken = cred.AccessToken,
            RefreshToken = cred.RefreshToken,
            ExpiresInSeconds = (long)(cred.Expiry - DateTime.UtcNow).TotalSeconds
        };

        var initializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret }
        };

        var flow = new GoogleAuthorizationCodeFlow(initializer);
        var userCredential = new UserCredential(flow, userId.ToString(), tokenResponse);

        // Auto-refresh
        if (userCredential.Token.IsStale)
        {
            await userCredential.RefreshTokenAsync(System.Threading.CancellationToken.None);
            cred.AccessToken = userCredential.Token.AccessToken;
            cred.RefreshToken = userCredential.Token.RefreshToken;
            cred.Expiry = DateTime.UtcNow.AddSeconds(userCredential.Token.ExpiresInSeconds ?? 3600);
            await _context.SaveChangesAsync();
        }

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = userCredential,
            ApplicationName = "Il Punto G"
        });
    }

    public async Task<IEnumerable<CalendarListEntry>> GetUserCalendarsAsync(int userId)
    {
        var service = await GetCalendarServiceAsync(userId);
        var list = await service.CalendarList.List().ExecuteAsync();
        return list.Items;
    }

    public async Task<IEnumerable<Event>> GetEventsAsync(int userId, string calendarId, DateTime start, DateTime end)
    {
        var service = await GetCalendarServiceAsync(userId);
        var request = service.Events.List(calendarId);
        request.TimeMinDateTimeOffset = start;
        request.TimeMaxDateTimeOffset = end;
        request.SingleEvents = true; // Expand recurrences
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        var events = await request.ExecuteAsync();
        return events.Items;
    }

    public async Task<Event> CreateEventAsync(int userId, string calendarId, Event ev)
    {
        var service = await GetCalendarServiceAsync(userId);

        // Conflict check
        if (await HasConflictsAsync(userId, calendarId, ev.Start.DateTimeDateTimeOffset?.DateTime ?? DateTime.MinValue, ev.End.DateTimeDateTimeOffset?.DateTime ?? DateTime.MinValue))
        {
            throw new Exception("Conflict detected");
        }

        return await service.Events.Insert(ev, calendarId).ExecuteAsync();
    }

    public async Task<Event> UpdateEventAsync(int userId, string calendarId, string eventId, Event ev, bool allSeries = false)
    {
        var service = await GetCalendarServiceAsync(userId);

        if (allSeries)
        {
            // Update the master event
            return await service.Events.Update(ev, calendarId, eventId).ExecuteAsync();
        }
        else
        {
            // Update only this instance
            return await service.Events.Patch(ev, calendarId, eventId).ExecuteAsync();
        }
    }

    public async Task DeleteEventAsync(int userId, string calendarId, string eventId)
    {
        var service = await GetCalendarServiceAsync(userId);
        await service.Events.Delete(calendarId, eventId).ExecuteAsync();
    }

    public async Task<bool> HasConflictsAsync(int userId, string calendarId, DateTime start, DateTime end)
    {
        var service = await GetCalendarServiceAsync(userId);
        var request = new FreeBusyRequest
        {
            TimeMinDateTimeOffset = start,
            TimeMaxDateTimeOffset = end,
            Items = new List<FreeBusyRequestItem> { new FreeBusyRequestItem { Id = calendarId } }
        };
        var response = await service.Freebusy.Query(request).ExecuteAsync();
        return response.Calendars[calendarId].Busy.Any();
    }
}
