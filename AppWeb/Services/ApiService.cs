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

        var token = _httpContextAccessor.HttpContext?.Request.Cookies["JwtToken"];
        if (!string.IsNullOrEmpty(token))
        {
            SetToken(token);
        }
    }

    public void SetToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

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
    public async Task CreateTaskAsync(TodoTask task) => await _httpClient.PostAsJsonAsync("api/todotasks", task);
    public async Task UpdateTaskAsync(int id, TodoTask task) => await _httpClient.PutAsJsonAsync($"api/todotasks/{id}", task);
    public async Task DeleteTaskAsync(int id) => await _httpClient.DeleteAsync($"api/todotasks/{id}");

    public async Task<List<object>> GetCalendarEventsAsync() => await _httpClient.GetFromJsonAsync<List<object>>("api/calendar/events") ?? new();
    public async Task UpdateCalendarEventAsync(string type, int dbId, DateTime newDate)
    {
        await _httpClient.PostAsJsonAsync("api/calendar/update-event", new { type, dbId, newDate });
    }
}
