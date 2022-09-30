using Microsoft.AspNetCore.SignalR;

namespace WebApplication17;

public class FileHub : Hub
{
    private readonly ClientService _clientService;
    public FileHub(ClientService clientService)
    {
        _clientService = clientService;
    }
    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }
}
