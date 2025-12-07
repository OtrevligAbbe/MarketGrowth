using Microsoft.JSInterop;

namespace frontend.Shared
{
    public class AuthState
    {
        private const string StorageKey = "mg-user-id";

        private readonly IJSRuntime _js;

        public AuthState(IJSRuntime js)
        {
            _js = js;
        }

        public string? UserId { get; private set; }

        public bool IsLoggedIn => !string.IsNullOrWhiteSpace(UserId);

        public async Task InitializeAsync()
        {
            UserId = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        }

        public async Task LoginAsync(string userId)
        {
            UserId = userId;
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, userId);
        }

        public async Task LogoutAsync()
        {
            UserId = null;
            await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
    }
}
