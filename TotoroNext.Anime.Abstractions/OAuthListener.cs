using System.Collections.Specialized;
using System.Net;

namespace TotoroNext.Anime.Abstractions;

public class OAuthListener
{
    private readonly HttpListener _listener = new();
    private readonly Func<NameValueCollection, Task> _processQuery;
    
    public OAuthListener(int port, Func<NameValueCollection, Task> processQuery)
    {
        _processQuery = processQuery;
        _listener.Prefixes.Add($"http://localhost:{port}/callback/");
    }

    public void Start()
    {
        if (_listener.IsListening)
        {
            return;
        }
        
        _listener.Start();
        
        _ = Task.Run(async () =>
        {
            while (true)
            {
                HttpListenerContext context;

                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (HttpListenerException) when (!_listener.IsListening)
                {
                    break;
                }

                _ = Task.Run(() => HandleRequestAsync(context));
            }
        });
        
    }

    public void Stop()
    {
        if (!_listener.IsListening)
        {
            return;
        }
        
        _listener.Stop();
    }
    
    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            var query = context.Request.QueryString;
            
            await _processQuery(query);

            try
            {
                await context.Response.OutputStream.WriteAsync("Authentication Completed, you may close the window"u8.ToArray());
            }
            finally
            {
                context.Response.Close();
            }
        }
        catch (Exception ex)
        {
            try
            {
                context.Response.StatusCode = 500;
                await context.Response.OutputStream.WriteAsync("Internal proxy error"u8.ToArray());
                context.Response.Close();
            }
            catch
            {
                Console.WriteLine("Defensive: client may already have disconnected");
            }
        }
    }
}