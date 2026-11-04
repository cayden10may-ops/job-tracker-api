using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JobTracker.Api.Dtos;

namespace JobTracker.Tests;

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // ---------- helpers ----------

    private static async Task<T> Read<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>())!;

    // Every test registers its own user, so tests never affect each other
    private async Task<HttpClient> NewUserClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"user{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Password123" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var auth = await Read<AuthResponse>(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    private static async Task<int> CreateCompanyAsync(HttpClient client, string name = "Acme Software")
    {
        var response = await client.PostAsJsonAsync("/api/companies", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await Read<CompanyResponse>(response)).Id;
    }

    private static async Task<ApplicationResponse> CreateApplicationAsync(
        HttpClient client, int companyId, string role = "Junior Developer")
    {
        var response = await client.PostAsJsonAsync("/api/applications",
            new { roleTitle = role, companyId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await Read<ApplicationResponse>(response);
    }

    private static Task<HttpResponseMessage> SetStatusAsync(HttpClient client, int id, string status)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/applications/{id}/status")
        {
            Content = JsonContent.Create(new { status })
        };
        return client.SendAsync(request);
    }

    // ---------- authentication ----------

    [Fact]
    public async Task Register_returns_a_token()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = $"a{Guid.NewGuid():N}@example.com", password = "Password123" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await Read<AuthResponse>(response);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
    }

    [Fact]
    public async Task Register_with_a_duplicate_email_returns_409()
    {
        var client = factory.CreateClient();
        var body = new { email = $"dup{Guid.NewGuid():N}@example.com", password = "Password123" };

        await client.PostAsJsonAsync("/api/auth/register", body);
        var second = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_with_a_short_password_returns_400()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = $"short{Guid.NewGuid():N}@example.com", password = "123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_returns_401()
    {
        var client = factory.CreateClient();
        var email = $"login{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Password123" });

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "WrongPassword" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoints_return_401_without_a_token()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/companies");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- applications and rules ----------

    [Fact]
    public async Task Creating_an_application_for_a_missing_company_returns_400()
    {
        var client = await NewUserClientAsync();
        var response = await client.PostAsJsonAsync("/api/applications",
            new { roleTitle = "Developer", companyId = 9999 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Salary_max_below_salary_min_returns_400()
    {
        var client = await NewUserClientAsync();
        var companyId = await CreateCompanyAsync(client);

        var response = await client.PostAsJsonAsync("/api/applications",
            new { roleTitle = "Developer", companyId, salaryMin = 50000, salaryMax = 40000 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Skipping_the_interview_stage_returns_409()
    {
        var client = await NewUserClientAsync();
        var application = await CreateApplicationAsync(client, await CreateCompanyAsync(client));

        var response = await SetStatusAsync(client, application.Id, "Offer");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task A_rejected_application_cannot_be_reopened()
    {
        var client = await NewUserClientAsync();
        var application = await CreateApplicationAsync(client, await CreateCompanyAsync(client));

        Assert.Equal(HttpStatusCode.OK, (await SetStatusAsync(client, application.Id, "Rejected")).StatusCode);
        var reopen = await SetStatusAsync(client, application.Id, "Interview");

        Assert.Equal(HttpStatusCode.Conflict, reopen.StatusCode);
    }

    [Fact]
    public async Task Status_changes_are_recorded_in_the_history_and_stats()
    {
        var client = await NewUserClientAsync();
        var application = await CreateApplicationAsync(client, await CreateCompanyAsync(client));

        await SetStatusAsync(client, application.Id, "Interview");
        await SetStatusAsync(client, application.Id, "Rejected");

        var history = await Read<List<StatusChangeResponse>>(
            await client.GetAsync($"/api/applications/{application.Id}/history"));
        Assert.Equal(2, history.Count);

        var stats = await Read<StatsResponse>(await client.GetAsync("/api/stats"));
        Assert.Equal(1, stats.Total);
        Assert.Equal(1, stats.ReachedInterview); // still counts, even though it was rejected later
        Assert.Equal(1, stats.ByStatus["Rejected"]);
    }

    [Fact]
    public async Task Filtering_searching_and_paging_work_together()
    {
        var client = await NewUserClientAsync();
        var companyId = await CreateCompanyAsync(client);

        var backend = await CreateApplicationAsync(client, companyId, "Backend Developer");
        await CreateApplicationAsync(client, companyId, "Frontend Developer");
        await CreateApplicationAsync(client, companyId, "QA Tester");
        await SetStatusAsync(client, backend.Id, "Interview");

        var interviews = await Read<PagedResult<ApplicationResponse>>(
            await client.GetAsync("/api/applications?status=Interview"));
        Assert.Equal(1, interviews.TotalCount);

        var developers = await Read<PagedResult<ApplicationResponse>>(
            await client.GetAsync("/api/applications?search=developer"));
        Assert.Equal(2, developers.TotalCount);

        var firstPage = await Read<PagedResult<ApplicationResponse>>(
            await client.GetAsync("/api/applications?pageSize=2"));
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
    }

    [Fact]
    public async Task A_company_with_applications_cannot_be_deleted()
    {
        var client = await NewUserClientAsync();
        var companyId = await CreateCompanyAsync(client);
        await CreateApplicationAsync(client, companyId);

        var response = await client.DeleteAsync($"/api/companies/{companyId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ---------- data isolation ----------

    [Fact]
    public async Task Users_cannot_see_or_change_each_others_data()
    {
        var alice = await NewUserClientAsync();
        var bob = await NewUserClientAsync();

        var companyId = await CreateCompanyAsync(alice);
        var application = await CreateApplicationAsync(alice, companyId);

        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/applications/{application.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SetStatusAsync(bob, application.Id, "Interview")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/applications/{application.Id}")).StatusCode);

        var bobsList = await Read<PagedResult<ApplicationResponse>>(await bob.GetAsync("/api/applications"));
        Assert.Equal(0, bobsList.TotalCount);

        var bobUsesAlicesCompany = await bob.PostAsJsonAsync("/api/applications",
            new { roleTitle = "Sneaky", companyId });
        Assert.Equal(HttpStatusCode.BadRequest, bobUsesAlicesCompany.StatusCode);
    }
}