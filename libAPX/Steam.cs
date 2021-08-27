using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace libAPX
{
    public class Steam
    {
        private PayMod[] paymods = new PayMod[]{
            new PayMod("McLaren 650S GT3", "1097229192"),
            new PayMod("Bentley Continental GT3", "1097230662"),
            new PayMod("Callaway Corvette C7 GT3-R", "1097232188"),
            new PayMod("Mercedes AMG GT3", "1097232656"),
            new PayMod("Radical RXC Turbo GT3", "1097232726"),
            new PayMod("Formula E 2018 Car", "1214555461"),
            new PayMod("Hong Kong E-Prix Track", "1214556324"),
            new PayMod("KartSim Rental Kart", "1344542862"),
            new PayMod("KartSim Buckmore Park International Circuit", "1344546612"),
            new PayMod("KartSim Paul Fletcher International", "1344546766"),
            new PayMod("KartSim Glan Y Gors", "1344546801"),
            new PayMod("KartSim Glan Y Gors", "1411387394"),
            new PayMod("McLaren 650S GT3", "1097229192"),
            new PayMod("McLaren 650S GT3", "1097229192"),
            new PayMod("McLaren 650S GT3", "1097229192"),
            new PayMod("McLaren 650S GT3", "1097229192"),
        };
        public Steam()
        {
            SteamClient.Init(365960, true);
        }

        public async void triggerDownload(PublishedFileId id)
        {

            var result = await SteamInventory.GetAllItemsAsync();
            var q = await Steamworks.Ugc.Item.GetAsync(id);

            if (q.HasValue)
            {
                q.Value.Download(true);
            }
        }

        public async void getSubscriptions(Action<InventoryItem[]> callback, Action<List<Item>> callbackWorkshop)
        {
            var query = Steamworks.Ugc.Query.Items.WithType(UgcType.GameManagedItems).WhereUserSubscribed();

            int ingameIndex = 1;
            List<Item> allItems = new List<Item>();
            while (true)
            {
                var items = await query.GetPageAsync(ingameIndex);
                IEnumerable<Item> entries = items.Value.Entries;
                int count = items.Value.ResultCount;
                if (items.HasValue && count > 0)
                {
                    ingameIndex++;
                    allItems.AddRange(items.Value.Entries);
                }
                else
                {
                    break;
                }
            }



            var q = Steamworks.Ugc.Query.Items.WhereUserSubscribed().WithType(UgcType.Items);
            int pageIndex = 1;
            while (true)
            {
                var items = await q.GetPageAsync(pageIndex);
                int count = items.Value.ResultCount;
                if (items.HasValue && count > 0)
                {
                    pageIndex++;

                    allItems.AddRange(items.Value.Entries);
                } else
                {
                    break;
                }
            }

            callbackWorkshop(allItems);
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
