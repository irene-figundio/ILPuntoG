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
        var calendarsToSearch = new List<string>();

        if (string.IsNullOrEmpty(calendarId) || calendarId == "all")
        {
            var calendarList = await service.CalendarList.List().ExecuteAsync();

            // Priority 1: "Irene Graldev"
            var irene = calendarList.Items.FirstOrDefault(c => c.Summary.Contains("Irene Graldev", StringComparison.OrdinalIgnoreCase));
            if (irene != null) calendarsToSearch.Add(irene.Id);

            // Priority 2: Primary and Branch calendars
            var branchCalendars = await GetUserBranchCalendarsAsync(userId);
            calendarsToSearch.AddRange(branchCalendars);

            // Distinct in case primary is already there
            calendarsToSearch = calendarsToSearch.Distinct().ToList();
        }
        else
        {
            calendarsToSearch.Add(calendarId);
        }

        var allEvents = new List<Event>();
        foreach (var calId in calendarsToSearch)
        {
            try
            {
                var request = service.Events.List(calId);
                // Fix: Start of today in UTC to avoid timezone issues
                var timeMin = DateTime.UtcNow.Date;
                request.TimeMinDateTimeOffset = timeMin > start ? timeMin : start;
                request.TimeMaxDateTimeOffset = end;
                request.SingleEvents = true;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                var result = await request.ExecuteAsync();
                var items = result.Items ?? new List<Event>();

                Console.WriteLine($"[GoogleCalendarService] Found {items.Count} events in calendar {calId}");
                allEvents.AddRange(items);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleCalendarService] Error fetching events from {calId}: {ex.Message}");
            }
        }

        return allEvents.OrderBy(e => e.Start.DateTimeDateTimeOffset?.DateTime ?? (DateTime.TryParse(e.Start.Date, out var dt) ? dt : DateTime.MinValue));
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

    public async Task<Event> CreateAllDayEventAsync(int userId, string calendarId, string title, DateTime start, DateTime end)
    {
        var service = await GetCalendarServiceAsync(userId);
        var ev = new Event
        {
            Summary = title,
            Start = new EventDateTime { Date = start.ToString("yyyy-MM-dd") },
            End = new EventDateTime { Date = end.ToString("yyyy-MM-dd") }
        };
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

    public string GetAuthUrl(string redirectUri)
    {
        var clientId = _configuration["Google:ClientId"];
        var initializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = _configuration["Google:ClientSecret"] },
            Scopes = new[] { CalendarService.Scope.Calendar, CalendarService.Scope.CalendarEvents }
        };
        var flow = new GoogleAuthorizationCodeFlow(initializer);
        var request = flow.CreateAuthorizationCodeRequest(redirectUri);
        var url = request.Build().ToString();
        url += "&access_type=offline&prompt=consent";
        return url;
    }

    public async Task ExchangeCodeForTokenAsync(int userId, string code, string redirectUri)
    {
        var clientId = _configuration["Google:ClientId"];
        var clientSecret = _configuration["Google:ClientSecret"];
        var initializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret }
        };
        var flow = new GoogleAuthorizationCodeFlow(initializer);
        var token = await flow.ExchangeCodeForTokenAsync(userId.ToString(), code, redirectUri, System.Threading.CancellationToken.None);

        var cred = await _context.GoogleCredentials.FirstOrDefaultAsync(g => g.UserId == userId);
        if (cred == null)
        {
            cred = new global::Models.GoogleCredential { UserId = userId };
            _context.GoogleCredentials.Add(cred);
        }

        cred.AccessToken = token.AccessToken;
        cred.RefreshToken = token.RefreshToken ?? cred.RefreshToken;
        cred.Expiry = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds ?? 3600);

        var service = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = new UserCredential(flow, userId.ToString(), token),
            ApplicationName = "Il Punto G"
        });
        var calendar = await service.Calendars.Get("primary").ExecuteAsync();
        cred.CalendarEmail = calendar.Id;

        await _context.SaveChangesAsync();
    }

    public async Task<List<string>> GetUserBranchCalendarsAsync(int userId)
    {
        var branchIds = await _context.UserBranches
            .Where(ub => ub.UserId == userId)
            .Include(ub => ub.Branch)
            .Select(ub => ub.Branch!.GoogleCalendarId)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToListAsync();

        var userCred = await _context.GoogleCredentials.FirstOrDefaultAsync(c => c.UserId == userId);
        if (userCred != null && !string.IsNullOrEmpty(userCred.CalendarEmail))
        {
            branchIds.Add("primary");

            try
            {
                var service = await GetCalendarServiceAsync(userId);
                var calendarList = await service.CalendarList.List().ExecuteAsync();
                var irene = calendarList.Items.FirstOrDefault(c => c.Summary.Contains("Irene Graldev", StringComparison.OrdinalIgnoreCase));
                if (irene != null) branchIds.Add(irene.Id);
            }
            catch {}
        }

        return branchIds.Where(id => id != null).Select(id => id!).Distinct().ToList();
    }

    public async Task<List<(DateTime Start, DateTime End)>> GetFreeSlotsAsync(int userId, DateTime start, DateTime end)
    {
        var service = await GetCalendarServiceAsync(userId);
        var calendarIds = await GetUserBranchCalendarsAsync(userId);

        if (!calendarIds.Any()) return new List<(DateTime Start, DateTime End)> { (start, end) };

        var request = new FreeBusyRequest
        {
            TimeMinDateTimeOffset = start,
            TimeMaxDateTimeOffset = end,
            Items = calendarIds.Select(id => new FreeBusyRequestItem { Id = id }).ToList()
        };

        var response = await service.Freebusy.Query(request).ExecuteAsync();

        var busyPeriods = response.Calendars.Values
            .SelectMany(c => c.Busy)
            .Select(b => (Start: b.StartDateTimeOffset?.DateTime ?? DateTime.MinValue, End: b.EndDateTimeOffset?.DateTime ?? DateTime.MinValue))
            .OrderBy(b => b.Start)
            .ToList();

        // Include non-working hours as "busy"
        var nonWorkingBusy = new List<(DateTime Start, DateTime End)>();
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            // Weekend: all day busy
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                nonWorkingBusy.Add((date, date.AddDays(1)));
            }
            else
            {
                // Working day: busy before 9 and after 18
                nonWorkingBusy.Add((date, date.AddHours(9)));
                nonWorkingBusy.Add((date.AddHours(18), date.AddDays(1)));
            }
        }

        var allBusy = busyPeriods.Concat(nonWorkingBusy).OrderBy(b => b.Start).ToList();

        var mergedBusy = new List<(DateTime Start, DateTime End)>();
        if (allBusy.Any())
        {
            var current = (allBusy[0].Start, allBusy[0].End);
            for (int i = 1; i < allBusy.Count; i++)
            {
                if (allBusy[i].Start <= current.End)
                {
                    if (allBusy[i].End > current.End)
                        current.End = allBusy[i].End;
                }
                else
                {
                    mergedBusy.Add(current);
                    current = (allBusy[i].Start, allBusy[i].End);
                }
            }
            mergedBusy.Add(current);
        }

        var freeSlots = new List<(DateTime Start, DateTime End)>();
        DateTime currentStart = start;

        foreach (var busy in mergedBusy)
        {
            if (busy.Start > currentStart)
            {
                freeSlots.Add((currentStart, busy.Start));
            }
            if (busy.End > currentStart)
                currentStart = busy.End;
        }

        if (currentStart < end)
        {
            freeSlots.Add((currentStart, end));
        }

        // Final filter: ensure slots are at least 30 mins and within range
        return freeSlots
            .Where(s => (s.End - s.Start).TotalMinutes >= 30)
            .Where(s => s.Start >= start && s.End <= end)
            .ToList();
    }

    public async Task AllocateTasksAsync(int userId)
    {
        var tasks = await _context.TodoTasks
            .Include(t => t.Priority)
            .Where(t => t.Status != TodoStatus.Completed && t.SyncStatus == SyncStatus.NotSynced)
            .OrderByDescending(t => t.Priority!.LevelId)
            .ThenBy(t => t.Deadline)
            .ToListAsync();

        if (!tasks.Any()) return;

        var start = DateTime.Now;
        var end = DateTime.Now.AddDays(14);
        var freeSlots = await GetFreeSlotsAsync(userId, start, end);

        foreach (var task in tasks)
        {
            double durationHours = task.Priority?.Name switch
            {
                "Urgente" => 8,
                "Media" => 4,
                "Bassa" => 2,
                _ => 1
            };

            var limitDate = task.Deadline.AddDays(7);
            var suitableSlotIndex = freeSlots.FindIndex(s =>
                (s.End - s.Start).TotalHours >= durationHours &&
                s.Start < limitDate &&
                s.Start >= DateTime.Now);

            if (suitableSlotIndex != -1)
            {
                var suitableSlot = freeSlots[suitableSlotIndex];
                var ev = new Event
                {
                    Summary = $"[Focus] {task.Title}",
                    Description = task.Description,
                    Start = new EventDateTime { DateTimeDateTimeOffset = suitableSlot.Start },
                    End = new EventDateTime { DateTimeDateTimeOffset = suitableSlot.Start.AddHours(durationHours) }
                };

                var created = await CreateEventAsync(userId, "primary", ev);

                task.GoogleEventId = created.Id;
                task.GoogleCalendarId = "primary";
                task.SyncStatus = SyncStatus.Synced;

                var newStart = suitableSlot.Start.AddHours(durationHours);
                if (newStart < suitableSlot.End)
                {
                    freeSlots[suitableSlotIndex] = (newStart, suitableSlot.End);
                }
                else
                {
                    freeSlots.RemoveAt(suitableSlotIndex);
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<double> GetTeamCapacityAsync(int userId)
    {
        var today = DateTime.Today;
        var start = today;
        var end = today.AddDays(1);
        var service = await GetCalendarServiceAsync(userId);
        var calendarIds = await GetUserBranchCalendarsAsync(userId);

        if (!calendarIds.Any()) return 0;

        var request = new FreeBusyRequest
        {
            TimeMinDateTimeOffset = start,
            TimeMaxDateTimeOffset = end,
            Items = calendarIds.Select(id => new FreeBusyRequestItem { Id = id }).ToList()
        };

        var response = await service.Freebusy.Query(request).ExecuteAsync();
        var busyTotalHours = response.Calendars.Values
            .SelectMany(c => c.Busy)
            .Sum(b => (b.EndDateTimeOffset - b.StartDateTimeOffset)?.TotalHours ?? 0);

        // Subtract vacations
        var vacations = await _context.Vacations
            .Where(v => v.StartDate <= today && v.EndDate >= today)
            .ToListAsync();

        // Each vacation takes 8 hours from total capacity
        // Total available hours = 8h * number of team members
        var teamCount = await _context.UserBranches
            .Where(ub => calendarIds.Contains(ub.Branch!.GoogleCalendarId ?? ""))
            .Select(ub => ub.UserId)
            .Distinct()
            .CountAsync();

        if (teamCount == 0) teamCount = 1; // Fallback

        double totalAvailableHours = 8.0 * teamCount;
        double vacationHours = vacations.Count * 8.0;

        double effectiveBusyHours = busyTotalHours + vacationHours;
        double capacity = Math.Min(effectiveBusyHours / totalAvailableHours, 1.0);

        return capacity;
    }

    public async Task SyncTaskShortenedAsync(int userId, string calendarId, string eventId, double newDurationHours)
    {
        var task = await _context.TodoTasks.FirstOrDefaultAsync(t => t.GoogleEventId == eventId);
        if (task == null) return;

        if (newDurationHours <= 0.5)
        {
            task.Status = TodoStatus.Completed;

            var service = await GetCalendarServiceAsync(userId);
            var ev = await service.Events.Get(calendarId, eventId).ExecuteAsync();
            if (!ev.Summary.StartsWith("✅"))
            {
                ev.Summary = "✅ " + ev.Summary;
                await service.Events.Update(ev, calendarId, eventId).ExecuteAsync();
            }
        }

        await _context.SaveChangesAsync();
    }
}
