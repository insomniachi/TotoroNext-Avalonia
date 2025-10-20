using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace TotoroNext.Anime.Abstractions;

public class OAuthListener
{
    public const string Response = """
                                   <!DOCTYPE html>
                                   <html lang="en">
                                   <head>
                                     <meta charset="UTF-8">
                                     <title>Authentication Complete</title>
                                     <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600&display=swap" rel="stylesheet">
                                     <style>
                                       body {
                                         font-family: 'Inter', sans-serif;
                                         background: linear-gradient(135deg, #f0f4ff, #e5f8ff);
                                         color: #2d3748;
                                         display: flex;
                                         flex-direction: column;
                                         justify-content: center;
                                         align-items: center;
                                         height: 100vh;
                                         margin: 0;
                                         padding: 0 20px;
                                       }

                                       .container {
                                         background: white;
                                         border-radius: 12px;
                                         box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
                                         padding: 40px;
                                         max-width: 480px;
                                         width: 100%;
                                         text-align: center;
                                         animation: fadeIn 0.6s ease-in-out;
                                       }

                                       h1 {
                                         font-size: 2rem;
                                         margin-bottom: 16px;
                                         color: #38a169;
                                       }

                                       .message {
                                         font-size: 1.1rem;
                                         margin-top: 12px;
                                         color: #4a5568;
                                       }

                                       @keyframes fadeIn {
                                         from { opacity: 0; transform: translateY(20px); }
                                         to { opacity: 1; transform: translateY(0); }
                                       }
                                     </style>
                                   </head>
                                   <body>
                                     <div class="container">
                                       <h1>✅ Login Successful</h1>
                                       <div class="message">
                                         You can now close this window and return to the app.<br>Thanks for logging in!
                                       </div>
                                     </div>
                                   </body>
                                   </html>
                                   """;

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
                await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(Response));
            }
            finally
            {
                context.Response.Close();
            }
        }
        catch (Exception)
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