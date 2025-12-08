using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MarketGrowth.Frontend.Shared;

namespace frontend.Shared
{


    public class AuthState
    {
        private readonly HttpClient _http;

        public AuthState(HttpClient http)
        {
            _http = http;
        }


        public bool IsLoggedIn { get; private set; }

    
        public string UserId { get; private set; } = string.Empty;
    
        public List<FavoriteAssetRequest> Favorites { get; private set; } = new();

        public int FavoritesCount => Favorites.Count;

     
        public event Action? OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();




        public async Task InitializeAsync()
        {
            if (IsLoggedIn && !string.IsNullOrWhiteSpace(UserId))
            {
                await ReloadFavoritesAsync();
            }
        }

     
        public async Task LoginAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            IsLoggedIn = true;
            UserId = userId;

            await ReloadFavoritesAsync();
            NotifyStateChanged();
        }


        public void Logout()
        {
            IsLoggedIn = false;
            UserId = string.Empty;
            Favorites.Clear();
            NotifyStateChanged();
        }



        public Task LogoutAsync()
        {
            Logout();
            return Task.CompletedTask;
        }



        public async Task ReloadFavoritesAsync()
        {
#if DEBUG
            var baseUrl = "http://localhost:7247";
#else
            var baseUrl = "https://marketgrowth-api-astenhoff-ajhuarfah0akf5gp.swedencentral-01.azurewebsites.net";
#endif
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
