using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
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
        bool canExecuteRemap = true;

        string nl = Environment.NewLine;

        public List<AssetLocation> MissingBlocks { get; set; } = new List<AssetLocation>();
        public List<AssetLocation> MissingItems { get; set; } = new List<AssetLocation>();

        public List<AssetLocation> NotMissingBlocks { get; set; } = new List<AssetLocation>();
        public List<AssetLocation> NotMissingItems { get; set; } = new List<AssetLocation>();

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
                        capi.SendChatMessage("/" + a.IR + " remap " + a.to + " " + a.from + " force");
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

            sapi.RegisterCommand("exportmatches", "Exports Matches", "", (p, g, a) =>
            {
                ExportMatches(p);
            }, Privilege.controlserver);

            sapi.RegisterCommand("loadmatches", "Loads Matches", "", (p, g, a) =>
            {
                LoadMatches();
            }, Privilege.controlserver);

            sapi.RegisterCommand("tryremap", "Try To Remap Missing Collectables Using Levenshtein Distance", "", (p, g, a) =>
            {
                if (canExecuteRemap)
                {
                    canExecuteRemap = false;
                    TryRemapMissing(p);
                }
                
            }, Privilege.controlserver);
        }

        public void LoadMatches()
        {
            try
            {
                using (TextReader tW = new StreamReader("matches.json"))
                {
                    MostLikely = JsonConvert.DeserializeObject<Dictionary<AssetLocation, AssetLocation>>(tW.ReadToEnd());
                }
            }
            catch (Exception) {}
        }

        public void RePopulate()
        {
            MissingBlocks.Clear();
            MissingItems.Clear();
            NotMissingBlocks.Clear();
            NotMissingItems.Clear();

            for (int i = 0; i < sapi.World.Blocks.Length; i++)
            {
                if (sapi.World.Blocks[i].IsMissing)
                {
                    MissingBlocks.Add(sapi.World.Blocks[i].Code);
                }
                else
                {
                    NotMissingBlocks.Add(sapi.World.Blocks[i].Code);
                }
            }
            for (int i = 0; i < sapi.World.Items.Length; i++)
            {
                if (sapi.World.Items[i].IsMissing)
                {
                    MissingItems.Add(sapi.World.Items[i].Code);
                }
                else
                {
                    NotMissingItems.Add(sapi.World.Items[i].Code);
                }
            }
        }

        public void ExportMissing(IServerPlayer player, int groupID)
        {
            RePopulate();
            List<AssetLocation> combined = MissingBlocks.Concat(MissingItems).ToList();
            string a = JsonConvert.SerializeObject(combined, Formatting.Indented);
            using (TextWriter tW = new StreamWriter("missingcollectibles.json"))
            {
                tW.Write(a);
                tW.Close();
            }
            player.SendMessage(groupID, "Okay, exported list of missing things.", EnumChatType.CommandError);
        }

        public void ExportMatches(IServerPlayer player)
        {
            FindMatches(player);

            using (TextWriter tW = new StreamWriter("matches.json"))
            {
                tW.Write(JsonConvert.SerializeObject(MostLikely, Formatting.Indented));
                tW.Close();
            }
            player.SendMessage(GlobalConstants.GeneralChatGroup, "Okay, exported list of matching things.", EnumChatType.CommandError);
        }

        public void FindMatches(IServerPlayer player)
        {
            MostLikely.Clear();
            RePopulate();
            Matches(player, MissingBlocks, NotMissingBlocks, "Block");
            Matches(player, MissingItems, NotMissingItems, "Item");
        }

        public void Matches(IPlayer player, List<AssetLocation> missing, List<AssetLocation> notmissing, string type = "Block")
        {
            for (int i = 0; i < missing.Count; i++)
            {
                List<int> distance = new List<int>();
                for (int j = 0; j < notmissing.Count; j++)
                {
                    if (missing[i] == null || notmissing[j] == null)
                    {
                        distance.Add(999999999);
                        continue;
                    }
                    distance.Add(missing[i].ToString().Replace(missing[i].Domain + ":", "").ComputeDistance(notmissing[j].ToString().Replace(notmissing[j].Domain + ":", "")));
                }
                int index = distance.IndexOfMin();

                if (!MostLikely.ContainsValue(notmissing[index]))
                {
                    MostLikely.Add(missing[i], notmissing[index]);
                    notmissing.RemoveAt(index);
                }

                sapi.SendMessage(player, GlobalConstants.InfoLogChatGroup, "Finding Closest "+ type +" Matches... " + Math.Round(i / (float)missing.Count * 100, 2) + "%", EnumChatType.Notification);
            }
            sapi.SendMessage(player, GlobalConstants.InfoLogChatGroup, "Finding Closest " + type + " Matches... 100%", EnumChatType.Notification);
        }

        long id;
        public void TryRemapMissing(IServerPlayer player)
        {
            RePopulate();
            if (MissingItems.Count < 1 && MissingBlocks.Count < 1)
            {
                sapi.SendMessage(player, GlobalConstants.GeneralChatGroup, "Looks good, no need for remapping.", EnumChatType.Notification);
                return;
            }
            LoadMatches();

            if (MostLikely.Count < 1)
            {
                sapi.SendMessage(player, GlobalConstants.InfoLogChatGroup, "Empty or Missing JSON, Will Search For Matches Instead Of Loading, Server May Lag For Bit.", EnumChatType.Notification);
                ExportMatches(player);
            }
            

            sapi.SendMessage(player, GlobalConstants.InfoLogChatGroup, "Begin Remapping", EnumChatType.Notification);

            int f = 0;
            id = sapi.World.RegisterGameTickListener(dt =>
            {
                if (f < MostLikely.Count)
                {
                    f++;
                    try
                    {
                        AssetLocation key = MostLikely.ElementAt(f).Key;
                        AssetLocation value = MostLikely.ElementAt(f).Value;
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
                        sapi.SendMessage(player, GlobalConstants.InfoLogChatGroup, "Remapping... " + Math.Round(f / (float)MostLikely.Count * 100, 2) + "%", EnumChatType.Notification);
                    }
                    catch (Exception) { }
                }
                else
                {
                    sapi.SendMessage(player, GlobalConstants.InfoLogChatGroup, "Remapping... 100%", EnumChatType.Notification);
                    sapi.SendMessage(player, GlobalConstants.InfoLogChatGroup, "Please Restart Server Or Leave And Reopen World", EnumChatType.Notification);
                    canExecuteRemap = true;
                    sapi.World.UnregisterGameTickListener(id);
                }
            }, 100);

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

        public static int ComputeDistance(this string a, string b)
        {
            return Compute(a, b);
        }
    }
}
