using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace Wrapper
{
    public static class Modification
    {
        static Dictionary<string, ISkyrimModDisposableGetter> ModGetterSingleton = [];
        public static ISkyrimModDisposableGetter Read(string name)
        {
            if (ModGetterSingleton.TryGetValue(name, out var result))
            {
                return result;
            }
            var key = ModKey.FromNameAndExtension(name);
            var newResult = SkyrimMod.CreateFromBinaryOverlay(new ModPath(key, "../"+name), SkyrimRelease.SkyrimSE);
            ModGetterSingleton[name] = newResult;
            return newResult;
        }
        public static SkyrimMod Patch(string outputName)
        {
            return new SkyrimMod(ModKey.FromNameAndExtension(outputName), SkyrimRelease.SkyrimSE);
        }

        public static void Save(this SkyrimMod mod, string name)
        {
            mod.WriteToBinary("../"+name);
        }
        public static void Flush()
        {
            var data = ModGetterSingleton;
            ModGetterSingleton = [];
            foreach (var item in data)
            {
                item.Value.Dispose();
            }
        }
    }

}
