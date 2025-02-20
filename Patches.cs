using System.Reflection.Emit;
using System.Text.Json;
using System.Text.RegularExpressions;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace CustomMoss;

public class helper
{
    public static Dictionary<string, string> DecodeModData(string content)
    {
        Dictionary<string, string>? desContent = JsonSerializer.Deserialize<Dictionary<string, string>>(content);

        return desContent ?? new Dictionary<string, string>();
    }

    public static NetString EncodeModData(Dictionary<string, string> content)
    {
        string output = JsonSerializer.Serialize(content);

        return new NetString(output);
    }

    public static List<string> ParseAsList(string content)
    {
        return Regex.Split(content, ", +").ToList();
    }
}


/////////////////
////  TREES  ////
/////////////////


[HarmonyPatch(typeof(Tree), nameof(Tree.dayUpdate))]
public class Tree_dayUpdate_Patches
{
    private static ModConfig Config = ModEntry.Config;
    
    public static void Postfix(Tree __instance)
    {
        string treeUID = "aceynk.CustomMoss/Tree";
        Random rnd = Random.Shared;
        Dictionary<string, Dictionary<string, string>> mossData = Game1.content.Load<Dictionary<string, Dictionary<string, string>>>(treeUID);

        if (!__instance.modData.ContainsKey(treeUID))
        {
            __instance.modData[treeUID] = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                {"current_moss", "null"}
            });
        }
        
        Dictionary<string, string> modDict = helper.DecodeModData(__instance.modData[treeUID]);
        
        /*
         * modDict looks like:
         *
         * {
         *      "current_moss": <moss id>
         * }
         */

        // checks //
        
        if (modDict.Keys.Count == 0)
        {
            modDict["current_moss"] = "null";
        }

        if (__instance.hasMoss.Value)
        {
            if (!Config.VanillaMossOverrides)
            {
                __instance.hasMoss.Value = modDict["current_moss"] == "null";
            }
            else
            {
                modDict["current_moss"] = "null";
            }
        }


        if (modDict["current_moss"] != "null" && mossData[modDict["current_moss"]].ContainsKey("GrowConditions") &&
            !GameStateQuery.CheckConditions(mossData[modDict["current_moss"]]["GrowConditions"], Game1.currentLocation,
                Game1.player)) 
        {
            modDict["current_moss"] = "null";
        }

        if (modDict["current_moss"] != "null" && mossData[modDict["current_moss"]].ContainsKey("ValidTrees") &&
            !helper.ParseAsList(mossData[modDict["current_moss"]]["ValidTrees"]).Contains(__instance.treeType.Value)) 
        {
            modDict["current_moss"] = "null";
        }
        
        // process moss data //

        foreach (string mossId in mossData.Keys)
        {
            if (modDict["current_moss"] != "null") break;

            if (mossData[mossId].ContainsKey("GrowConditions") &&
                !GameStateQuery.CheckConditions(mossData[mossId]["GrowConditions"]))  
            {
                continue;
            }

            if (mossData[mossId].ContainsKey("ValidTrees") && !helper
                    .ParseAsList(mossData[mossId]["ValidTrees"])
                    .Contains(__instance.treeType.Value)) 
            {
                continue;
            }

            if (rnd.NextBool(float.Parse(mossData[mossId]["Chance"])))
            {
                modDict["current_moss"] = mossId;
                if (!Config.VanillaMossOverrides)
                {
                    __instance.hasMoss.Value = false;
                }
            }
        }
        
        __instance.modData[treeUID] = helper.EncodeModData(modDict);
    }
}

[HarmonyPatch(typeof(Tree), nameof(Tree.performToolAction))]
public class Tree_performToolAction_Patches
{
    public static void Prefix(Tool t, int explosion, Vector2 tileLocation, Tree __instance)
    {
        string treeUID = "aceynk.CustomMoss/Tree";
        
        if (!__instance.modData.ContainsKey(treeUID))
        {
            __instance.modData[treeUID] = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                {"current_moss", "null"}
            });
        }
        
        Dictionary<string, string> modDict = helper.DecodeModData(__instance.modData[treeUID]);
        Dictionary<string, Dictionary<string, string>> mossData = Game1.content.Load<Dictionary<string, Dictionary<string, string>>>("aceynk.CustomMoss/Tree");
        Random rnd = Random.Shared;
        
        if (__instance.growthStage.Value >= 5)
        {
            if (!mossData.ContainsKey(modDict["current_moss"]))
            {
                modDict["current_moss"] = "null";
            }
            
            if (modDict["current_moss"] != "null")
            {
                string mossId = modDict["current_moss"];

                int amountBonus = 0;

                if (ModEntry.ScytheAPI != null
                    && ModEntry.ScytheAPI.HasGathererEnchantment(t)
                    && Random.Shared.NextBool())
                {
                    amountBonus = 1;
                }
                
                Item outItem = ItemRegistry.Create(
                    mossId, 
                    amount: rnd.Next(
                        int.Parse(mossData[mossId]["MinAmount"]), 
                        int.Parse(mossData[mossId]["MaxAmount"]) + 1
                    ) + amountBonus
                );

                // modified from decompiled game
                if (t is not null && t.getLastFarmerToUse() is not null)
                {
                    t.getLastFarmerToUse().gainExperience(Farmer.foragingSkill, outItem.Stack * int.Parse(mossData[mossId]["Experience"]));
                }
                
                // modified from decompiled game vv
                GameLocation location = __instance.Location ?? Game1.currentLocation;
                Vector2 itemDebrisVector = new Vector2(tileLocation.X, tileLocation.Y - 1f) * 64f;

                Game1.createMultipleItemDebris(outItem, itemDebrisVector, -1, location,
                    Game1.player.StandingPixel.Y - 32);

                modDict["current_moss"] = "null";

                // modified from decompiled game vvvv
                Game1.stats.Increment("mossHarvested");
                __instance.shake(tileLocation, doEvenIfStillShaking: true);
                __instance.growthStage.Value = 9;
                Game1.playSound("moss_cut");

                __instance.modData[treeUID] = helper.EncodeModData(modDict);
            }
        }
    }
}

[HarmonyPatch(typeof(Tree), nameof(Tree.draw))]
public class Tree_draw_Patches
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher cMatcher = new CodeMatcher(instructions);

        cMatcher.MatchEndForward(
                new CodeMatch(OpCodes.Add),
                new CodeMatch(OpCodes.Ldc_R4, 10000f),
                new CodeMatch(OpCodes.Div),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Vector2), nameof(Vector2.X))),
                new CodeMatch(OpCodes.Ldc_R4, 1000000f),
                new CodeMatch(OpCodes.Div),
                new CodeMatch(OpCodes.Sub)
            )
            .ThrowIfNotMatch("Couldn't find transpiler start position for Tree.draw. (main)")
            .Advance(1)
            .RemoveInstruction()
            .Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(TranspilerSupplementary), nameof(TranspilerSupplementary.TreeDraw)))
            );

        cMatcher.MatchEndForward(
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Ldloc_2),
                new CodeMatch(OpCodes.Ldc_R4, 10000f),
                new CodeMatch(OpCodes.Div)
            )
            .ThrowIfNotMatch("Couldn't find transpiler start position for Tree.draw. (trunk #1)")
            .Advance(1)
            .RemoveInstruction()
            .Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(TranspilerSupplementary), nameof(TranspilerSupplementary.TreeDraw)))
            );

        return cMatcher.InstructionEnumeration();
    }
}

public class TranspilerSupplementary
{
    public static void TreeDraw(SpriteBatch spriteBatch, Texture2D textureValue, Vector2 position,
        Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects,
        float layerDepth, Tree curInstance)
    {
        string treeUID = "aceynk.CustomMoss/Tree";

        if (curInstance.modData.ContainsKey(treeUID))
        {
            Dictionary<string, string> modDict = helper.DecodeModData(curInstance.modData[treeUID]);
            Dictionary<string, Dictionary<string, string>> mossData =
                Game1.content.Load<Dictionary<string, Dictionary<string, string>>>(treeUID);

            void dealWithRectangle()
            {
                if (sourceRectangle is not null)
                {
                    Rectangle tempRect = (Rectangle)sourceRectangle;
                    if (Game1.currentSeason != "winter")
                    {
                        tempRect.X += 96;
                    }
                    sourceRectangle = tempRect;
                }
            }

            void evalTreeTexture(string textureKey)
            {
                if (mossData[modDict["current_moss"]].ContainsKey(textureKey))
                {
                    try
                    {
                        textureValue = Game1.content.Load<Texture2D>(mossData[modDict["current_moss"]][textureKey]);
                    }
                    catch
                    {
                        ModEntry._log.Log($"Failed to load the {textureKey} asset. Defaulting to vanilla sprites if possible.");
                        dealWithRectangle();
                    }
                }
                else
                {
                    dealWithRectangle();
                }
            }
            
            if (!mossData.ContainsKey(modDict["current_moss"]))
            {
                modDict["current_moss"] = "null";
            }

            if (modDict["current_moss"] != "null")
            {
                switch (curInstance.treeType.Value)
                {
                    case Tree.bushyTree: // Oak
                        evalTreeTexture("TextureOak");
                        break;
                    case Tree.leafyTree: // Maple
                        evalTreeTexture("TextureMaple");
                        break;
                    case Tree.pineTree: // Pine
                        evalTreeTexture("TexturePine");
                        break;
                    case Tree.greenRainTreeBushy: // Green Rain Type 1
                        evalTreeTexture("Texture1");
                        break;
                    case Tree.greenRainTreeLeafy: // Green Rain Type 2
                        evalTreeTexture("Texture2");
                        break;
                    default:
                        if (mossData[modDict["custom_moss"]].ContainsKey("Texture" + curInstance.treeType.Value))
                        {
                            evalTreeTexture("Texture" + curInstance.treeType.Value);
                        }
                        else dealWithRectangle();
                        break;
                }
            }
        }

        spriteBatch.Draw(textureValue, position, sourceRectangle, color, rotation, origin, scale, effects,
                layerDepth);
    }

    public static bool MushroomLog_hasMoss(Tree t)
    {
        string treeUID = "aceynk.CustomMoss/Tree";

        if (t.modData.ContainsKey(treeUID))
        {
            Dictionary<string, string> modDict = helper.DecodeModData(t.modData[treeUID]);

            return modDict["current_moss"] != "null";
        }

        return t.hasMoss.Value;
    }
}


//////////////////
////  STONES  ////
//////////////////


[HarmonyPatch(typeof(Object), nameof(Object.DayUpdate))]
public class Object_dayUpdate_Patches
{
    public static void Postfix(Object __instance)
    {
        string stoneUID = "aceynk.CustomMoss/Stone";
        Random rnd = Random.Shared;
        Dictionary<string, Dictionary<string, string>> mossData =
            Game1.content.Load<Dictionary<string, Dictionary<string, string>>>(stoneUID);
            
        if (!__instance.modData.ContainsKey(stoneUID))
        {
            __instance.modData[stoneUID] = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                {"current_moss", "null"}
            });
        }
        
        Dictionary<string, string> modDict = helper.DecodeModData(__instance.modData[stoneUID]);
            
        if (__instance.QualifiedItemId == "(O)343" || __instance.QualifiedItemId == "(O)450")
        {
            if (modDict["current_moss"] != "null")
            {
                if (mossData[modDict["current_moss"]].ContainsKey("GrowConditions") &&
                    !GameStateQuery.CheckConditions(mossData[modDict["current_moss"]]["GrowConditions"])) 
                {
                    modDict["current_moss"] = "null";
                }
            }

            foreach (string mossId in mossData.Keys)
            {
                if (modDict["current_moss"] != "null") break;

                if (mossData[mossId].ContainsKey("GrowConditions") && !helper
                        .ParseAsList(mossData[mossId]["GrowConditions"]).Contains(Game1.currentSeason)) 
                {
                    continue;
                }

                if (rnd.NextBool(float.Parse(mossData[mossId]["Chance"])))
                {
                    modDict["current_moss"] = mossId;
                }
            }
        }
        
        __instance.modData[stoneUID] = helper.EncodeModData(modDict);
    }
}

[HarmonyPatch(typeof(Object), nameof(Object.performToolAction))]
public class Object_performToolAction_Patches
{
    public static void Prefix(Tool t, Object __instance)
    {
        string stoneUID = "aceynk.CustomMoss/Stone";
        
        if (!__instance.modData.ContainsKey(stoneUID))
        {
            __instance.modData[stoneUID] = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                {"current_moss", "null"}
            });
        }
        
        Dictionary<string, string> modDict = helper.DecodeModData(__instance.modData[stoneUID]);
        Dictionary<string, Dictionary<string, string>> mossData = Game1.content.Load<Dictionary<string, Dictionary<string, string>>>(stoneUID);
        Random rnd = Random.Shared;
        
        if (!mossData.ContainsKey(modDict["current_moss"]))
        {
            modDict["current_moss"] = "null";
        }
        
        if (modDict["current_moss"] != "null")
        {
            string mossId = modDict["current_moss"];
            
            int amountBonus = 0;

            if (ModEntry.ScytheAPI != null
                && ModEntry.ScytheAPI.HasGathererEnchantment(t)
                && Random.Shared.NextBool())
            {
                amountBonus = 1;
            }
            
            Item outItem = ItemRegistry.Create(
                mossId, 
                amount: rnd.Next(
                    int.Parse(mossData[mossId]["MinAmount"]), 
                    int.Parse(mossData[mossId]["MaxAmount"]) + 1
                ) + amountBonus
            );

            // modified from decompiled game
            if (t is not null && t.getLastFarmerToUse() is not null)
            {
                t.getLastFarmerToUse().gainExperience(Farmer.foragingSkill, outItem.Stack * int.Parse(mossData[mossId]["Experience"]));
            }
                
            // modified from decompiled game vv
            GameLocation location = __instance.Location ?? Game1.currentLocation;
            Vector2 itemDebrisVector = new Vector2(__instance.TileLocation.X, __instance.TileLocation.Y - 1f) * 64f;

            Game1.createMultipleItemDebris(outItem, itemDebrisVector, -1, location,
                Game1.player.StandingPixel.Y - 32);

            modDict["current_moss"] = "null";

            // modified from decompiled game vvvv
            Game1.stats.Increment("mossHarvested");
            Game1.playSound("moss_cut");

            __instance.modData[stoneUID] = helper.EncodeModData(modDict);
        }
    }
}

[HarmonyPatch(typeof(Object), nameof(Object.performRemoveAction))]
public class Object_performRemoveAction_Patches
{
    public static void Prefix(Object __instance)
    {
        string stoneUID = "aceynk.CustomMoss/Stone";
        
        if (!__instance.modData.ContainsKey(stoneUID))
        {
            __instance.modData[stoneUID] = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                {"current_moss", "null"}
            });
        }
        
        Dictionary<string, string> modDict = helper.DecodeModData(__instance.modData[stoneUID]);
        Dictionary<string, Dictionary<string, string>> mossData = Game1.content.Load<Dictionary<string, Dictionary<string, string>>>(stoneUID);
        Random rnd = Random.Shared;
        
        if (!mossData.ContainsKey(modDict["current_moss"]))
        {
            modDict["current_moss"] = "null";
        }
        
        if (modDict["current_moss"] != "null")
        {
            string mossId = modDict["current_moss"];
            
            int amountBonus = 0;

            if (ModEntry.ScytheAPI != null
                && ModEntry.ScytheAPI.HasGathererEnchantment(Game1.player.CurrentTool)
                && Random.Shared.NextBool())
            {
                amountBonus = 1;
            }
            
            Item outItem = ItemRegistry.Create(
                mossId, 
                amount: rnd.Next(
                    int.Parse(mossData[mossId]["MinAmount"]), 
                    int.Parse(mossData[mossId]["MaxAmount"]) + 1
                ) + amountBonus
            );
            
            Game1.player.gainExperience(Farmer.foragingSkill, outItem.Stack * int.Parse(mossData[mossId]["Experience"]));
            
                
            // modified from decompiled game vv
            GameLocation location = __instance.Location ?? Game1.currentLocation;
            Vector2 itemDebrisVector = new Vector2(__instance.TileLocation.X, __instance.TileLocation.Y - 1f) * 64f;

            Game1.createMultipleItemDebris(outItem, itemDebrisVector, -1, location,
                Game1.player.StandingPixel.Y - 32);

            modDict["current_moss"] = "null";

            // modified from decompiled game vvvv
            Game1.stats.Increment("mossHarvested");
            Game1.playSound("moss_cut");

            __instance.modData[stoneUID] = helper.EncodeModData(modDict);
        }
    }
}

[HarmonyPatch(typeof(Object), nameof(Object.draw), new[] {typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)})]
public class Object_draw_Patches
{
    public static void Postfix(SpriteBatch spriteBatch, int x, int y, float alpha, Object __instance)
    {
        string stoneUID = "aceynk.CustomMoss/Stone";
        if (!__instance.modData.ContainsKey(stoneUID)) return;
        
        Dictionary<string, string> modDict = helper.DecodeModData(__instance.modData[stoneUID]);
        Dictionary<string, Dictionary<string, string>> mossData =
            Game1.content.Load<Dictionary<string, Dictionary<string, string>>>(stoneUID);

        if (!mossData.ContainsKey(modDict["current_moss"]))
        {
            modDict["current_moss"] = "null";
        }
        
        if (modDict["current_moss"] == "null" || !mossData[modDict["current_moss"]].ContainsKey("Texture")) return;
        
        Texture2D texture = Game1.content.Load<Texture2D>(mossData[modDict["current_moss"]]["Texture"]);
        Vector2 position = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32, y * 64 + 51 + 5));
        float depth = (__instance.isPassable() ? __instance.boundingBox.Top : __instance.boundingBox.Bottom) / 10000f;
        Rectangle sourceRect;
        
        switch (__instance.QualifiedItemId)
        {
            case "(O)343":
                if (!mossData[modDict["current_moss"]].ContainsKey("SpriteIndex343")) break;
                sourceRect = Game1.getSourceRectForStandardTileSheet(texture,
                    int.Parse(mossData[modDict["current_moss"]]["SpriteIndex343"]), 16, 16);
                spriteBatch.Draw(texture, position, sourceRect, Color.White, 0f, new Vector2(8f, 14f), 4f,
                    __instance.flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    depth);
                break;
            case "(O)450":
                if (!mossData[modDict["current_moss"]].ContainsKey("SpriteIndex450")) break;
                sourceRect = Game1.getSourceRectForStandardTileSheet(texture,
                    int.Parse(mossData[modDict["current_moss"]]["SpriteIndex450"]), 16, 16);
                spriteBatch.Draw(texture, position, sourceRect, Color.White, 0f, new Vector2(8f, 14f), 4f,
                    __instance.flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    depth);
                break;
        }
    }
}

///////////////////////
//// MUSHROOM LOGS ////
///////////////////////

[HarmonyPatch(typeof(Object), nameof(Object.OutputMushroomLog))]
public class Object_OutputMushroomLog
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher cMatcher = new(instructions);

        cMatcher.MatchEndForward(
                new CodeMatch(OpCodes.Ldstr, "(O)422"),
                new CodeMatch(OpCodes.Stloc_S, 12),
                new CodeMatch(OpCodes.Ldloc_2),
                new CodeMatch(OpCodes.Ldloc_S, 12),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(List<string>), nameof(List<string>.Add)))
            )
            .ThrowIfNotMatch("Couldn't find transpiler start position for Object.OutputMushroomLog patch.")
            .Advance(2)
            .RemoveInstructions(2)
            .Insert(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TranspilerSupplementary), nameof(TranspilerSupplementary.MushroomLog_hasMoss)))
            );

        return cMatcher.InstructionEnumeration();
    }
}