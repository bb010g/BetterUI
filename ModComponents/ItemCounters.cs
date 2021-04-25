﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

using RoR2;
using RoR2.UI;
using BepInEx.Configuration;

namespace BetterUI
{
    static class ItemCounters
    {
        static string[] tierColorMap = new string[]
        {
            ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Tier1Item),
            ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Tier2Item),
            ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Tier3Item),
            ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.LunarItem),
            ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.BossItem),
            ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Error),
        };

        static ItemCounters()
        {
            BetterUIPlugin.onStart += onStart;
        }
        internal static void Hook()
        {
            if (BetterUIPlugin.instance.config.ItemCountersShowItemCounters.Value)
            {
                HookManager.Add<RoR2.UI.ScoreboardStrip, CharacterMaster>("SetMaster", ScoreboardStrip_SetMaster);
                HookManager.Add<RoR2.UI.ScoreboardStrip>("Update", ScoreboardStrip_Update);
            }
        }

        private static void onStart(BetterUIPlugin plugin)
        {
            char[] bad_characters = new char[] { '\n', '\t', '\\', '"', '\'', '[', ']' };
            bool first = true;
            foreach (var itemIndex in RoR2.ItemCatalog.allItems)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (String.IsNullOrWhiteSpace(itemDef.nameToken))
                {
                    continue;
                }
                int itemValue = BetterUIPlugin.instance.config.ItemCountersTierScores[(int)itemDef.tier];
                String safe_name = String.Join("", itemDef.nameToken.Split(bad_characters));
                ConfigEntry<int> itemScore;
                if (first)
                {
                    itemScore = BetterUIPlugin.instance.config.ConfigFileItemCounters.Bind<int>("ItemScores", safe_name, itemValue, $"Score of each item for the ItemScore.\n{Language.GetString(itemDef.nameToken)}");
                    first = false;
                }
                else
                {
                    itemScore = BetterUIPlugin.instance.config.ConfigFileItemCounters.Bind<int>("ItemScores", safe_name, itemValue, Language.GetString(itemDef.nameToken));
                }

                BetterUIPlugin.instance.config.ItemCountersItemScores.Add(itemDef.nameToken, itemScore.Value);
            }
        }
        internal static void ScoreboardStrip_SetMaster(Action<RoR2.UI.ScoreboardStrip, CharacterMaster> orig, ScoreboardStrip self, CharacterMaster master)
        {
            orig(self, master);

            self.nameLabel.lineSpacing = -20;
            self.nameLabel.overflowMode = TMPro.TextOverflowModes.Truncate;
            self.nameLabel.enableWordWrapping = false;
            self.moneyText.overflowMode = TMPro.TextOverflowModes.Overflow;
        }

        static int itemSum;
        static int itemScore;
        internal static void ScoreboardStrip_Update(Action<RoR2.UI.ScoreboardStrip> orig, ScoreboardStrip self)
        {
            orig(self);

            if (self.master && self.master.inventory)
            {
                BetterUIPlugin.sharedStringBuilder.Clear();
                BetterUIPlugin.sharedStringBuilder.Append(Util.GetBestMasterName(self.master));
                BetterUIPlugin.sharedStringBuilder.Append("\n<#F8FC97>");
                BetterUIPlugin.sharedStringBuilder.Append(self.master.money);
                BetterUIPlugin.sharedStringBuilder.Append("</color>");

                self.nameLabel.text = BetterUIPlugin.sharedStringBuilder.ToString();
                BetterUIPlugin.sharedStringBuilder.Clear();
                BetterUIPlugin.sharedStringBuilder.Append("<#FFFFFF>");



                if (BetterUIPlugin.instance.config.ItemCountersShowItemSum.Value)
                {
                    itemSum = 0;
                    foreach (var tier in BetterUIPlugin.instance.config.ItemCountersItemSumTiers)
                    {
                        itemSum += self.master.inventory.GetTotalItemCountOfTier(tier);
                    }
                    BetterUIPlugin.sharedStringBuilder.Append(itemSum);
                    if (BetterUIPlugin.instance.config.ItemCountersShowItemScore.Value)
                    {
                        BetterUIPlugin.sharedStringBuilder.Append(" | ");
                    }
                }
                if (BetterUIPlugin.instance.config.ItemCountersShowItemScore.Value)
                {
                    itemScore = 0;
                    foreach (var item in self.master.inventory.itemAcquisitionOrder)
                    {
                        int value;
                        itemScore += BetterUIPlugin.instance.config.ItemCountersItemScores.TryGetValue(ItemCatalog.GetItemDef(item).nameToken, out value) ? value * self.master.inventory.GetItemCount(item) : 0;
                    }
                    BetterUIPlugin.sharedStringBuilder.Append(itemScore);
                }

                if (BetterUIPlugin.instance.config.ItemCountersShowItemsByTier.Value)
                {
                    BetterUIPlugin.sharedStringBuilder.Append("\n");
                    foreach (var tier in BetterUIPlugin.instance.config.ItemCountersItemsByTierOrder)
                    {
                        BetterUIPlugin.sharedStringBuilder.Append(" <#");
                        BetterUIPlugin.sharedStringBuilder.Append(tierColorMap[(int)tier]);
                        BetterUIPlugin.sharedStringBuilder.Append(">");
                        BetterUIPlugin.sharedStringBuilder.Append(self.master.inventory.GetTotalItemCountOfTier(tier));
                        BetterUIPlugin.sharedStringBuilder.Append("</color>");
                    }
                }

                BetterUIPlugin.sharedStringBuilder.Append("</color>");

                self.moneyText.text = BetterUIPlugin.sharedStringBuilder.ToString();
            }
        }
    }
}
