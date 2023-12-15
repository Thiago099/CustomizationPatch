using Domain;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Translations;
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


using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Cache.Internals;
using Mutagen.Bethesda.Plugins.Exceptions;
using static System.Runtime.InteropServices.JavaScript.JSType;


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


        class ModificationResult<TGetter, TSetter>
        where TGetter : class, ISkyrimMajorRecordGetter, IBinaryItem
        where TSetter : class, ISkyrimMajorRecordInternal, IBinaryItem, IMajorRecordInternal, TGetter
        {
            public ISkyrimGroupGetter<TGetter> Getter { get; set; }
            public SkyrimGroup<TSetter> Setter { get; set; }
            public TGetter Item { get; set; }
        }

        static ModificationResult<TGetter, TSetter>? getData<TGetter, TSetter>(
            ISkyrimModDisposableGetter chosenPlugin,
            SkyrimMod patch,
              Func<ISkyrimModDisposableGetter, ISkyrimGroupGetter<TGetter>> getGetter,
              Func<SkyrimMod, SkyrimGroup<TSetter>> getSetter,
              Func<ISkyrimGroupGetter<TGetter>, TGetter?> get
          )
              where TGetter : class, ISkyrimMajorRecordGetter, IBinaryItem
              where TSetter : class, ISkyrimMajorRecordInternal, IBinaryItem, IMajorRecordInternal, TGetter
        {
            var result = new ModificationResult<TGetter, TSetter>();
            result.Getter = getGetter(chosenPlugin);
            var item = get(result.Getter);
            if (item == null) return null;
            result.Item = item;
            result.Setter = getSetter(patch);
            return result;
        }
        static void apply<TGetter, TSetter, TValue>(ModificationResult<TGetter, TSetter> input, object value,

            Func<string, TValue> parse,
            Func<TGetter, TValue> getValue,
            Action<TSetter, TValue> set

            )
            where TGetter : class, ISkyrimMajorRecordGetter, IBinaryItem
            where TSetter : class, ISkyrimMajorRecordInternal, IBinaryItem, IMajorRecordInternal, TGetter
            where TValue : IEquatable<TValue>
        {
            var stringValue = value.ToString();
            var parsedValue = parse(stringValue);
            if (!getValue(input.Item).Equals(stringValue))
            {
                var newRecord = input.Setter.GetOrAddAsOverride(input.Item);
                set(newRecord, parsedValue);
            }
        }

        static void apply<TGetter, TSetter, TValue>(ModificationResult<TGetter, TSetter> input, object value,

    Func<string, TValue> parse,
    Action<TSetter, TValue> set

    )
    where TGetter : class, ISkyrimMajorRecordGetter, IBinaryItem
    where TSetter : class, ISkyrimMajorRecordInternal, IBinaryItem, IMajorRecordInternal, TGetter
    where TValue : IEquatable<TValue>
        {
            var stringValue = value.ToString();
            var parsedValue = parse(stringValue);

            var newRecord = input.Setter.GetOrAddAsOverride(input.Item);
            set(newRecord, parsedValue);
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
                if (GlobalVariable(i)) return true;
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
                var prop = (string)path[i + 1];

                var data = getData(
                    chosenPlugin,
                    patch,
                    x => x.LeveledItems,
                    x => x.LeveledItems,
                    x => x.Records.FirstOrDefault(x => x.EditorID == element)
                );

                if (data == null) return false;

                switch (prop)
                {
                    case "ChanceNone":
                        apply(
                            data,
                            value,
                                byte.Parse,
                                x => x.ChanceNone,
                            (x, y) => x.ChanceNone = y
                        );
                        break;
                    case "Entries":
                        Entry(data, i + 1);
                        break;
                }
                return true;

            }
            bool GlobalVariable(int i)
            {
                var element = (string)path[i];

                var data = getData(
                    chosenPlugin,
                    patch,
                    x => x.Globals,
                    x => x.Globals,
                    x => x.Records.FirstOrDefault(x => x.EditorID == element)
                );

                if (data == null) return false;

                var prop = (string)path[i + 1];

                switch (prop)
                {
                    case "Value":
                        apply(
                                data, 
                                value,
                                float.Parse,
                            (x, y) => x.RawFloat= y
                        );
                        break;
                }
                return true;

            }
            bool MiscItem(int i)
            {
                var element = (string)path[i];

                var data = getData(
                    chosenPlugin,
                    patch,
                    x => x.MiscItems,
                    x => x.MiscItems,
                    x => x.Records.FirstOrDefault(x => x.EditorID == element)
                );

                if (data == null) return false;


                var prop = (string)path[i + 1];
                switch (prop)
                {
                    case "GoldValue":

                        apply(
                            data,
                            value,
                                uint.Parse,
                                x => x.Value,
                            (x, y) => x.Value = y
                        );
                        break;
                    case "Weight":
                        apply(
                            data,
                            value,
                                float.Parse,
                                x => x.Weight,
                            (x, y) => x.Weight = y
                        );
                        break;
                    case "Name":
                        apply(
                            data,
                            value,
                                x=>x,
                                x => x.Name?.ToString()??"",
                            (x, y) => x.Name = y
                        );
                        break;
                }
                return true;
            }

            

            bool Key(int i)
            {
                var element = (string)path[i];

                var data = getData(
                    chosenPlugin,
                    patch,
                    x => x.Keys,
                    x => x.Keys,
                    x => x.Records.FirstOrDefault(x => x.EditorID == element)
                );

                if (data == null) return false;

                var prop = (string)path[i + 1];

                switch (prop)
                {
                    case "GoldValue":
                        apply(
                            data,
                            value,
                                uint.Parse,
                                x => x.Value,
                            (x, y) => x.Value = y
                        );
                        break;
                    case "Weight":
                        apply(
                            data,
                            value,
                                float.Parse,
                                x => x.Weight,
                            (x, y) => x.Weight = y
                        );
                        break;
                    case "Name":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.Name?.ToString() ?? "",
                            (x, y) => x.Name = y
                        );
                        break;
                }
                return true;

            }
            bool Book(int i)
            {
                var element = (string)path[i];

                var data = getData(
                    chosenPlugin,
                    patch,
                    x => x.Books,
                    x => x.Books,
                    x => x.Records.FirstOrDefault(x => x.EditorID == element)
                );

                if (data == null) return false;

                var prop = (string)path[i + 1];

                switch (prop)
                {
                    case "GoldValue":
                        apply(
                            data,
                            value,
                                uint.Parse,
                                x => x.Value,
                            (x, y) => x.Value = y
                        );
                        break;
                    case "Weight":
                        apply(
                            data,
                            value,
                                float.Parse,
                                x => x.Weight,
                            (x, y) => x.Weight = y
                        );
                        break;
                    case "Name":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.Name?.ToString() ?? "",
                            (x, y) => x.Name = y
                        );
                        break;
                    case "BookText":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.BookText?.ToString() ?? "",
                            (x, y) => x.BookText = y
                        );
                        break;
                    case "Description":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.Description?.ToString() ?? "",
                            (x, y) => x.Description = y
                        );
                        break;
                }
                return true;

            }
            bool Ingredient(int i)
            {
                var element = (string)path[i];

                var data = getData(
                    chosenPlugin,
                    patch,
                    x => x.Ingredients,
                    x => x.Ingredients,
                    x => x.Records.FirstOrDefault(x => x.EditorID == element)
                );

                if (data == null) return false;

                var prop = (string)path[i + 1];
                switch (prop)
                {
                    case "GoldValue":
                        apply(
                            data,
                            value,
                                uint.Parse,
                                x => x.Value,
                            (x, y) => x.Value = y
                        );
                        break;
                    case "Weight":
                        apply(
                            data,
                            value,
                                float.Parse,
                                x => x.Weight,
                            (x, y) => x.Weight = y
                        );
                        break;
                    case "Name":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.Name?.ToString() ?? "",
                            (x, y) => x.Name = y
                        );
                        break;
                    case "Effects":
                        EffectIngredient(data, i + 1);
                        break;
                }
                return true;

            }
            bool Weapon(int i)
            {
                var element = (string)path[i];

                var data = getData(
                    chosenPlugin,
                    patch,
                    x => x.Weapons,
                    x => x.Weapons,
                    x => x.Records.FirstOrDefault(x => x.EditorID == element)
                );

                if (data == null) return false;

                var prop = (string)path[i + 1];

                switch (prop)
                {
                    case "GoldValue":
                        apply(
                            data,
                            value,
                                uint.Parse,
                                x => x.BasicStats.Value,
                            (x, y) => x.BasicStats.Value = y
                        );
                        break;
                    case "Weight":
                        apply(
                            data,
                            value,
                                float.Parse,
                                x => x.BasicStats.Weight,
                            (x, y) => x.BasicStats.Weight = y
                        );
                        break;
                    case "Name":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.Name?.ToString() ?? "",
                            (x, y) => x.Name = y
                        );
                        break;
                    case "Damge":
                        apply(
                            data,
                            value,
                                ushort.Parse,
                                x => x.BasicStats.Damage,
                            (x, y) => x.BasicStats.Damage = y
                        );
                        break;
                    case "Speed":
                        apply(
                            data,
                            value,
                                float.Parse,
                                x => x.Data.Speed,
                            (x, y) => x.Data.Speed = y
                        );
                        break;
                    case "Reach":
                        apply(
                            data,
                            value,
                                float.Parse,
                                x => x.Data.Reach,
                            (x, y) => x.Data.Reach = y
                        );
                        break;
                    case "Description":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.Description?.ToString() ?? "",
                            (x, y) => x.Description = y
                        );
                        break;
                }
                return true;
            }

            bool Ammunitions(int i)
            {
                var element = (string)path[i];


                var data = getData(
                    chosenPlugin,
                    patch,
                    x => x.Ammunitions,
                    x => x.Ammunitions,
                    x => x.Records.FirstOrDefault(x => x.EditorID == element)
                );

                if (data == null) return false;

                var prop = (string)path[i + 1];

                switch (prop)
                {
                    case "GoldValue":
                        apply(
                            data,
                            value,
                                uint.Parse,
                                x => x.Value,
                            (x, y) => x.Value = y
                        );
                        break;
                    case "Weight":
                        apply(
                            data,
                            value,
                                float.Parse,
                                x => x.Weight,
                            (x, y) => x.Weight = y
                        );
                        break;
                    case "Name":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.Name?.ToString() ?? "",
                            (x, y) => x.Name = y
                        );
                        break;
                    case "Damge":
                        apply(
                            data,
                            value,
                                float.Parse,
                                x => x.Damage,
                            (x, y) => x.Damage = y
                        );
                        break;
                    case "Description":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.Description.ToString(),
                            (x, y) => x.Description = y
                        );
                        break;
                }
                return true;

            }
            bool Armor(int i)
            {
                var element = (string)path[i];

                var data = getData(
                    chosenPlugin,
                    patch,
                    x => x.Armors,
                    x => x.Armors,
                    x => x.Records.FirstOrDefault(x => x.EditorID == element)
                );

                if (data == null) return false;


                var prop = (string)path[i + 1];

                switch (prop)
                {
                    case "GoldValue":
                        apply(
                            data,
                            value,
                                uint.Parse,
                                x => x.Value,
                            (x, y) => x.Value = y
                        );
                        break;
                    case "Weight":
                        apply(
                            data,
                            value,
                                float.Parse,
                                x => x.Weight,
                            (x, y) => x.Weight = y
                        );
                        break;
                    case "Name":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.Name?.ToString() ?? "",
                            (x, y) => x.Name = y
                        );
                        break;
                    case "ArmorRating":
                        apply(
                            data,
                            value,
                                float.Parse,
                                x => x.ArmorRating,
                            (x, y) => x.ArmorRating = y
                        );
                        break;
                    case "Description":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.Description?.ToString() ?? "",
                            (x, y) => x.Description = y
                        );
                    break;
                }
                return true;

            }
            bool Spell(int i)
            {
                var element = (string)path[i];


                var data = getData(
                    chosenPlugin,
                    patch,
                    x => x.Spells,
                    x => x.Spells,
                    x => x.Records.FirstOrDefault(x => x.EditorID == element)
                );

                if (data == null) return false;


                var prop = (string)path[i + 1];

                switch (prop)
                {
                    case "Description":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.Description?.ToString() ?? "",
                            (x, y) => x.Description = y
                        );
                        break;
                    case "Effects":
                        EffectSpell(data, i + 1);
                        break;
                    case "Name":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.Name?.ToString() ?? "",
                            (x, y) => x.Name = y
                        );
                        break;
                }
                return true;
            }
            bool Shouts(int i)
            {
                var element = (string)path[i];

                var data = getData(
                    chosenPlugin,
                    patch,
                    x => x.Shouts,
                    x => x.Shouts,
                    x => x.Records.FirstOrDefault(x => x.EditorID == element)
                );

                if (data == null) return false;


                var prop = (string)path[i + 1];

                switch (prop)
                {
                    case "Description":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.Description?.ToString() ?? "",
                            (x, y) => x.Description = y
                        );
                        break;
                    case "WordsOfPower":
                        WordsOfPower(data, i + 1);
                        break;
                    case "Name":
                        apply(
                            data,
                            value,
                                x => x,
                                x => x.Name?.ToString() ?? "",
                            (x, y) => x.Name = y
                        );
                        break;
                }
                return true;
            }
            void WordsOfPower(ModificationResult<IShoutGetter, Mutagen.Bethesda.Skyrim.Shout> data, int i)
            {
                var id = (int)path[i + 1];
                var prop = (string)path[i + 2];

                if (id >= data.Item.WordsOfPower.Count)
                {
                    throw new Exception($"Index \"{id}\" is outside of \"{path[i]}\" bounds in expression \"{expression}\"");
                }


                switch (prop)
                {
                    case "RecoveryTime":
                        apply(
                            data,
                            value,
                            float.Parse,
                            x => x.WordsOfPower[id].RecoveryTime,
                            (x, y) => x.WordsOfPower[id].RecoveryTime = y
                        );
                        break;
                }
            }

            bool ConstructibleObjects(int i)
            {
                var element = (string)path[i];

                var data = getData(
                    chosenPlugin,
                    patch,
                    x => x.ConstructibleObjects,
                    x => x.ConstructibleObjects,
                    x => x.Records.FirstOrDefault(x => x.EditorID == element)
                );

                if (data == null) return false;

                var prop = (string)path[i + 1];

                switch (prop)
                {
                    case "Items":
                        ConstructibleObjectsItem(data, i + 1);
                        break;
                }
                return true;
            }
            void ConstructibleObjectsItem(ModificationResult<IConstructibleObjectGetter, ConstructibleObject> data, int i)
            {
                var id = (int)path[i + 1];
                var prop = (string)path[i + 2];

                if (data.Item.Items == null || id >= data.Item.Items.Count)
                {
                    throw new Exception($"Index \"{id}\" is outside of \"{path[i]}\" bounds in expression \"{expression}\"");
                }

                var copy = data.Item.Items[id];

                if (copy == null) return;

                switch (prop)
                {
                    case "Count":
                        apply(
                            data,
                            value,
                            float.Parse,
                            x => x.Items[id].Item.Count,
                            (x, y) => x.Items[id].Item.Count = y
                        );
                        break;
                }
            }

            void EffectIngredient(ModificationResult<IIngredientGetter, Ingredient> data, int i)
            {
                var id = (int)path[i+1];
                var prop = (string)path[i + 2];

                if (id >= data.Item.Effects.Count)
                {
                    throw new Exception($"Index \"{id}\" is outside of \"{path[i]}\" bounds in expression \"{expression}\"");
                }

                switch (prop)
                {
                    case "Magnitude":
                        apply(
                            data,
                            value,
                            float.Parse,
                            x => x.Effects[id].Data.Magnitude,
                            (x, y) => x.Effects[id].Data.Magnitude = y
                        );
                        break;
                    case "Area":
                        apply(
                            data,
                            value,
                            int.Parse,
                            x => x.Effects[id].Data.Area,
                            (x, y) => x.Effects[id].Data.Area = y
                        );
                        break;
                    case "Duration":
                        apply(
                            data,
                            value,
                            int.Parse,
                            x => x.Effects[id].Data.Duration,
                            (x, y) => x.Effects[id].Data.Duration = y
                        );
                        break;
                }
            }
            void EffectSpell(ModificationResult<ISpellGetter, Mutagen.Bethesda.Skyrim.Spell> data, int i)
            {
                var id = (int)path[i + 1];
                var prop = (string)path[i + 2];

                if (id >= data.Item.Effects.Count)
                {
                    throw new Exception($"Index \"{id}\" is outside of \"{path[i]}\" bounds in expression \"{expression}\"");
                }

                switch (prop)
                {
                    case "Magnitude":
                        apply(
                            data,
                            value,
                            float.Parse,
                            x => x.Effects[id].Data.Magnitude,
                            (x, y) => x.Effects[id].Data.Magnitude = y
                        );
                        break;
                    case "Area":
                        apply(
                            data,
                            value,
                            int.Parse,
                            x => x.Effects[id].Data.Area,
                            (x, y) => x.Effects[id].Data.Area = y
                        );
                        break;
                    case "Duration":
                        apply(
                            data,
                            value,
                            int.Parse,
                            x => x.Effects[id].Data.Duration,
                            (x, y) => x.Effects[id].Data.Duration = y
                        );
                        break;
                }
            }
            void Entry(ModificationResult<ILeveledItemGetter, LeveledItem> data, int i)
            {
                var id = (int)path[i + 1];
                var prop = (string)path[i + 2];

                if (data.Item.Entries == null || id >= data.Item.Entries.Count)
                {
                    throw new Exception($"Index \"{id}\" is outside of \"{path[i]}\" bounds in expression \"{expression}\"");
                }


                switch (prop)
                {
                    case "Count":
                        apply(
                            data, 
                            value,
                            short.Parse, 
                            x => x.Entries[id].Data.Count, 
                            (x, y) => x.Entries[id].Data.Count = y
                        );
                        break;
                    case "Level":
                        apply(
                            data, 
                            value,
                            short.Parse, 
                            x => x.Entries[id].Data.Level, 
                            (x, y) => x.Entries[id].Data.Level = y
                        );
                        break;
                }
            }
            bool FormLists(int i)
            {
                var element = (string)path[i];


                var data = getData(
                    chosenPlugin,
                    patch,
                    x => x.FormLists,
                    x => x.FormLists,
                    x => x.Records.FirstOrDefault(x => x.EditorID == element)
                );

                if (data == null) return false;

                var prop = (string)path[i + 1];
                switch (prop)
                {
                    case "Items":
                        FormListsItem(copy.Items, i + 1);
                        break;
                }
                return true;
            }
            void FormListsItem(ModificationResult<IFormListGetter, FormList> data, int i)
            {
                var id = (int)path[i + 1];
                var prop = (string)path[i + 2];

                if(data.Item == null || id >= data.Item.Items.Count)
                {
                    throw new Exception($"Index \"{id}\" is outside of \"{path[i]}\" bounds in expression \"{expression}\"");
                }

                var copy = data.Item.Items[id];


                if (copy == null) return;

                var name = copy.FormKey.IDString() + "-" + copy.FormKey.ModKey;

                switch (prop)
                {
                    case "Repeat":
                        var count = int.Parse(value.ToString());
                        if (count > 1)
                        {
                            data.Setter.Remove(copy.FormKey);
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
                                data.Setter.Add(itemListPlaceholder);
                            }
                        }
                        break;
                }
            }
        }
        static Dictionary<string, FormList> createdAlias = new();
    }
}
