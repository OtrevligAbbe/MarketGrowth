using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MarketGrowth.Frontend.Shared;

namespace frontend.Shared
{

    // Enkel auth-/state-service för frontenden.
    // Håller koll på inloggad användare + favoriter i headern/dropdown.
    public class AuthState
    {
        private readonly HttpClient _http;

        public AuthState(HttpClient http)
        {
            _http = http;
        }

        // Är någon inloggad?
        public bool IsLoggedIn { get; private set; }

        // UserId vi använder mot API:et
        public string UserId { get; private set; } = string.Empty;

        // Favoriter som visas i header/dropdown
        public List<FavoriteAssetRequest> Favorites { get; private set; } = new();

        public int FavoritesCount => Favorites.Count;

        // Event så att komponenter kan lyssna (FavoritesDropdown, navbar osv.)
        public event Action? OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();


        // Körs från Authentication.razor på OnInitializedAsync().
        // Just nu: om vi redan är inloggade så laddar vi bara om favoriterna.

        public async Task InitializeAsync()
        {
            if (IsLoggedIn && !string.IsNullOrWhiteSpace(UserId))
            {
                await ReloadFavoritesAsync();
            }
        }

        // Anropas när du loggar in (Authentication.razor).
        public async Task LoginAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            IsLoggedIn = true;
            UserId = userId;

            await ReloadFavoritesAsync();
            NotifyStateChanged();
        }

        // Synkron logout-metod (om du vill använda den direkt).
        public void Logout()
        {
            IsLoggedIn = false;
            UserId = string.Empty;
            Favorites.Clear();
            NotifyStateChanged();
        }


        // Async-variant som dina Razor-komponenter anropar: await Auth.LogoutAsync().

        public Task LogoutAsync()
        {
            Logout();
            return Task.CompletedTask;
        }


        // Hämtar favoriter från backend för nuvarande UserId och
        // uppdaterar Favorites + FavoritesCount + notifierar lyssnare.
        public async Task ReloadFavoritesAsync()
        {
#if DEBUG
            var baseUrl = "http://localhost:7247";
#else
            var baseUrl = "https://marketgrowth-api-astenhoff-ajhuarfah0akf5gp.swedencentral-01.azurewebsites.net";
#endif
            // Om inte inloggad -> rensa favoriter
            if (!IsLoggedIn || string.IsNullOrWhiteSpace(UserId))
            {
                Favorites.Clear();
                NotifyStateChanged();
                return;
            }

            var url = $"{baseUrl}/api/favorites/{UserId}";

            var favorites = await _http.GetFromJsonAsync<List<FavoriteAssetRequest>>(url)
                           ?? new List<FavoriteAssetRequest>();

            Favorites = favorites;
            NotifyStateChanged();
        }
    }
}
