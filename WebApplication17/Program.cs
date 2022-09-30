using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using WebApplication17;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o =>
{
    o.ConfigureEndpointDefaults(o2 =>
    {
        o2.UseConnectionLogging();
    });
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddSingleton<ClientService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseWebSockets();
app.Use((c, n) =>
{
    var feature = c.Features.Get<IHttpWebSocketFeature>();
    if (feature is not null)
    {
        c.Features.Set<IHttpWebSocketFeature>(new WebSocketWrapper(feature));
    }
    return n(c);
});

app.MapHub<FileHub>("/hub");

app.MapHub<GameHub>("/game");

app.MapRazorPages();

app.MapGet("/send", async ([FromQuery] string id, IHubContext<FileHub> hubContext) =>
{
    var fileChunks = GetFile();
    foreach (var chunk in fileChunks)
    {
        using var tcs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _ = await hubContext.Clients.Client(id).InvokeAsync<bool>("SendFile2", chunk, tcs.Token);
    }

    await hubContext.Clients.Client(id).SendAsync("FileDone");
});

app.Run();

List<byte[]> GetFile()
{
    var chunks = new List<byte[]>();
    for (var i = 0; i < 3; i++)
    {
        var bytes = new byte[100];
        Random.Shared.NextBytes(bytes);
        chunks.Add(bytes);
    }
    return chunks;
}

public class ClientService
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _clients = new();
    public void AddClient(string id)
    {
        _clients.TryAdd(id, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
    }

    public async Task WaitForReceive(string id)
    {
        if (_clients.TryGetValue(id, out var tcs))
        {
            await tcs.Task;
            _clients.TryUpdate(id, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously), tcs);
        }
    }

    public void SetReceived(string id)
    {
        if (_clients.TryGetValue(id, out var tcs))
        {
            tcs.TrySetResult();
        }
    }
}

internal class WebSocketWrapper : IHttpWebSocketFeature
{
    private readonly IHttpWebSocketFeature _innerFeature;

    public WebSocketWrapper(IHttpWebSocketFeature innerFeature)
    {
        _innerFeature = innerFeature;
    }

    public bool IsWebSocketRequest => _innerFeature.IsWebSocketRequest;

    public async Task<WebSocket> AcceptAsync(WebSocketAcceptContext context)
    {
        context.DangerousEnableCompression = true;
        var ws = await _innerFeature.AcceptAsync(context);
        return ws;
    }
}