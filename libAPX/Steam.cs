using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace libAPX
{
    public class Steam
    {
        public Steam()
        {
            SteamClient.Init(365960, true);
        }
        public async void getSubscriptions(Action<InventoryItem[]> callback)
        {
            
            var result = await SteamInventory.GetAllItemsAsync();
            callback(SteamInventory.Items);
        }

        public async void getServers(Action<ServerInfo> callback)
        {
            using (var list = new Steamworks.ServerList.Internet())
            {
                list.AppId = 365960;
                await list.RunQueryAsync();
                var items = list.Responsive;
                List<ServerInfo> result = new List<ServerInfo>();
                foreach(var server in items)
                {
                    callback(server);
                }
            }

        }
    }
}
