using static CF.Events.Shared.Constants;
using CF.Events.Shared.DTOs;

namespace CF.Events.Web.Services;

public class AuthService(IHttpClientFactory httpClientFactory)
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient(HttpClients.EventsApi);

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var httpClient = CreateClient();
            var response = await httpClient.PostAsJsonAsync("auth/register", request);
            if (response.IsSuccessStatusCode)
                return new AuthResponse { Success = true };

            var error = await response.Content.ReadFromJsonAsync<AuthResponse>();
            return error ?? new AuthResponse
            {
                Success = false,
                Error = "Registration failed."
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var httpClient = CreateClient();
            var response = await httpClient.PostAsJsonAsync("auth/login", request);
            if (!response.IsSuccessStatusCode)
                return new AuthResponse
                {
                    Success = false,
                    Error = "Invalid credentials."
                };

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            return result ?? new AuthResponse
            {
                Success = false,
                Error = "Login failed."
            };

        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<AuthResponse> ChangePasswordAsync(UpdatePasswordRequest request)
    {
        try
        {
            var httpClient = CreateClient();
            var response = await httpClient.PostAsJsonAsync("auth/change-password", request);
            if (response.IsSuccessStatusCode)
                return new AuthResponse { Success = true };

            var error = await response.Content.ReadFromJsonAsync<AuthResponse>();
            return error ?? new AuthResponse
            {
                Success = false,
                Error = "Change password failed."
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<AuthResponse> SetupAccountAsync(SetupAccountRequest request)
    {
        try
        {
            var httpClient = CreateClient();
            var response = await httpClient.PostAsJsonAsync("auth/setup-account", request);
            if (response.IsSuccessStatusCode)
                return new AuthResponse { Success = true };

            var error = await response.Content.ReadFromJsonAsync<AuthResponse>();
            return error ?? new AuthResponse
            {
                Success = false,
                Error = "Account setup failed."
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        try
        {
            var httpClient = CreateClient();
            return await httpClient.GetFromJsonAsync<List<UserDto>>("auth/users") ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var httpClient = CreateClient();
            await httpClient.PostAsync("auth/logout", null);
        }
        catch
        {
            // Best effort
        }
    }
}
