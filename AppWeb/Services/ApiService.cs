using System.Net.Http.Json;
using System.Net.Http.Headers;
using Models;
namespace AppWeb.Services;
public class ApiService {
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetBaseUrl() => _httpClient.BaseAddress?.ToString() ?? "";

    public async Task<string?> LoginAsync(string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/account/login", new { username, password });
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return result?.Token;
        }
        return null;
    }

    public async Task<bool> RegisterAsync(string name, string email, string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/account/register", new { name, email, username, password });
        return response.IsSuccessStatusCode;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    public async Task<List<Branch>> GetBranchesAsync() => await _httpClient.GetFromJsonAsync<List<Branch>>("api/branches") ?? new();
    public async Task<Branch?> GetBranchAsync(int id) => await _httpClient.GetFromJsonAsync<Branch>($"api/branches/{id}");
    public async Task CreateBranchAsync(Branch branch) => await _httpClient.PostAsJsonAsync("api/branches", branch);
    public async Task UpdateBranchAsync(int id, Branch branch) => await _httpClient.PutAsJsonAsync($"api/branches/{id}", branch);
    public async Task DeleteBranchAsync(int id) => await _httpClient.DeleteAsync($"api/branches/{id}");
    public async Task<List<Project>> GetProjectsAsync(int? branchId = null) => await _httpClient.GetFromJsonAsync<List<Project>>("api/projects" + (branchId.HasValue ? "?branchId=" + branchId : "")) ?? new();
    public async Task<Project?> GetProjectAsync(int id) => await _httpClient.GetFromJsonAsync<Project>($"api/projects/{id}");
    public async Task CreateProjectAsync(Project project) => await _httpClient.PostAsJsonAsync("api/projects", project);
    public async Task UpdateProjectAsync(int id, Project project) => await _httpClient.PutAsJsonAsync($"api/projects/{id}", project);
    public async Task DeleteProjectAsync(int id) => await _httpClient.DeleteAsync($"api/projects/{id}");
    public async Task<List<Appointment>> GetAppointmentsAsync(int? branchId = null) => await _httpClient.GetFromJsonAsync<List<Appointment>>("api/appointments" + (branchId.HasValue ? "?branchId=" + branchId : "")) ?? new();
    public async Task<Appointment?> GetAppointmentAsync(int id) => await _httpClient.GetFromJsonAsync<Appointment>($"api/appointments/{id}");
    public async Task CreateAppointmentAsync(Appointment appt) => await _httpClient.PostAsJsonAsync("api/appointments", appt);
    public async Task DeleteAppointmentAsync(int id) => await _httpClient.DeleteAsync($"api/appointments/{id}");
    public async Task<List<TodoTask>> GetTasksAsync(int? projectId = null) => await _httpClient.GetFromJsonAsync<List<TodoTask>>("api/todotasks" + (projectId.HasValue ? "?projectId=" + projectId : "")) ?? new();
    public async Task<TodoTask?> GetTaskAsync(int id) => await _httpClient.GetFromJsonAsync<TodoTask>($"api/todotasks/{id}");
    public async Task CreateTaskAsync(TodoTask task, List<int>? selectedUserIds = null, bool assignToAll = false)
        => await _httpClient.PostAsJsonAsync("api/todotasks", new { task, selectedUserIds, assignToAllInBranch = assignToAll });
    public async Task UpdateTaskAsync(int id, TodoTask task) => await _httpClient.PutAsJsonAsync($"api/todotasks/{id}", task);
    public async Task DeleteTaskAsync(int id) => await _httpClient.DeleteAsync($"api/todotasks/{id}");

    public async Task<List<object>> GetCalendarEventsAsync(int? userId = null, int? branchId = null, int? projectId = null, int? clientId = null)
    {
        var url = "api/calendar/events?";
        if (userId.HasValue) url += $"userId={userId}&";
        if (branchId.HasValue) url += $"branchId={branchId}&";
        if (projectId.HasValue) url += $"projectId={projectId}&";
        if (clientId.HasValue) url += $"clientId={clientId}&";
        return await _httpClient.GetFromJsonAsync<List<object>>(url) ?? new();
    }
    public async Task UpdateCalendarEventAsync(string type, int dbId, DateTime newDate, TodoStatus? status = null, TimeSpan? duration = null)
    {
        await _httpClient.PostAsJsonAsync("api/calendar/update-event", new { type, dbId, newDate, status, duration });
    }

    public async Task<List<Client>> GetClientsAsync(int? branchId = null) => await _httpClient.GetFromJsonAsync<List<Client>>("api/clients" + (branchId.HasValue ? "?branchId=" + branchId : "")) ?? new();
    public async Task CreateClientAsync(Client client) => await _httpClient.PostAsJsonAsync("api/clients", client);
    public async Task<List<TaskType>> GetTaskTypesAsync() => await _httpClient.GetFromJsonAsync<List<TaskType>>("api/tasktypes") ?? new();
    public async Task<List<Priority>> GetPrioritiesAsync() => await _httpClient.GetFromJsonAsync<List<Priority>>("api/priorities") ?? new();
    public async Task<List<User>> GetUsersAsync() => await _httpClient.GetFromJsonAsync<List<User>>("api/users") ?? new();

    public async Task<List<WorkLog>> GetWorkLogsAsync(int? userId = null, int? month = null, int? year = null)
    {
        var url = "api/worklogs?";
        if (userId.HasValue) url += $"userId={userId}&";
        if (month.HasValue) url += $"month={month}&";
        if (year.HasValue) url += $"year={year}&";
        return await _httpClient.GetFromJsonAsync<List<WorkLog>>(url) ?? new();
    }

    public async Task<WorkLog?> CreateWorkLogAsync(WorkLog log)
    {
        var response = await _httpClient.PostAsJsonAsync("api/worklogs", log);
        if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<WorkLog>();
        return null;
    }
    public async Task UpdateWorkLogAsync(int id, WorkLog log) => await _httpClient.PutAsJsonAsync($"api/worklogs/{id}", log);
    public async Task DeleteWorkLogAsync(int id) => await _httpClient.DeleteAsync($"api/worklogs/{id}");

    public async Task<List<dynamic>> GetWorkLogReportAsync(int month, int year)
    {
        return await _httpClient.GetFromJsonAsync<List<dynamic>>($"api/worklogs/report?month={month}&year={year}") ?? new();
    }

    public async Task<DashboardStats?> GetDashboardStatsAsync()
    {
        return await _httpClient.GetFromJsonAsync<DashboardStats>("api/dashboard/stats");
    }
    public async Task<List<TodoTask>> GetKanbanTasksAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<TodoTask>>("api/dashboard/kanban") ?? new();
    }
    public async Task<List<BranchSummary>> GetBranchesSummaryAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<BranchSummary>>("api/dashboard/branches-summary") ?? new();
    }

    public async Task<dynamic> GetGoogleCalendarStatusAsync() => await _httpClient.GetFromJsonAsync<dynamic>("api/googlecalendar/status") ?? new { };
    public async Task<dynamic> ConnectGoogleCalendarAsync() => await (await _httpClient.PostAsync("api/googlecalendar/connect", null)).Content.ReadFromJsonAsync<dynamic>() ?? new { };
    public async Task<dynamic> SyncGoogleCalendarAsync() => await (await _httpClient.PostAsync("api/googlecalendar/sync", null)).Content.ReadFromJsonAsync<dynamic>() ?? new { };
    public async Task<dynamic> DisconnectGoogleCalendarAsync() => await (await _httpClient.PostAsync("api/googlecalendar/disconnect", null)).Content.ReadFromJsonAsync<dynamic>() ?? new { };

    public async Task<List<AuditLog>> GetAuditLogsAsync() => await _httpClient.GetFromJsonAsync<List<AuditLog>>("api/auditlog") ?? new();
    public async Task CreateAuditLogAsync(AuditLog log) => await _httpClient.PostAsJsonAsync("api/auditlog", log);

    public async Task UploadDocumentAsync(MultipartFormDataContent content)
    {
        await _httpClient.PostAsync("api/documents/upload", content);
    }
    public async Task<List<Document>> GetDocumentsAsync() => await _httpClient.GetFromJsonAsync<List<Document>>("api/documents") ?? new();

    public async Task<dynamic> ForgotPasswordAsync(string email)
    {
        var response = await _httpClient.PostAsJsonAsync("api/account/forgot-password", new { email });
        return await response.Content.ReadFromJsonAsync<dynamic>() ?? new { };
    }

    public async Task<bool> ResetPasswordAsync(string username, string token, string newPassword)
    {
        var response = await _httpClient.PostAsJsonAsync("api/account/reset-password", new { username, token, newPassword });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
    {
        var response = await _httpClient.PostAsJsonAsync("api/users/change-password", new { oldPassword, newPassword });
        return response.IsSuccessStatusCode;
    }
}
