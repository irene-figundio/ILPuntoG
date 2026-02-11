using System.Net.Http.Json;
using Models;
namespace AppWeb.Services;
public class ApiService {
    private readonly HttpClient _httpClient;
    public ApiService(HttpClient httpClient) { _httpClient = httpClient; }
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
}
