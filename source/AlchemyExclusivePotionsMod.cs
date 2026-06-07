using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace AlchemyExclusivePotions;

public class AlchemyExclusivePotionsModSystem : ModSystem
{
    private const string HarmonyId = "alchemyexclusivepotions";

    private Harmony? _harmony;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    public override bool ShouldLoad(EnumAppSide forSide) => true;

    public override void Start(ICoreAPI api)
    {
        _harmony = new Harmony(HarmonyId);
        _harmony.PatchAll(Assembly.GetExecutingAssembly());

        api.Logger.Notification("[AlchemyExclusivePotions] Patches applied — only one Alchemy potion effect may be active at a time.");
    }

    public override void Dispose()
    {
        _harmony?.UnpatchAll(HarmonyId);
        base.Dispose();
    }

    // ── Potion activity detection ──────────────────────────────────────────────

    /// <summary>
    /// Returns true if <paramref name="entity"/> currently has any Alchemy potion
    /// effect active.
    ///
    /// Alchemy tracks each running effect by storing a long (the game-tick listener
    /// handle) in WatchedAttributes under the potion's own ID key (e.g.
    /// "archerpotionid"). The key is present for as long as the effect is running
    /// and removed when it expires. Any non-zero long value means the effect is live.
    /// </summary>
    internal static bool HasAnyActivePotionEffect(EntityPlayer entity)
    {
        if (entity?.WatchedAttributes == null) return false;

        foreach (string potionId in AlchemyPotionIds.All)
        {
            // Alchemy stores a long listener/callback handle while the effect is active
            // and removes the attribute entirely when the effect expires.
            if (entity.WatchedAttributes.HasAttribute(potionId))
                return true;
        }

        return false;
    }
}

// ── Harmony patch ──────────────────────────────────────────────────────────────

/// <summary>
/// Prefix patch on <c>Alchemy.PotionEffectManager.TryApplyPotion</c>.
///
/// Signature (from source):
///   public bool TryApplyPotion(string id, PotionContext ctx, string name)
///
/// <c>PotionEffectManager</c> is an instance that holds a <c>private readonly
/// EntityPlayer entity</c> field. We retrieve the player from <c>__instance</c>
/// via reflection rather than trying to name a parameter that doesn't exist in
/// the patched method's signature.
///
/// Call chain:
///   BlockPotionFlask / ItemPotion (herbball)
///     → HandleCollectibleBehaviorsForDrink
///       → PotionEffectBehavior (EntityBehavior on the player)
///         → PotionEffectManager.TryApplyPotion  ← patched here
/// </summary>
[HarmonyPatch]
internal static class PatchTryApplyPotion
{
    // Cached once at patch time — avoids per-call reflection overhead.
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

        // Cache the private 'entity' field on PotionEffectManager.
        _entityField = managerType.GetField("entity",
            BindingFlags.NonPublic | BindingFlags.Instance);

        return managerType.GetMethod("TryApplyPotion",
            BindingFlags.Public | BindingFlags.Instance);
    }

    /// <summary>
    /// Prefix — runs before TryApplyPotion.
    /// <para>
    /// Harmony injects <c>__instance</c> (the <c>PotionEffectManager</c>) automatically.
    /// We read its private <c>entity</c> field to get the <c>EntityPlayer</c>, then
    /// check whether any Alchemy potion is already active. If one is, we send a
    /// notification and return false to cancel the original method.
    /// </para>
    /// </summary>
    static bool Prefix(object __instance)
    {
        if (_entityField == null) return true;

        EntityPlayer? player = _entityField.GetValue(__instance) as EntityPlayer;
        if (player == null) return true;

        if (!AlchemyExclusivePotionsModSystem.HasAnyActivePotionEffect(player))
            return true;

        // A potion is already active — notify and block.
        if (player.World.Side == EnumAppSide.Server)
        {
            IServerPlayer? serverPlayer = player.Player as IServerPlayer;
            serverPlayer?.SendMessage(
                GlobalConstants.GeneralChatGroup,
                Lang.Get("alchemyexclusivepotions:potion-already-active"),
                EnumChatType.Notification
            );
        }
        else
        {
            (player.World.Api as ICoreClientAPI)?.ShowChatMessage(
                Lang.Get("alchemyexclusivepotions:potion-already-active")
            );
        }

        return false; // cancel TryApplyPotion
    }
}
