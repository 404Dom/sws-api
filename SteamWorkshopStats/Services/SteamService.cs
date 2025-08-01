using NLog;
using SteamWorkshopStats.Exceptions;
using SteamWorkshopStats.Models;
using SteamWorkshopStats.Models.Records;

namespace SteamWorkshopStats.Services;

public class SteamService : ISteamService
{
	
	private readonly IConfiguration _configuration;

	private readonly IHttpClientFactory _httpClientFactory;

	private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

	public SteamService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
	{
		_configuration = configuration;
		_httpClientFactory = httpClientFactory;
	}

	/// <summary>
	/// Retrieves the SteamID of the User, using the Vanity URL.
	/// </summary>
	/// <param name="profileId">The ProfileID from the URL of the User's profile</param>
	/// <returns>The SteamID of the User</returns>
	/// <exception cref="SteamServiceException"></exception>
	public async Task<string?> GetSteamIdAsync(string profileId)
	{
		HttpClient client = _httpClientFactory.CreateClient("SteamClient");

		HttpResponseMessage response = await client.GetAsync(
			$"ISteamUser/ResolveVanityURL/v1/?key={_configuration["SteamApiKey"]}&vanityurl={profileId}"
		);

		if (!response.IsSuccessStatusCode)
		{
			_logger.Error(response);

			throw new SteamServiceException("Steam API failed to fetch SteamID");
		}

		ResolveVanityUrl? responseData = await response.Content.ReadFromJsonAsync<ResolveVanityUrl>();

		if (responseData is null || responseData.Response.Success != 1)
		{
			_logger.Warn($"GetSteamIdAsync API Response {responseData?.Response.Success}");

			return null;
		}

		return responseData.Response.SteamId;
	}

	/// <summary>
	/// Retrieves the User's profile information using the SteamID.
	/// </summary>
	/// <param name="steamId">The SteamID of the User</param>
	/// <returns>The User's profile information, including the username and the profile image URL.</returns>
	/// <exception cref="SteamServiceException"></exception>
	public async Task<GetPlayerSummariesPlayer?> GetProfileInfoAsync(string steamId)
	{
		HttpClient client = _httpClientFactory.CreateClient("SteamClient");

		HttpResponseMessage response = await client.GetAsync(
			$"ISteamUser/GetPlayerSummaries/v2/?key={_configuration["SteamApiKey"]}&steamids={steamId}"
		);

		if (!response.IsSuccessStatusCode)
		{
			_logger.Error(response);

			throw new SteamServiceException("Steam API failed to fetch Profile Info");
		}

		GetPlayerSummaries? responseData = await response.Content.ReadFromJsonAsync<GetPlayerSummaries>();

		if (responseData is null || responseData.Response.Players.Count == 0)
			return null;

		return responseData.Response.Players.ElementAt(0);
	}

	/// <summary>
	/// Retrieves a list of Addons made by the User based on their SteamID.
	/// </summary>
	/// <param name="steamId">The SteamID of the User.</param>
	/// <returns>A list of Addons sorted from newest to oldest.</returns>
	/// <exception cref="SteamServiceException"></exception>
	public async Task<List<Addon>> GetAddonsAsync(string steamId)
	{
		HttpClient client = _httpClientFactory.CreateClient("SteamClient");

		HttpResponseMessage response = await client.GetAsync(
			$"IPublishedFileService/GetUserFiles/v1/?key={_configuration["SteamApiKey"]}&steamid={steamId}&numperpage=500&return_vote_data=true&return_reactions=true"
		);

		if (!response.IsSuccessStatusCode)
		{
			_logger.Error(response);

			throw new SteamServiceException("Steam API failed to fetch Addons");
		}

		GetUserFiles? responseData = await response.Content.ReadFromJsonAsync<GetUserFiles>();

		if (responseData is null || responseData.Response.PublishedFiles is null)
			return new List<Addon>();

		List<Addon> addons = new List<Addon>();

		foreach (PublishedFile addon in responseData.Response.PublishedFiles)
		{
			int likes = addon.Votes.Likes ?? 0;
			int dislikes = addon.Votes.Dislikes ?? 0;

			addons.Add(
				new Addon
				{
					Id = addon.Id,
					Title = addon.Title,
					ImageUrl = addon.ImageUrl,
					Views = addon.Views,
					Subscribers = addon.Subscribers,
					Favorites = addon.Favorites,
					Likes = likes,
					Dislikes = dislikes,
					Awards = addon.Awards,
					Stars = Addon.GetNumberOfStars(likes + dislikes, addon.Votes.Score),
				}
			);
		}

		addons.Sort();

		return addons;
	}
}
