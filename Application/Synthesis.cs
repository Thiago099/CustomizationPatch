using Domain;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrapper;

namespace Application
{
    public static class Synthesis
    {
        private static IGameEnvironment gameEnv;
        private static ILoadOrderGetter<IModListingGetter<IModGetter>> loadOrder;
        static void init()
        {
            // Initialize the game environment for Skyrim Special Edition
            gameEnv = GameEnvironment.Typical.Builder(GameRelease.SkyrimSE).WithTargetDataFolder(Path.GetFullPath("../")).Build();
            loadOrder = gameEnv.LoadOrder;
        }
        static void end()
        {
            gameEnv.Dispose();
        }
        public static void Build(Dictionary<string, PageUserData> AllData, List<Page> Pages)
        {
            init();
            try
            {
                var patch = Modification.Patch("All.customisation-patch.esp");

                patch.ModHeader.Flags = SkyrimModHeader.HeaderFlag.LightMaster;

                foreach (var page in Pages) {
                    BuildPage(AllData, page, patch);
                }
                end();
                Modification.Save(patch, "All.customisation-patch.esp");

            }
            catch
            {
                end();
                throw;
            }
        }

        public static void BuildSingle(Dictionary<string, PageUserData> AllData, Page page)
        {
            init();
            try
            {
                var patch = Modification.Patch(page.Name + ".customisation-patch.esp");
                patch.ModHeader.Flags = SkyrimModHeader.HeaderFlag.LightMaster;

                BuildPage(AllData, page, patch);

                end();
                Modification.Save(patch, page.Name + ".customisation-patch.esp");
            }
            catch
            {
                end();
                throw;
            }
        }


        private static void BuildPage(Dictionary<string, PageUserData> AllData, Page page, SkyrimMod patch)
        {
            var pageData = AllData[page.Id];
            if (page.Files != null)
            {
                foreach (var item in page.Files)
                {
                    if(item.Value.EndsWith(".ini"))
                    {
                        File.Delete("../" + item.Value.Replace("..",""));
                    }
                    else
                    {
                        throw new Exception($"Invalid file format on \"{item.Value}\", it must be a ini file");
                    }
                }
            }
            foreach (var group in page.Groups)
            {
                var groupData = pageData.ChosenData[group.Id];
                foreach (var item in group.Items)
                {
                    var target = AST.Parse(item.Target);
                    var value = AST.Parse(item.Value);

                    ApplyChanges(page, patch, target, AST.Resolve(group, groupData, value, item), item.Target);
                }
            }
        }

        static void ApplyChanges(Page page, SkyrimMod patch, List<ASTNode> target, object value, string expression)
        {
            var path = AST.GetPath(target);
            ISkyrimModDisposableGetter chosenPlugin = null;



            ModSelection(0);

            void ModSelection(int i)
            {
                if(page.Files != null && page.Files.TryGetValue((string)path[i], out string? file))
                {
                    if (file.EndsWith(".ini"))
                    {
                        File.AppendAllText("../" + file, value.ToString().Replace("..", "") + "\n");
                    }
                    return;
                }
                foreach (var plugin in loadOrder.PriorityOrder)
                {
                    if(plugin.FileName.EndsWith(".customisation-patch.esp") || !plugin.ExistsOnDisk || !plugin.Enabled)
                    {
                        continue;
                    }
                    chosenPlugin = Modification.Read(plugin.FileName);
                    if (FeatureSelection(i)) return;
                }

                var element = (string)path[i];
                throw new Exception($"Item not found \"{element}\"");
            }

            bool FeatureSelection(int i)
            {
                if (LeveledItem(i)) return true;
                if (MiscItem(i)) return true;
                if (Book(i)) return true;
                if (Ingredient(i)) return true;
                if (Weapon(i)) return true;
                if (Ammunitions(i)) return true;
                if (Armor(i)) return true;
                if (Spell(i)) return true;
                if (Shouts(i)) return true;
                if (Key(i)) return true;
                if (ConstructibleObjects(i)) return true;
                if (FormLists(i)) return true;

                return false;
            }
            bool LeveledItem(int i)
            {
                var element = (string)path[i];

                var cur = chosenPlugin?.LeveledItems.Records.FirstOrDefault(x=>x.EditorID == element);

                if (cur == null) return false;

                var prop = (string)path[i + 1];

                var copy = patch.LeveledItems.GetOrAddAsOverride(cur);
                switch (prop)
                {
                    case "ChanceNone":
                        copy.ChanceNone = byte.Parse(value.ToString());
                        break;
                    case "Entries":
                        Entry(copy.Entries, i + 1);
                        break;
                }
                return true;

            }
            bool MiscItem(int i)
            {
                var element = (string)path[i];

                var cur = chosenPlugin?.MiscItems.Records.FirstOrDefault(x => x.EditorID == element);

                if (cur == null) return false;

                var prop = (string)path[i + 1];

                var copy = patch.MiscItems.GetOrAddAsOverride(cur);
                switch (prop)
                {
                    case "GoldValue":
                        copy.Value = uint.Parse(value.ToString());
                        break;
                    case "Weight":
                        copy.Weight = float.Parse(value.ToString());
                        break;
                    case "Name":
                        copy.Name = (string)value;
                        break;
                }
                return true;
            }

            

            bool Key(int i)
            {
                var element = (string)path[i];

                var cur = chosenPlugin?.Keys.Records.FirstOrDefault(x => x.EditorID == element);

                if (cur == null) return false;

                var prop = (string)path[i + 1];

                var copy = patch.Keys.GetOrAddAsOverride(cur);
                switch (prop)
                {
                    case "GoldValue":
                        copy.Value = uint.Parse(value.ToString());
                        break;
                    case "Weight":
                        copy.Weight = float.Parse(value.ToString());
                        break;
                    case "Name":
                        copy.Name = (string)value;
                        break;
                }
                return true;

            }
            bool Book(int i)
            {
                var element = (string)path[i];

                var cur = chosenPlugin?.Books.Records.FirstOrDefault(x => x.EditorID == element);

                if (cur == null) return false;

                var prop = (string)path[i + 1];

                var copy = patch.Books.GetOrAddAsOverride(cur);
                switch (prop)
                {
                    case "GoldValue":
                        copy.Value = uint.Parse(value.ToString());
                        break;
                    case "Weight":
                        copy.Weight = float.Parse(value.ToString());
                        break;
                    case "Name":
                        copy.Name = (string)value;
                        break;
                    case "BookText":
                        copy.BookText = (string)value;
                        break;
                    case "Description":
                        copy.Description = (string)value;
                        break;
                }
                return true;

            }
            bool Ingredient(int i)
            {
                var element = (string)path[i];

                var cur = chosenPlugin?.Ingredients.Records.FirstOrDefault(x => x.EditorID == element);

                if (cur == null) return false;

                var prop = (string)path[i + 1];

                var copy = patch.Ingredients.GetOrAddAsOverride(cur);
                switch (prop)
                {
                    case "GoldValue":
                        copy.Value = uint.Parse(value.ToString());
                        break;
                    case "Weight":
                        copy.Weight = float.Parse(value.ToString());
                        break;
                    case "Name":
                        copy.Name = (string)value;
                        break;
                    case "Effects":
                        Effect(copy.Effects, i + 1);
                        break;
                }
                return true;

            }
            bool Weapon(int i)
            {
                var element = (string)path[i];

                var cur = chosenPlugin?.Weapons.Records.FirstOrDefault(x => x.EditorID == element);

                if (cur == null) return false;

                var prop = (string)path[i + 1];

                var copy = patch.Weapons.GetOrAddAsOverride(cur);
                switch (prop)
                {
                    case "GoldValue":
                        copy.BasicStats.Value = uint.Parse(value.ToString());
                        break;
                    case "Weight":
                        copy.BasicStats.Weight = float.Parse(value.ToString());
                        break;
                    case "Damge":
                        copy.BasicStats.Damage = ushort.Parse(value.ToString());
                        break;
                    case "Speed":
                        copy.Data.Speed = ushort.Parse(value.ToString());
                        break;
                    case "Reach":
                        copy.Data.Reach = ushort.Parse(value.ToString());
                        break;
                    case "Name":
                        copy.Name = (string)value;
                        break;
                    case "Description":
                        copy.Description = (string)value;
                        break;
                }
                return true;
            }

            bool Ammunitions(int i)
            {
                var element = (string)path[i];

                var cur = chosenPlugin?.Ammunitions.Records.FirstOrDefault(x => x.EditorID == element);

                if (cur == null) return false;

                var prop = (string)path[i + 1];

                var copy = patch.Ammunitions.GetOrAddAsOverride(cur);
                switch (prop)
                {
                    case "GoldValue":
                        copy.Value = uint.Parse(value.ToString());
                        break;
                    case "Weight":
                        copy.Weight = float.Parse(value.ToString());
                        break;
                    case "Damge":
                        copy.Damage = ushort.Parse(value.ToString());
                        break;
                    case "Name":
                        copy.Name = (string)value;
                        break;
                    case "Description":
                        copy.Description = (string)value;
                        break;
                }
                return true;

            }
            bool Armor(int i)
            {
                var element = (string)path[i];

                var cur = chosenPlugin?.Armors.Records.FirstOrDefault(x => x.EditorID == element);

                if (cur == null) return false;

                var prop = (string)path[i + 1];

                var copy = patch.Armors.GetOrAddAsOverride(cur);
                switch (prop)
                {
                    case "GoldValue":
                        copy.Value = uint.Parse(value.ToString());
                        break;
                    case "Weight":
                        copy.Weight = float.Parse(value.ToString());
                        break;
                    case "ArmorRating":
                        copy.ArmorRating = ushort.Parse(value.ToString());
                        break;
                    case "Name":
                        copy.Name = (string)value;
                        break;
                    case "Description":
                        copy.Description = (string)value;
                        break;
                }
                return true;

            }
            bool Spell(int i)
            {
                var element = (string)path[i];

                var cur = chosenPlugin?.Spells.Records.FirstOrDefault(x => x.EditorID == element);

                if (cur == null) return false;

                var prop = (string)path[i + 1];

                var copy = patch.Spells.GetOrAddAsOverride(cur);
                switch (prop)
                {
                    case "Description":
                        copy.Description = (string)value;
                        break;
                    case "Effects":
                        Effect(copy.Effects, i + 1);
                        break;
                    case "Name":
                        copy.Name = (string)value;
                        break;
                }
                return true;
            }
            bool Shouts(int i)
            {
                var element = (string)path[i];

                var cur = chosenPlugin?.Shouts.Records.FirstOrDefault(x => x.EditorID == element);

                if (cur == null) return false;

                var prop = (string)path[i + 1];

                var copy = patch.Shouts.GetOrAddAsOverride(cur);
                switch (prop)
                {
                    case "Description":
                        copy.Description = (string)value;
                        break;
                    case "WordsOfPower":
                        WordsOfPower(copy.WordsOfPower, i + 1);
                        break;
                    case "Name":
                        copy.Name = (string)value;
                        break;
                }
                return true;
            }
            void WordsOfPower(ExtendedList<ShoutWord> Words, int i)
            {
                var id = (int)path[i + 1];
                var prop = (string)path[i + 2];

                if (id >= Words.Count)
                {
                    throw new Exception($"Index \"{id}\" is outside of \"{path[i]}\" bounds in expression \"{expression}\"");
                }

                var copy = Words[id];

                if (copy == null) return;

                switch (prop)
                {
                    case "RecoveryTime":
                        copy.RecoveryTime = float.Parse(value.ToString());
                        break;
                }
            }

            bool ConstructibleObjects(int i)
            {
                var element = (string)path[i];

                var cur = chosenPlugin?.ConstructibleObjects.Records.FirstOrDefault(x => x.EditorID == element);

                if (cur == null) return false;

                var prop = (string)path[i + 1];

                var copy = patch.ConstructibleObjects.GetOrAddAsOverride(cur);
                switch (prop)
                {
                    case "Items":
                        ConstructibleObjectsItem(copy.Items, i + 1);
                        break;
                }
                return true;
            }
            void ConstructibleObjectsItem(ExtendedList<ContainerEntry> Items, int i)
            {
                var id = (int)path[i + 1];
                var prop = (string)path[i + 2];

                if (id >= Items.Count)
                {
                    throw new Exception($"Index \"{id}\" is outside of \"{path[i]}\" bounds in expression \"{expression}\"");
                }

                var copy = Items[id];

                if (copy == null) return;

                switch (prop)
                {
                    case "Count":
                        copy.Item.Count = int.Parse(value.ToString());
                        break;
                }
            }

            void Effect(ExtendedList<Mutagen.Bethesda.Skyrim.Effect> Effects, int i)
            {
                var id = (int)path[i+1];
                var prop = (string)path[i + 2];

                if (id >= Effects.Count)
                {
                    throw new Exception($"Index \"{id}\" is outside of \"{path[i]}\" bounds in expression \"{expression}\"");
                }

                var copy = Effects[id].Data;

                if (copy == null) return;

                switch (prop)
                {
                    case "Magnitude":
                        copy.Magnitude = float.Parse(value.ToString());
                        break;
                    case "Area":
                        copy.Area = int.Parse(value.ToString());
                        break;
                    case "Duration":
                        copy.Duration = int.Parse(value.ToString());
                        break;
                }
            }
            void Entry(ExtendedList<Mutagen.Bethesda.Skyrim.LeveledItemEntry> Entries, int i)
            {
                var id = (int)path[i + 1];
                var prop = (string)path[i + 2];

                if (id >= Entries.Count)
                {
                    throw new Exception($"Index \"{id}\" is outside of \"{path[i]}\" bounds in expression \"{expression}\"");
                }

                var copy = Entries[id].Data;

                if (copy == null) return;

                switch (prop)
                {
                    case "Count":
                        copy.Count = short.Parse(value.ToString());
                        break;
                    case "Level":
                        copy.Level = short.Parse(value.ToString());
                        break;
                }
            }
            bool FormLists(int i)
            {
                var element = (string)path[i];

                var cur = chosenPlugin?.FormLists.Records.FirstOrDefault(x => x.EditorID == element);

                if (cur == null) return false;

                var prop = (string)path[i + 1];

                var copy = patch.FormLists.GetOrAddAsOverride(cur);
                switch (prop)
                {
                    case "Items":
                        FormListsItem(copy.Items, i + 1);
                        break;
                }
                return true;
            }
            void FormListsItem(ExtendedList<IFormLinkGetter<ISkyrimMajorRecordGetter>> Items, int i)
            {
                var id = (int)path[i + 1];
                var prop = (string)path[i + 2];

                if(id >= Items.Count)
                {
                    throw new Exception($"Index \"{id}\" is outside of \"{path[i]}\" bounds in expression \"{expression}\"");
                }

                var copy = Items[id];


                if (copy == null) return;

                var name = copy.FormKey.IDString() + "-" + copy.FormKey.ModKey;

                switch (prop)
                {
                    case "Repeat":
                        var count = int.Parse(value.ToString());
                        if (count > 1)
                        {
                            Items.Remove(copy);
                            string itemId = name+"-alias-";
                            for (int j = 0; j < count; j++)
                            {
                                var current = itemId + (j + 1);


                                FormList? itemListPlaceholder;
                                if (!createdAlias.TryGetValue(current, out itemListPlaceholder))
                                {
                                    itemListPlaceholder = patch.FormLists.AddNew(current);
                                    itemListPlaceholder.Items.Add(copy);
                                    createdAlias[current] = itemListPlaceholder;
                                }
                                Items.Add(itemListPlaceholder);
                            }
                        }
                        break;
                }
            }
        }
        static Dictionary<string, FormList> createdAlias = new();
    }
}
