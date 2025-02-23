﻿using MapAssist.Types;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MapAssist.Helpers
{
    public class ItemExport
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private static string itemTemplate = "<div class=\"item\"><div class=\"item-name\" style=\"color: {{color}}\">{{name}}</div>{{stats}}</div>";
        private static string statTemplate = "<div class=\"stat\" style=\"color:#4169E1\">{{text}}</div>";

        public static void ExportPlayerInventory(UnitPlayer player, UnitItem[] itemAry)
        {
            var itemsExport = GetItemsExport(player, itemAry);

            ExportPlayerInventoryHTML(player, itemsExport);
            ExportPlayerInventoryJSON(player, itemsExport);
        }

        public static ExportedItems GetItemsExport(UnitPlayer player, UnitItem[] itemAry)
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                var items = itemAry.Select(item => { item.IsCached = false; return item.Update(); }).ToList();

                var equippedItems = items.Where(x => x.ItemData.dwOwnerID == player.UnitId && x.ItemData.InvPage == InvPage.NULL && x.ItemData.BodyLoc != BodyLoc.NONE).ToList();
                var inventoryItems = items.Where(x => x.ItemData.dwOwnerID == player.UnitId && x.ItemData.InvPage == InvPage.INVENTORY).ToList();
                var mercItems = items.Where(x => x.ItemMode == ItemMode.EQUIP && x.ItemModeMapped == ItemModeMapped.Mercenary).ToList();
                var stashPersonalItems = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Personal).ToList();
                var stashShared1Items = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Shared1).ToList();
                var stashShared2Items = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Shared2).ToList();
                var stashShared3Items = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Shared3).ToList();
                var cubeItems = items.Where(x => x.ItemData.dwOwnerID == player.UnitId && x.ItemModeMapped == ItemModeMapped.Cube).ToList();

                return new ExportedItems()
                {
                    equipped = equippedItems,
                    inventory = inventoryItems,
                    mercenary = mercItems,
                    cube = cubeItems,
                    personalStash = stashPersonalItems,
                    sharedStashTab1 = stashShared1Items,
                    sharedStashTab2 = stashShared2Items,
                    sharedStashTab3 = stashShared3Items,
                };
            }
        }

        public static void ExportPlayerInventoryJSON(UnitPlayer player, ExportedItems items)
        {
            var outputfile = player.Name + ".json";

            var json = new Dictionary<string, object>
            {
                { "items",  new Dictionary<string, List<JSONItem>> {
                    { "equipped", ItemsToList(player, items.equipped)},
                    { "inventory", ItemsToList(player,items.inventory) },
                    { "mercenary", ItemsToList(player,items.mercenary) },
                    { "cube", ItemsToList(player,items.cube) },
                    { "personalStash", ItemsToList(player,items.personalStash) },
                    { "sharedStashTab1", ItemsToList(player,items.sharedStashTab1) },
                    { "sharedStashTab2", ItemsToList(player,items.sharedStashTab2) },
                    { "sharedStashTab3", ItemsToList(player,items.sharedStashTab3) },
                }}
            };
            var finalJSONstr = JsonConvert.SerializeObject(json);

            File.WriteAllText(outputfile, finalJSONstr);
            _log.Info($"Created JSON item file {outputfile}");
        }

        public static void ExportPlayerInventoryHTML(UnitPlayer player, ExportedItems items)
        {
            var finalHTMLstr = Properties.Resources.InventoryExportTemplate;
            var outputfile = player.Name + ".html";

            finalHTMLstr = finalHTMLstr.Replace("{{player-name}}", player.Name);

            if (items.equipped.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-equipped}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{equipped-items}}", GetItemHtmlList(player, items.equipped));
            }

            if (items.inventory.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-inventory}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{inventory-items}}", GetItemHtmlList(player, items.inventory));
            }

            if (items.mercenary.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-merc}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{merc-items}}", GetItemHtmlList(player, items.mercenary));
            }

            if (items.personalStash.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-stash-personal}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{stash-personal-items}}", GetItemHtmlList(player, items.personalStash));
            }

            if (items.sharedStashTab1.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-stash-shared1}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{stash-shared1-items}}", GetItemHtmlList(player, items.sharedStashTab1));
            }

            if (items.sharedStashTab2.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-stash-shared2}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{stash-shared2-items}}", GetItemHtmlList(player, items.sharedStashTab2));
            }

            if (items.sharedStashTab3.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-stash-shared3}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{stash-shared3-items}}", GetItemHtmlList(player, items.sharedStashTab3));
            }

            if (items.cube.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-cube}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{cube-items}}", GetItemHtmlList(player, items.cube));
            }

            File.WriteAllText(outputfile, finalHTMLstr);
            _log.Info($"Created HTML item file {outputfile}");
        }

        public static List<JSONItem> ItemsToList(UnitPlayer player, List<UnitItem> filteredItems)
        {
            var itemJSONarr = new List<JSONItem>();
            foreach (var item in filteredItems)
            {
                item.Stats.TryGetValue(Stats.Stat.NumSockets, out var numSockets);
                var thisItem = new JSONItem()
                {
                    txtFileNo = item.TxtFileNo,
                    baseName = item.ItemBaseName,
                    quality = item.ItemData.ItemQuality.ToString(),
                    fullName = Items.ItemFullName(item),
                    runeWord = item.IsRuneWord ? Items.GetRunewordFromId(item.Prefixes[0]) : null,
                    ethereal = item.IsEthereal,
                    identified = item.IsIdentified,
                    numSockets = numSockets,
                    position = new Position() { x = (uint)item.Position.X, y = (uint)item.Position.Y },
                    bodyLoc = item.ItemData.BodyLoc.ToString(),
                    stats = StatReader.GetStatsText(item, player)
                };
                itemJSONarr.Add(thisItem);
            }
            return itemJSONarr;
        }

        private static string GetItemHtmlList(UnitPlayer player, IEnumerable<UnitItem> items)
        {
            var result = "";

            foreach (var item in items.OrderBy(x => x.TxtFileNo))
            {
                result += GetItemHtml(player, item);
            }

            return result;
        }

        private static string GetItemHtml(UnitPlayer player, UnitItem item)
        {
            var itemName = Items.ItemFullName(item);

            if (item.ItemData.ItemQuality > ItemQuality.SUPERIOR && item.IsIdentified)
            {
                itemName = itemName.Replace("[Identified] ", "");
            }

            var itemText = itemTemplate.Replace("{{color}}", ColorTranslator.ToHtml(item.ItemBaseColor)).Replace("{{name}}", itemName);
            var statText = "";

            if (item.ItemData.ItemQuality > ItemQuality.SUPERIOR && !item.IsIdentified)
            {
                statText += statTemplate.Replace("{{text}}", "Unidentified").Replace("4169E1", "DD0000");

                if (item.Stats.TryGetValue(Stats.Stat.Defense, out var defense))
                {
                    statText += statTemplate.Replace("{{text}}", Stats.Stat.Defense.ToString().AddSpaces() + ": " + defense);
                }
            }
            else
            {
                var affixes = StatReader.GetStatsText(item, player);
                foreach (var affix in affixes)
                {
                    statText += statTemplate.Replace("{{text}}", affix);
                }
            }

            itemText = itemText.Replace("{{stats}}", statText);

            return itemText;
        }
    }
}
