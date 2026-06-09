using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace AlchemyExclusivePotions;

public class AlchemyExclusivePotionsModSystem : ModSystem
{
    private const string HarmonyId   = "alchemyexclusivepotions";
    private const string ConfigFile  = "alchemyexclusivepotions.json";

    private Harmony? _harmony;

    /// <summary>
    /// Loaded config — shared with the Harmony patch via a static reference so the
    /// patch (which has no ModSystem reference) can read it without reflection.
    /// </summary>
    internal static AlchemyExclusivePotionsConfig Config { get; private set; } = new();

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    public override bool ShouldLoad(EnumAppSide forSide) => true;

    public override void Start(ICoreAPI api)
    {
        Config = LoadConfig(api);

        _harmony = new Harmony(HarmonyId);
        _harmony.PatchAll(Assembly.GetExecutingAssembly());

        api.Logger.Notification("[AlchemyExclusivePotions] Patches applied.");
        api.Logger.Notification(
            $"[AlchemyExclusivePotions] Config: Enabled={Config.Enabled}, " +
            $"Blacklist={Config.ExclusiveBlacklist.Count} potions, " +
            $"Groups={Config.PotionGroups.Count}");
    }

    public override void Dispose()
    {
        _harmony?.UnpatchAll(HarmonyId);
        base.Dispose();
    }

    // ── Config loading ─────────────────────────────────────────────────────────

    private static AlchemyExclusivePotionsConfig LoadConfig(ICoreAPI api)
    {
        AlchemyExclusivePotionsConfig? cfg = null;
        try
        {
            cfg = api.LoadModConfig<AlchemyExclusivePotionsConfig>(ConfigFile);
        }
        catch (Exception ex)
        {
            api.Logger.Error(
                $"[AlchemyExclusivePotions] Failed to parse ModConfig/{ConfigFile}: " +
                $"{ex.Message}. Using defaults.");
        }

        if (cfg != null)
        {
            api.Logger.Notification(
                $"[AlchemyExclusivePotions] Config loaded from ModConfig/{ConfigFile}");
            return cfg;
        }

        // First launch — write a default config so the player can inspect and edit it.
        cfg = BuildDefaultConfig();
        try
        {
            api.StoreModConfig(cfg, ConfigFile);
            api.Logger.Notification(
                $"[AlchemyExclusivePotions] Default config written to ModConfig/{ConfigFile}");
        }
        catch (Exception ex)
        {
            api.Logger.Warning(
                $"[AlchemyExclusivePotions] Could not write default config: {ex.Message}");
        }
        return cfg;
    }

    /// <summary>
    /// Sensible out-of-the-box defaults shipped with the mod.
    /// Combat potions block each other, survival potions block each other, etc.
    /// Potions in different groups may be combined freely.
    /// Blacklisted potions can never be combined with anything.
    /// </summary>
    private static AlchemyExclusivePotionsConfig BuildDefaultConfig() => new()
    {
        Enabled = true,

        ExclusiveBlacklist = new()
        {
            "poisontickpotionid",
            "temporalpotionid",
        },

        PotionGroups = new()
        {
            new PotionGroup
            {
                GroupName = "combat",
                PotionIds = new() { "archerpotionid", "hunterpotionid", "meleepotionid", "predatorpotionid", "vitalitypotionid", "speedpotionid" },
            },
            new PotionGroup
            {
                GroupName = "survival",
                PotionIds = new() { "hungerenhancepotionid", "hungersupresspotionid", "healingeffectpotionid", "regentickpotionid" },
            },
            new PotionGroup
            {
                GroupName = "utility",
                PotionIds = new() { "scentmaskpotionid", "looterpotionid", "miningpotionid" },
            },
            new PotionGroup
            {
                GroupName = "special",
                PotionIds = new() { "recallpotionid", "nutritionpotionid", "glowpotionid", "waterbreathepotionid", "reshapepotionid" },
            },
        },
    };

    // ── Exclusivity logic ──────────────────────────────────────────────────────

    /// <summary>
    /// Core decision — called by the Harmony prefix with the <c>potionId</c> of
    /// the potion the player is about to drink.
    ///
    /// Returns a non-null string (the lang-key to display) when the potion should
    /// be blocked, or null when it may proceed.
    ///
    /// Decision tree:
    /// <list type="number">
    ///   <item>If <see cref="AlchemyExclusivePotionsConfig.Enabled"/> is false → allow.</item>
    ///   <item>Gather every Alchemy potion currently active on the player.</item>
    ///   <item>If none are active → allow.</item>
    ///   <item>If the incoming potion is on the Blacklist → block (solo-only rule).</item>
    ///   <item>If any currently-active potion is on the Blacklist → block.</item>
    ///   <item>Find which group the incoming potion belongs to (if any).</item>
    ///   <item>If any currently-active potion shares that group → block.</item>
    ///   <item>Otherwise → allow.</item>
    /// </list>
    /// </summary>
    internal static string? CheckExclusivity(EntityPlayer entity, string incomingPotionId)
    {
        var cfg = Config;
        if (!cfg.Enabled) return null;

        // Collect every potion currently active on the player.
        var active = GetActivePotionIds(entity);
        if (active.Count == 0) return null;

        var incoming = incomingPotionId.ToLowerInvariant();

        // ── Rule 1: incoming potion is blacklisted → must be the only one ──────
        if (cfg.ExclusiveBlacklist.Contains(incoming, StringComparer.OrdinalIgnoreCase))
            return "alchemyexclusivepotions:potion-blacklisted-incoming";

        // ── Rule 2: a blacklisted potion is already active → nothing else allowed
        foreach (var a in active)
        {
            if (cfg.ExclusiveBlacklist.Contains(a, StringComparer.OrdinalIgnoreCase))
                return "alchemyexclusivepotions:potion-blacklisted-active";
        }

        // ── Rule 3: intra-group conflict ──────────────────────────────────────
        foreach (var group in cfg.PotionGroups)
        {
            bool incomingInGroup = group.PotionIds.Contains(incoming, StringComparer.OrdinalIgnoreCase);
            if (!incomingInGroup) continue;

            foreach (var a in active)
            {
                if (group.PotionIds.Contains(a, StringComparer.OrdinalIgnoreCase))
                    return "alchemyexclusivepotions:potion-group-conflict";
            }
        }

        return null; // all clear
    }

    /// <summary>
    /// Returns the lower-case potionId for every Alchemy potion currently tracked
    /// in <c>entity.WatchedAttributes</c>.
    /// </summary>
    internal static HashSet<string> GetActivePotionIds(EntityPlayer entity)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (entity?.WatchedAttributes == null) return result;

        foreach (string id in AlchemyPotionIds.All)
        {
            if (entity.WatchedAttributes.HasAttribute(id))
                result.Add(id);
        }
        return result;
    }

    // ── Player notification helpers ────────────────────────────────────────────

    internal static void NotifyPlayer(EntityPlayer player, string langKey)
    {
        if (player.World.Side == EnumAppSide.Server)
        {
            (player.Player as IServerPlayer)?.SendMessage(
                GlobalConstants.GeneralChatGroup,
                Lang.Get(langKey),
                EnumChatType.Notification);
        }
        else
        {
            (player.World.Api as ICoreClientAPI)?.ShowChatMessage(Lang.Get(langKey));
        }
    }
}

// ── Harmony patch ──────────────────────────────────────────────────────────────

/// <summary>
/// Prefix patch on <c>Alchemy.PotionEffectManager.TryApplyPotion</c>.
///
/// Signature (from Alchemy source):
///   public bool TryApplyPotion(string id, PotionContext ctx, string name)
///
/// The <c>id</c> parameter is the potionId string (e.g. "archerpotionid").
/// <c>PotionEffectManager</c> holds a <c>private readonly EntityPlayer entity</c>
/// field which we retrieve via the cached <see cref="_entityField"/> reflection.
/// </summary>
[HarmonyPatch]
internal static class PatchTryApplyPotion
{
    private static FieldInfo? _entityField;

    static MethodBase? TargetMethod()
    {
        Assembly? alchemy = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Alchemy");

        if (alchemy == null) return null;

        Type? managerType = alchemy.GetTypes()
            .FirstOrDefault(t => t.Name == "PotionEffectManager");

        if (managerType == null) return null;

        _entityField = managerType.GetField("entity",
            BindingFlags.NonPublic | BindingFlags.Instance);

        return managerType.GetMethod("TryApplyPotion",
            BindingFlags.Public | BindingFlags.Instance);
    }

    /// <summary>
    /// Harmony injects:
    ///   __instance — the PotionEffectManager object
    ///   id         — the first string parameter of TryApplyPotion (the potionId)
    ///
    /// Returns false (cancels original method) when the config says to block.
    /// </summary>
    static bool Prefix(object __instance, string id)
    {
        if (_entityField == null) return true;

        EntityPlayer? player = _entityField.GetValue(__instance) as EntityPlayer;
        if (player == null) return true;

        string? langKey = AlchemyExclusivePotionsModSystem.CheckExclusivity(player, id);
        if (langKey == null) return true; // allowed

        AlchemyExclusivePotionsModSystem.NotifyPlayer(player, langKey);
        return false; // cancel TryApplyPotion
    }
}
