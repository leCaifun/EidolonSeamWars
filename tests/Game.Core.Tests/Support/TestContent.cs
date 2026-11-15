using Game.Core.Domain;

namespace Game.Core.Tests;

/// <summary>
/// Конструкторы тестовых доменных данных. Конкретные юниты и фракции
/// остаются данными теста, а не кодом игры.
/// </summary>
internal static class TestContent
{
    public static UnitDef CreateUnit(
        string id = "test.unit",
        int leadershipCost = 1,
        int attack = 5,
        int defense = 5,
        int initiative = 5,
        int speed = 4,
        int damageMin = 2,
        int damageMax = 4,
        int critChancePercent = 5,
        int critDamagePercent = 150,
        int health = 10,
        int tier = 1,
        string factionId = "test.faction",
        MovementType movementType = MovementType.Infantry,
        Resistances resistances = default)
    {
        return new UnitDef(
            id,
            1,
            $"{id}.name",
            $"{id}.description",
            factionId,
            tier,
            leadershipCost,
            attack,
            defense,
            initiative,
            speed,
            damageMin,
            damageMax,
            critChancePercent,
            critDamagePercent,
            health,
            movementType,
            resistances);
    }

    public static HeroClassDefinition CreateHeroClass(
        string id = "test.hero_class",
        HeroStats? startingStats = null,
        IReadOnlyDictionary<HeroStatKind, int>? weights = null)
    {
        return new HeroClassDefinition(
            id,
            1,
            $"{id}.name",
            "test.faction",
            startingStats ?? new HeroStats(3, 3, 1, 1, 70, 2),
            weights ?? new Dictionary<HeroStatKind, int>
            {
                [HeroStatKind.Attack] = 1,
                [HeroStatKind.Defense] = 1,
                [HeroStatKind.Leadership] = 1
            },
            "test.command_ability",
            "test.spell");
    }

    public static Hero CreateHero(HeroClassDefinition? heroClass = null, TestLogger? logger = null)
    {
        return new Hero(
            "test.hero",
            "test.hero.name",
            "test.faction",
            heroClass ?? CreateHeroClass(),
            logger: logger);
    }
}
