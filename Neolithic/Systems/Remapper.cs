using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace TheNeolithicMod
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class Message
    {
        public string IR;
        public string UID;
        public string from;
        public string to;
    }

    class Remapper : ModSystem
    {
        ICoreServerAPI sapi;
        ICoreClientAPI capi;
        IClientNetworkChannel cChannel;
        IServerNetworkChannel sChannel;

        string nl = Environment.NewLine;

        public List<AssetLocation> Missing { get; set; } = new List<AssetLocation>();
        public List<AssetLocation> NotMissing { get; set; } = new List<AssetLocation>();
        Dictionary<AssetLocation, AssetLocation> MostLikely { get; set; } = new Dictionary<AssetLocation, AssetLocation>();

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            cChannel = capi.Network.RegisterChannel("remapperchannel")
                .RegisterMessageType(typeof(Message))
                .SetMessageHandler<Message>(a => 
                {
                    if (a.UID == capi.World.Player.PlayerUID)
                    {
                        capi.SendChatMessage("/" + a.IR + " remap " + a.from + " " + a.to + " force");
                    }
                });
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            sChannel =sapi.Network.RegisterChannel("remapperchannel")
                .RegisterMessageType(typeof(Message));

            sapi.RegisterCommand("exportmissing", "Exports Names Of Missing Collectables", "", (p, g, a) =>
            {
                ExportMissing(p, g);
            }, Privilege.controlserver);

            sapi.RegisterCommand("tryremap", "Try To Remap Missing Collectables Using Levenshtein Distance", "", (p, g, a) =>
            {
                TryRemapMissing(p);
            }, Privilege.controlserver);
        }

        public void RePopulate()
        {
            Missing.Clear();
            for (int i = 0; i < sapi.World.Blocks.Length; i++)
            {
                if (sapi.World.Blocks[i].IsMissing)
                {
                    Missing.Add(sapi.World.Blocks[i].Code);
                }
                else
                {
                    NotMissing.Add(sapi.World.Blocks[i].Code);
                }
            }
            for (int i = 0; i < sapi.World.Items.Length; i++)
            {
                if (sapi.World.Items[i].IsMissing)
                {
                    Missing.Add(sapi.World.Items[i].Code);
                }
                else
                {
                    NotMissing.Add(sapi.World.Items[i].Code);
                }
            }
        }

        public void ExportMissing(IServerPlayer player, int groupID)
        {
            RePopulate();
            string missing = "[" + nl;
            for (int i = 0; i < Missing.Count - 1; i++)
            {
                missing += "    \"" + Missing[i].ToString() + "\"," + nl;
            }
            missing += "    \"" + Missing[Missing.Count - 1].ToString() + "\"" + nl + "]";

            using (TextWriter tW = new StreamWriter("missingcollectibles.json"))
            {
                tW.Write(missing);
                tW.Close();
            }
            player.SendMessage(groupID, "Okay, exported list of missing things.", EnumChatType.CommandError);
        }

        public void TryRemapMissing(IServerPlayer player)
        {
            RePopulate();
            if (Missing.Count < 1) return;
            sapi.SendMessage(player, GlobalConstants.GeneralChatGroup, "Starting Remapping, Server May Lag For Bit.", EnumChatType.Notification);

            MostLikely.Clear();

            for (int i = 0; i < Missing.Count; i++)
            {
                List<int> distance = new List<int>();
                for (int j = 0; j < NotMissing.Count; j++)
                {
                    if (Missing[i] == null || NotMissing[j] == null)
                    {
                        distance.Add(999999999);
                        continue;
                    }
                    distance.Add(LevenshteinDistance.Compute(Missing[i].ToString().Replace("game:", ""), NotMissing[j].ToString().Replace("game:", "")));
                }
                MostLikely.Add(Missing[i], NotMissing[distance.IndexOfMin()]);
                sapi.SendMessage(player, GlobalConstants.GeneralChatGroup, Math.Round((i / (float)Missing.Count) * 100, 2) + "%", EnumChatType.Notification);
            }
            for (int i = 0; i < MostLikely.Count; i++)
            {
                try
                {
                    AssetLocation key = MostLikely.ElementAt(i).Key;
                    AssetLocation value = MostLikely.ElementAt(i).Value;
                    if (key.GetBlock(sapi) != null && value.GetBlock(sapi) != null)
                    {
                        sChannel.BroadcastPacket(new Message()
                        {
                            IR = "bir",
                            UID = player.PlayerUID,
                            from = key.ToString(),
                            to = value.ToString()
                        });
                    }
                    else if (key.GetItem(sapi) != null && value.GetItem(sapi) != null)
                    {
                        sChannel.BroadcastPacket(new Message()
                        {
                            IR = "iir",
                            UID = player.PlayerUID,
                            from = key.ToString(),
                            to = value.ToString()
                        });
                    }
                }
                catch (Exception){}
            }
            sapi.SendMessage(player, GlobalConstants.GeneralChatGroup, "100%, Please Restart Server", EnumChatType.Notification);
        }
    }

    static class Util1
    {
        public static int IndexOfMin(this IList<int> self)
        {
            if (self == null)
            {
                throw new ArgumentNullException("self");
            }

            if (self.Count == 0)
            {
                throw new ArgumentException("List is empty.", "self");
            }

            int min = self[0];
            int minIndex = 0;

            for (int i = 1; i < self.Count; ++i)
            {
                if (self[i] < min)
                {
                    min = self[i];
                    minIndex = i;
                }
            }
            return minIndex;
        }
    }

    //https://stackoverflow.com/questions/2344320/comparing-strings-with-tolerance
    static class LevenshteinDistance
    {
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }
}
