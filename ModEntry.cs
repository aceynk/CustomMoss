using GenericModConfigMenu;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Minigames;

namespace CustomMoss;

public class ModEntry : Mod
{
    public static void Log(string v)
    {
        _log.Log(v, LogLevel.Debug);
    }

    public static IMonitor _log = null!;
    public static ModConfig Config;

    public override void Entry(IModHelper helper)
    {
        Config = Helper.ReadConfig<ModConfig>();
        _log = Monitor;

        var harmony = new Harmony(ModManifest.UniqueID);

        Helper.Events.Content.AssetRequested += OnAssetRequested;
        Helper.Events.GameLoop.GameLaunched += InitConfig;
        
        harmony.PatchAll();
    }
    
    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        
        /*
         * "aceynk.CustomMoss/Trees" like:
         *
         * {
         *      "MossId": {
         *          "DropWhenShaken": <bool>,
         *          "ValidSeasons": <GSQ>,                  [Implemented]
         *          "ValidTrees": <list<string>>,           [Implemented]
         *          "MaxAmount": <int>,                     [Implemented]
         *          "MinAmount": <int>,                     [Implemented]
         *          "TextureOak": <texture fp>,             [Implemented]
         *          "TextureMaple": <asset>,                [Implemented]
         *          "TexturePine": <asset>,                 [Implemented]
         *          "Texture1": <asset>,                    [Implemented]
         *          "Texture2": <asset>,                    [Implemented]
         *          "Chance": <int>,                        [Implemented]
         *          "Experience": <int>                     [Implemented]
         *      }
         * }
         */
        
        if (e.Name.IsEquivalentTo("aceynk.CustomMoss/Tree"))
        {
            e.LoadFrom(() => new Dictionary<string, Dictionary<string, string>>(), AssetLoadPriority.High);
        }

        if (e.Name.IsEquivalentTo("aceynk.CustomMoss/Stone"))
        {
            e.LoadFrom(() => new Dictionary<string, Dictionary<string, string>>(), AssetLoadPriority.High);
        }
    }

    private void InitConfig(object? sender, GameLaunchedEventArgs e)
    {
        var menu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

        if (menu is null)
        {
            return;
        }
        
        menu.Register(
            mod: ModManifest,
            reset: () => Config = new ModConfig(),
            save: () => Helper.WriteConfig(Config)
        );
        
        menu.AddSectionTitle(
            mod: ModManifest,
            text: () => Helper.Translation.Get("GMCM.MainTitle")
        );
        
        menu.AddBoolOption(
            mod: ModManifest,
            name: () => Helper.Translation.Get("GMCM.VanillaMossOverrides.Name"),
            tooltip: () => Helper.Translation.Get("GMCM.VanillaMossOverrides.Desc"),
            getValue: () => Config.VanillaMossOverrides,
            setValue: v => Config.VanillaMossOverrides = v
        );
    }
}