@using frontend.Shared

    @code
{
    // ... (existing code for _data, _timestamp, etc.) ...

    // Use a string to show status to the user
    private string? _saveStatus;

// ... (existing code for CoinData class and OnInitializedAsync) ...

private async void SaveFavorite(string assetName)
{
    _saveStatus = $"Saving {assetName} as favorite...";

    try
    {
        var client = HttpClientFactory.CreateClient("MarketGrowth.Api");

        // 1. Create the request payload
        var payload = new FavoriteAssetRequest { Asset = assetName };

        // 2. Call the new SaveFavorite Function with the asset name
        // The HttpClientFactory client automatically includes the B2C token!
        var response = await client.PostAsJsonAsync("api/favorites", payload);

        if (response.IsSuccessStatusCode)
        {
            _saveStatus = $"Successfully saved {assetName}!";
        }
        else
        {
            // Read and display error message from the API
            var error = await response.Content.ReadAsStringAsync();
            _saveStatus = $"Error saving {assetName}: Status {response.StatusCode}. Details: {error}";
        }
    }
    catch (AccessTokenNotAvailableException exception)
    {
        // Should not happen if B2C works, but redirects to login if token expired.
        exception.Redirect();
    }
    catch (Exception ex)
    {
        _saveStatus = $"A critical error occurred: {ex.Message}";
    }

    // Re-render the UI to show the status
    StateHasChanged();
}
}