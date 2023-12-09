using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Binary.Headers;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Wrapper;
using System.Collections;
using Wrapper;
using static Mutagen.Bethesda.Skyrim.SkyrimModHeader;


using var master = Modification.Read("Mythic Items.esp");

using var original = Modification.Read("Mythic Items Vendor.esp");
var patch = Modification.Patch("Mythic items Vendor - patch.esp");

//patch.ModHeader.Flags |= HeaderFlag.LightMaster;
//var il = original.FormLists.Records.First();
//var li = patch.FormLists.GetOrAddAsOverride(il);

var item = patch.LeveledItems.AddNew("TestItem");
item.ChanceNone = 0;

var nii = new LeveledItemEntry() { Data = new() { Reference = master.MiscItems.First().ToLink() } };
item.Entries = new Noggog.ExtendedList<LeveledItemEntry> { nii };

foreach (var misc in original.LeveledItems.Records)
{
    var miscCopy = patch.LeveledItems.GetOrAddAsOverride(misc);


    var ni = new LeveledItemEntry() { Data = new() { Reference = master.MiscItems.First().ToLink() } };
    miscCopy.Entries?.Add(ni);

    miscCopy.ChanceNone = 100;

    var first = miscCopy.Entries?.FirstOrDefault()?.Data;

    if (first != null)
    {
        first.Level = 10;
    }

    Console.WriteLine($"Modified Name: {misc.EditorID} {misc.ChanceNone} - {miscCopy.ChanceNone}");
}

patch.Save("Mythic items Vendor - patch.esp");
