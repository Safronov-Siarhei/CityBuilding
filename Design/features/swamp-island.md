# Swamp island + potions (sandbox archipelago)

> **Status (2026-11-22):** IDEA. Discussed only; nothing is built. The user parked it («потом сделаем»).
> **Next step:**
> 1. The user picks the direction and which island it replaces.
> 2. Lay out the buildings, the chain and the numbers.
> 3. Make the concepts as 1920x1080 screens.
> 4. Only then the code.
>
> **Read with:**
> - [archipelago-and-ships](archipelago-and-ships.md)
> - [hunting](hunting.md)
> - [predators](predators.md)
> - [orc-tribute-and-table-waves](orc-tribute-and-table-waves.md)
> - [dungeon](dungeon.md) (the potion shop)
> - [moat](moat.md) (wading slows walkers)

## The user's request (2026-11-22)
Replace one island of the sandbox map with a **swamp island** that has its own unique resource.

## Direction I proposed: herbs → potions (healing)
Not chosen yet.
- **Main resource:** healing herbs, gathered by a Herbalist's hut on the swamp.
- **Second resource:** sphagnum moss.
- **The Apothecary in town** brews the potions.

**Why potions are needed** (the user asked; the rule: no second «Hides» that piles up unused):
- **Dungeon:** a home-made replacement for the potion shop's potions (healing, strength, speed of the hero). This is the main sink.
- **Plague:** an antiplague potion lets the healer end the island's plague early.
- **Infirmary:** patients recover twice as fast with a potion. Without one, everything works as now: a soft bonus.
- **Soldiers between table waves:** a potion in the barracks or garrison heals a squad at once.
- **Predator bites:** a bitten citizen without a potion falls ill; with one he does not.
- **Tribute:** potions can be written into the «Орки: волны» table as a tribute resource. Only a table row is needed.

**Proposed potions, kept to three:**
- **Healing potion** (herbs + moss): infirmary, soldiers, bites.
- **Antiplague potion** (herbs + snake venom, rare): plague.
- **Hero potions** (herbs + will-o'-wisp dust): dungeon.

**Do not:**
- make potions required for the town's growth (the swamp island opens late);
- make ten kinds of potions.

## Swamp traits (proposed)
- **Bog:** slows walkers like moat wading. Reuse the `Moat` speed mechanism.
- **Hummocks:** buildings only on hummocks; boardwalk roads.
- **Fog:** lingering fog over the island.
- **Mosquitoes:** raise the chance of sickness.
- **A swamp predator** (болотник or a crocodile) through the Predators system.
- **Art:** the orcs' swamp kit can be reused.

## Other resources discussed (alternatives / second resource)
- **Land and water:** peat, bog iron, clay, bog gas, salt.
- **Plants:** reed, cranberry and cloudberry, sphagnum moss, poisonous mushrooms and hemlock, willow, pine resin and tar.
- **Animals:** leeches, frogs and crayfish, heron feathers, snake venom.
- **Rare finds:** amber, bog oak, will-o'-wisp dust, sunken caches.

**Pairs I suggested:**
- herbs + moss: healing;
- resin + bog oak: defence and ships;
- clay + reed: late houses;
- mushrooms + venom: war (poisoned arrows).

## Open questions
- Which island to replace (not the start island).
- Which direction (healing / defence / building / war).
- Mosquitoes and a swamp predator: yes or no.
- Give Hides a use (bandages)?
