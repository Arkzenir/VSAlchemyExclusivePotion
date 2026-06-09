# Alchemy Exclusive Potions

A companion mod for [Alchemy by Llama3013](https://mods.vintagestory.at/alchemy) that controls which potions can be taken together and which cannot.

Potions are divided into **groups**. Only one potion per group may be active at a time — but potions from *different* groups can be combined freely. Certain potions are **blacklisted** and can never be combined with anything.

All of this is configured through a simple JSON file.

## Default behaviour

### Blacklisted (solo only)

These potions cannot be active at the same time as any other potion:

| Potion | ID |
|---|---|
| Purging Brew | `poisontickpotionid` |
| Temporal Potion | `temporalpotionid` |

### Groups

Within each group, only one potion may be active at a time. Potions from different groups can be stacked.

**Combat** — archer, hunter, melee, predator, vitality, speed

| Potion | ID |
|---|---|
| Archer's Flask | `archerpotionid` |
| Hunter's Delight | `hunterpotionid` |
| Fighter's Flask | `meleepotionid` |
| Predator Pheromones | `predatorpotionid` |
| Vitality Flask | `vitalitypotionid` |
| Sprinter's Flask | `speedpotionid` |

**Survival** — hunger and health management

| Potion | ID |
|---|---|
| Hunger Enhance | `hungerenhancepotionid` |
| Hunger Suppress | `hungersupresspotionid` |
| Healing Oil | `healingeffectpotionid` |
| Potent Oil (Regen) | `regentickpotionid` |

**Utility** — non-combat buffs

| Potion | ID |
|---|---|
| Scent Mask | `scentmaskpotionid` |
| Looter's Delight | `looterpotionid` |
| Miner's Flask | `miningpotionid` |

**Special** — utility and transformation

| Potion | ID |
|---|---|
| Recall Flask | `recallpotionid` |
| Nutrition Potion | `nutritionpotionid` |
| Glow Flask | `glowpotionid` |
| Water Breathing Flask | `waterbreathepotionid` |
| Reshape Potion | `reshapepotionid` |

So for example a player could drink a **combat** potion and a **survival** potion simultaneously, but could not stack two **combat** potions, and could not drink **Purging Brew** alongside anything.

## Configuration

On first launch the mod writes a default config to:

```
ModConfig/alchemyexclusivepotions.json
```

Edit this file while the server is stopped. Changes take effect on the next server/client start.

### Full config reference

```json
{
  "Enabled": true,

  "ExclusiveBlacklist": [
    "poisontickpotionid",
    "temporalpotionid"
  ],

  "PotionGroups": [
    {
      "GroupName": "combat",
      "PotionIds": [
        "archerpotionid",
        "hunterpotionid",
        "meleepotionid",
        "predatorpotionid",
        "vitalitypotionid",
        "speedpotionid"
      ]
    }
  ]
}
```

| Field | Type | Description |
|---|---|---|
| `Enabled` | bool | Master switch. `false` disables all restrictions. |
| `ExclusiveBlacklist` | string[] | Potion IDs that must always be taken alone. A blacklisted potion blocks everything, and everything is blocked while a blacklisted potion is active. |
| `PotionGroups` | array | Named groups. Only one potion per group may be active at a time. Potions in different groups may be combined freely. |
| `PotionGroups[].GroupName` | string | Label used in log messages. |
| `PotionGroups[].PotionIds` | string[] | The `potionId` values belonging to this group. |

### Adding new potions

If Alchemy adds new potions in a future update, find the new potion's `potionId` value (it appears in the item's `potioninfo` attribute block in the Alchemy assets) and add it to whichever group fits, or create a new group entirely. You can also add it to `ExclusiveBlacklist` if it should always be taken alone.

New potion IDs also need to be added to the `AlchemyPotionIds.All` set in `source/AlchemyPotionIds.cs` so the mod knows to check for them in `WatchedAttributes`.

## How it works

The mod uses [Harmony](https://harmony.pardeike.net/) (bundled with Vintage Story) to prefix-patch `PotionEffectManager.TryApplyPotion` — the single method Alchemy calls whenever any potion is consumed.

When the patch fires it checks the config against the player's `WatchedAttributes` (where Alchemy records every active effect) and cancels the call if a conflict is found. The potion is not consumed and the effect is not applied. The player receives a chat notification explaining which rule was triggered.

No Alchemy source files are modified. The patch is applied at runtime and cleanly removed on mod unload.

## Requirements

- Vintage Story 1.21.0+
- Alchemy mod 1.8.0+
