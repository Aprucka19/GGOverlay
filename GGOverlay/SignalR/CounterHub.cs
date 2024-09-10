using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace GGOverlay.SignalR
{
    public class CounterHub : Hub
    {
        public async Task UpdateCounter(int newValue)
        {
            await Clients.All.SendAsync("ReceiveCounterUpdate", newValue);
        }
    }
}
