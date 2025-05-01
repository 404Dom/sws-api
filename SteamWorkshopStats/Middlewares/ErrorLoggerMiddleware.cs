using NLog;
using SteamWorkshopStats.Exceptions;
using SteamWorkshopStats.Services;
using SteamWorkshopStats.Utils;

namespace SteamWorkshopStats.Middlewares;

public class ErrorLoggerMiddleware
{
	private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

	private readonly RequestDelegate _next;

	private readonly IDiscordService _discordService;

	public ErrorLoggerMiddleware(RequestDelegate next, IDiscordService discordService)
	{
		_next = next;
		_discordService = discordService;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await _next(context);
		}
		catch (Exception ex)
		{
			_logger.Error(ex, "Unhandled exception:");
			_ = _discordService.LogErrorAsync(context.Request.Path, IpUtils.GetIp(context), ex.ToString());

			context.Response.StatusCode = StatusCodes.Status500InternalServerError;

			await context.Response.WriteAsJsonAsync(
				new { Message = ex is SteamServiceException ? ex.Message : "HTTP 500 Internal Server Error" }
			);
		}
	}
}
