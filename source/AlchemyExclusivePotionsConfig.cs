using System.Collections.Generic;

namespace AlchemyExclusivePotions;

/// <summary>
/// Root configuration object for the Alchemy Exclusive Potions mod.
///
/// The config file is written to <c>ModConfig/alchemyexclusivepotions.json</c> on
/// first launch and can be edited freely while the server/client is stopped.
///
/// ─────────────────────────────────────────────────────────────────────────────
/// How the three exclusivity mechanisms interact (evaluated in order):
///
///  1. <see cref="Enabled"/> — master switch.  false = no restrictions at all.
///
///  2. <see cref="ExclusiveBlacklist"/> — "solo" potions.  If the potion the
///     player is trying to drink is on this list, it is blocked whenever *any*
///     other Alchemy potion is already active.  Conversely, if any blacklisted
///     potion is already active, every other potion attempt is blocked.
///
///  3. <see cref="PotionGroups"/> — named groups.  Potions inside the same group
///     cannot be stacked with each other.  Potions in *different* groups (and
///     not on the blacklist) can be taken simultaneously.
///
/// Example (mirrors the shipped default):
/// <code>
/// {
///   "Enabled": true,
///   "ExclusiveBlacklist": [ "poisontickpotionid", "temporalpotionid" ],
///   "PotionGroups": [
///     {
///       "GroupName": "combat",
///       "PotionIds": [ "archerpotionid", "hunterpotionid", "meleepotionid" ]
///     },
///     {
///       "GroupName": "survival",
///       "PotionIds": [ "hungerenhancepotionid", "hungersupresspotionid", "vitalitypotionid" ]
///     }
///   ]
/// }
/// </code>
/// With the above config a player could drink <c>archerpotionid</c> (combat group)
/// and <c>hungersupresspotionid</c> (survival group) at the same time, but could
/// not stack <c>archerpotionid</c> with <c>hunterpotionid</c> (both combat), and
/// could not drink <c>poisontickpotionid</c> alongside anything else.
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public class AlchemyExclusivePotionsConfig
{
    // ── Master switch ──────────────────────────────────────────────────────────

    /// <summary>
    /// Set to <c>false</c> to completely disable all exclusivity restrictions.
    /// Default: <c>true</c>
    /// </summary>
    public bool Enabled { get; set; } = true;

    // ── Blacklist (solo potions) ───────────────────────────────────────────────

    /// <summary>
    /// Potion IDs that are mutually exclusive with *every* other potion.
    ///
    /// <list type="bullet">
    ///   <item>If the player tries to drink a blacklisted potion while any other
    ///         Alchemy potion is active → blocked.</item>
    ///   <item>If any blacklisted potion is already active → every new potion
    ///         attempt is blocked.</item>
    /// </list>
    ///
    /// Use this for potions with extreme or overlapping effects (e.g. poisons,
    /// temporal-stability potions) that should never be combined with anything.
    ///
    /// Default: <c>[]</c> (empty — no solo-restriction)
    /// </summary>
    public List<string> ExclusiveBlacklist { get; set; } = new();

    // ── Groups (intra-group exclusivity) ──────────────────────────────────────

    /// <summary>
    /// Named potion groups.  Within a group only one potion may be active at a
    /// time.  Potions in different groups (and not on the blacklist) may be
    /// combined freely.
    ///
    /// Default: <c>[]</c> (empty — all potions are mutually exclusive, i.e. the
    /// original mod behaviour)
    /// </summary>
    public List<PotionGroup> PotionGroups { get; set; } = new();
}

/// <summary>
/// A named set of potions that are mutually exclusive with each other.
/// </summary>
public class PotionGroup
{
    /// <summary>
    /// Human-readable label used in log messages and player notifications.
    /// </summary>
    public string GroupName { get; set; } = "unnamed";

    /// <summary>
    /// The Alchemy <c>potionId</c> values belonging to this group
    /// (e.g. <c>"archerpotionid"</c>, <c>"hunterpotionid"</c>).
    /// </summary>
    public List<string> PotionIds { get; set; } = new();
}
