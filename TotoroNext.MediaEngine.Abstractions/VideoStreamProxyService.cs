using System.Net;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TotoroNext.MediaEngine.Abstractions;

public class VideoStreamProxyService(ILogger<VideoStreamProxyService> logger) : IHostedService
{
    public const int Port = 5678;
    private readonly HttpClient _httpClient = new();
    private readonly HttpListener _listener = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _listener.Prefixes.Add($"http://localhost:{Port}/video/");
        _listener.Start();
        logger.LogInformation($"Video proxy started on http://localhost:{Port}/video/");

        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context;

                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (HttpListenerException) when (!_listener.IsListening)
                {
                    logger.LogInformation("Listener stopped.");
                    break;
                }

                _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _listener.Stop();
        logger.LogInformation("Video proxy stopped.");
        return Task.CompletedTask;
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            var query = context.Request.QueryString;
            var videoUrl = query["url"];

            if (string.IsNullOrWhiteSpace(videoUrl))
            {
                context.Response.StatusCode = 400;
                await context.Response.OutputStream.WriteAsync("Missing video URL"u8.ToArray());
                context.Response.Close();
                return;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, videoUrl);
            foreach (var key in query.AllKeys!)
            {
                if (key is null or "url")
                {
                    continue;
                }

                var value = query[key];
                if (!string.IsNullOrEmpty(value))
                {
                    request.Headers.TryAddWithoutValidation(key, value);
                }
            }

            using var upstream = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!upstream.IsSuccessStatusCode)
            {
                context.Response.StatusCode = (int)upstream.StatusCode;
                await context.Response.OutputStream
                             .WriteAsync(Encoding.UTF8.GetBytes($"Upstream error: {(int)upstream.StatusCode}"));
                context.Response.Close();
                return;
            }

            context.Response.StatusCode = (int)upstream.StatusCode;
            context.Response.ContentType = upstream.Content.Headers.ContentType?.ToString();
            context.Response.Headers["Connection"] = "close";
            if (upstream.Content.Headers.ContentLength.HasValue)
            {
                context.Response.ContentLength64 = upstream.Content.Headers.ContentLength.Value;
                context.Response.SendChunked = false;
            }
            else
            {
                context.Response.SendChunked = true;
            }

            try
            {
                await using var upstreamStream = await upstream.Content.ReadAsStreamAsync();
                await upstreamStream.CopyToAsync(context.Response.OutputStream);
            }
            catch (IOException ioEx)
            {
                logger.LogWarning(ioEx, "Stream failed: {Message}", ioEx.Message);
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 64)
            {
                logger.LogWarning(ex, "Client disconnected during stream (network name no longer available).");
            }
            finally
            {
                context.Response.Close();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error streaming video proxy response.");
            try
            {
                context.Response.StatusCode = 500;
                await context.Response.OutputStream.WriteAsync("Internal proxy error"u8.ToArray());
                context.Response.Close();
            }
            catch
            {
                // Defensive: client may already have disconnected
            }
        }
    }
}