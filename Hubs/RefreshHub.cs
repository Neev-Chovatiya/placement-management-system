using Microsoft.AspNetCore.SignalR;

namespace pms.Hubs
{
    public class RefreshHub : Hub
    {
        public async Task SendRefresh()
        {
            await Clients.All.SendAsync("ReceiveRefresh");
        }
    }
}
