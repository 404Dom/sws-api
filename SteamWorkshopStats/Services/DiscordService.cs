﻿using System.Text;
using System.Text.Json;
using NLog;
using SteamWorkshopStats.Models;

namespace SteamWorkshopStats.Services;

public class DiscordService : IDiscordService
{
	private readonly IConfiguration _configuration;

	private readonly IHttpClientFactory _httpClientFactory;

	private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

	public DiscordService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
	{
		_configuration = configuration;
		_httpClientFactory = httpClientFactory;
	}

	/// <summary>
	/// Logs the path and the IP of the User into a Discord Channel
	/// </summary>
	/// <param name="path">Path where the User made a query</param>
	/// <param name="ip">IP of the User</param>
	/// <returns></returns>
	public async Task LogQueryAsync(string path, string ip)
	{
		HttpClient client = _httpClientFactory.CreateClient("DiscordClient");

		var payload = new
		{
			embeds = new[]
			{
				new
				{
					title = "New Query",
					color = 5814783,
					type = "rich",
					fields = new[]
					{
						new
						{
							name = "Path",
							value = path,
							inline = false,
						},
						new
						{
							name = "IP",
							value = ip,
							inline = true,
						},
					},
					timestamp = DateTime.UtcNow,
				},
			},
		};

		StringContent content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

		HttpResponseMessage response = await client.PostAsync(_configuration["DiscordLogQueryWebhook"], content);

		if (!response.IsSuccessStatusCode)
		{
			_logger.Error(response);
			_ = LogErrorAsync(path, ip, $"Discord Service LogQueryAsync failed: {response.StatusCode}");
		}
	}

	/// <summary>
	/// Logs the User's Stats into a Discord Channel
	/// </summary>
	/// <param name="user">The User whose data is going to be logged</param>
	/// <returns></returns>
	public async Task LogUserAsync(User user)
	{
		HttpClient client = _httpClientFactory.CreateClient("DiscordClient");

		var payload = new
		{
			embeds = new[]
			{
				new
				{
					title = $"{user.Username} ({user.SteamId})",
					color = 5814783,
					type = "rich",
					fields = new[]
					{
						new
						{
							name = "Views",
							value = user.Views,
							inline = true,
						},
						new
						{
							name = "Subscribers",
							value = user.Subscribers,
							inline = true,
						},
						new
						{
							name = "Favorites",
							value = user.Favorites,
							inline = true,
						},
						new
						{
							name = "Likes",
							value = user.Likes,
							inline = true,
						},
						new
						{
							name = "Dislikes",
							value = user.Dislikes,
							inline = true,
						},
      						new
	    					{
							name = "Awards",
       							value = user.Awards,
	      						inline = true,
   						},
					},
					thumbnail = new { url = user.ProfileImageUrl },
					timestamp = DateTime.UtcNow,
				},
			},
		};

		StringContent content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

		HttpResponseMessage response = await client.PostAsync(_configuration["DiscordLogUserWebhook"], content);

		if (!response.IsSuccessStatusCode)
		{
			_logger.Error(response);
			_ = LogErrorAsync("Unknown", "Unknown", $"Discord Service LogUserAsync failed: {response.StatusCode}");
		}
	}

	/// <summary>
	/// Logs an error into a Discord Channel
	/// </summary>
	/// <param name="path">Path of the query</param>
	/// <param name="ip">IP of the User</param>
	/// <param name="message">Error Message</param>
	/// <returns></returns>
	public async Task LogErrorAsync(string path, string ip, string message)
	{
		HttpClient client = _httpClientFactory.CreateClient("DiscordClient");

		var payload = new
		{
			embeds = new[]
			{
				new
				{
					title = "Error",
					color = 5814783,
					type = "rich",
					fields = new[]
					{
						new
						{
							name = "Path",
							value = path,
							inline = true,
						},
						new
						{
							name = "IP",
							value = ip,
							inline = true,
						},
						new
						{
							name = "Error Message",
							value = message,
							inline = false,
						},
					},
					timestamp = DateTime.UtcNow,
				},
			},
		};

		StringContent content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

		HttpResponseMessage response = await client.PostAsync(_configuration["DiscordLogErrorWebhook"], content);

		if (!response.IsSuccessStatusCode)
		{
			_logger.Error(response);

			throw new Exception("Discord Service LogErrorAsync failed");
		}
	}
}
