# Alchemy Exclusive Potions

A small companion mod for [Alchemy by Llama3013](https://mods.vintagestory.at/alchemy) that enforces a **one-active-potion-at-a-time** rule.

When a player already has any Alchemy potion effect active, attempting to drink another will:
- **Block** the second potion (it is not consumed)
- **Notify** the player with a chat message

The current potion must expire naturally before a new one can be drunk.

## Covered potions

All potions from the Alchemy mod are covered:

| Potion | ID |
|---|---|
| Archer's Flask | archerpotionid |
| Healing Oil | healingeffectpotionid |
| Hunger Enhance | hungerenhancepotionid |
| Hunger Suppress | hungersupresspotionid |
| Hunter's Delight | hunterpotionid |
| Looter's Delight | looterpotionid |
| Fighter's Flask | meleepotionid |
| Miner's Flask | miningpotionid |
| Purging Brew | poisontickpotionid |
| Predator Pheromones | predatorpotionid |
| Potent Oil (Regen) | regentickpotionid |
| Scent Mask | scentmaskpotionid |
| Sprinter's Flask | speedpotionid |
| Vitality Flask | vitalitypotionid |
| Recall Flask | recallpotionid |
| Nutrition Potion | nutritionpotionid |
| Glow Flask | glowpotionid |
| Water Breathing Flask | waterbreathepotionid |
| Temporal Potion | temporalpotionid |
| Reshape Potion | reshapepotionid |

## Requirements

- Vintage Story 1.21.0+
- Alchemy mod 1.8.0+
- .NET 7 SDK (to build from source)

## Installation

1. Build the mod (see below) or grab the pre-built zip
2. Place `AlchemyExclusivePotions.zip` in your `Mods/` folder alongside Alchemy

## Building from source

**Linux / macOS:**
```bash
./build.sh "/path/to/VintageStory"
```

**Windows:**
```bat
build.bat "C:\Program Files\Vintage Story"
```

Output zip will be in the `dist/` folder.

Alternatively, build manually:
```bash
dotnet build AlchemyExclusivePotions.csproj -c Release /p:VSGamePath="/path/to/VintageStory"
```
Then zip `bin/Release/AlchemyExclusivePotions.dll`, `modinfo.json`, and the `assets/` folder together.

## How it works

The mod uses [Harmony](https://harmony.pardeike.net/) (bundled with Vintage Story) to patch
`ItemPotion.TryApplyPotion` — the single method Alchemy calls whenever any potion is consumed.
The prefix patch checks the player's `WatchedAttributes` for any currently-active Alchemy
potion key. If one is found, the patch returns `false`, which cancels the original method
entirely (the potion is not consumed and the effect is not applied).

No Alchemy source files are modified. The patch is applied at runtime and cleanly removed
on mod unload.

## Customisation

If Alchemy adds new potions in future updates, add their `potionId` values to the
`AlchemyPotionIds.All` set in `src/AlchemyExclusivePotionsMod.cs`.

To change the player notification message, edit:
`assets/alchemyexclusivepotions/lang/en.json`
