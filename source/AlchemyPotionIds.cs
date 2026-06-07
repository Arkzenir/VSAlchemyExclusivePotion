namespace AlchemyExclusivePotions;

/// <summary>
/// All WatchedAttributes keys the Alchemy mod uses to track active potion effects.
/// Each string is the <c>potionId</c> value from the item's <c>potioninfo</c>
/// attribute block (e.g. <c>attributesByType["*-archer-*"].potioninfo.potionId</c>).
///
/// These are exactly the keys Alchemy writes into <c>entity.WatchedAttributes</c>
/// while an effect is running.  To support a potion added in a future Alchemy
/// update, add its potionId here.
/// </summary>
internal static class AlchemyPotionIds
{
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── potionportion variants ────────────────────────────────────────────
        "archerpotionid",
        "healingeffectpotionid",
        "hungerenhancepotionid",
        "hungersupresspotionid",
        "hunterpotionid",
        "looterpotionid",
        "meleepotionid",
        "miningpotionid",
        "poisontickpotionid",
        "predatorpotionid",
        "regentickpotionid",
        "scentmaskpotionid",
        "speedpotionid",
        "vitalitypotionid",

        // ── utilitypotionportion variants ─────────────────────────────────────
        "recallpotionid",
        "nutritionpotionid",
        "glowpotionid",
        "waterbreathepotionid",
        "temporalpotionid",
        "reshapepotionid",
    };
}
