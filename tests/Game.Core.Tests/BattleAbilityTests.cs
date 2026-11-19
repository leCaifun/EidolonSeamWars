using Game.Core.Battle;
using Game.Core.Content;
using Game.Core.Domain;
using Game.Core.Rng;

namespace Game.Core.Tests;

public class BattleAbilityTests
{
    private const string Curse = "core.ability.ash.curse";
    private const string AshBurst = "core.ability.ash.ash_burst";
    private const string Banner = "core.ability.brass.banner";
    private const string Regeneration = "core.ability.root.regeneration";
    private const string Thorns = "core.spell.root.thorns";
    private const string Blessing = "core.spell.brass.blessing";

    private static ContentRegistry Registry =>
        RealContent.MergeWith().Registry ?? throw new InvalidOperationException("Контент не слит.");

    private static BattleCatalog RealCatalog => BattleContentAdapter.CreateCatalog(Registry);

    private static UnitDef Def(
        string id,
        int initiative = 5,
        int speed = 3,
        int tier = 1,
        string[]? abilities = null)
    {
        return new UnitDef(
            id, 1, $"{id}.name", $"{id}.description", "test.faction", tier, 1, 10, 10, initiative, speed,
            5, 5, 0, 150, 10, MovementType.Infantry, default, abilities ?? Array.Empty<string>());
    }

    private static Stack Make(UnitDef def, int count = 5) => new(def, count);

    private static BattleHero Hero(
        int mana = 20,
        string? spell = null,
        string? command = null,
        string? passive = null,
        int attack = 0,
        int defense = 0)
    {
        return new BattleHero("test.hero", attack, defense, 1, mana, spell, command, passive);
    }

    private static BattleState Create(
        IReadOnlyList<Stack> attackers,
        IReadOnlyList<Stack> defenders,
        BattleCatalog? catalog = null,
        BattleHero? attackerHero = null,
        BattleHero? defenderHero = null,
        string? attackerHeroFaction = null,
        Action<BattleState>? beforeStart = null,
        TestLogger? logger = null)
    {
        BattleRules rules = BattleContentAdapter.CreateRules(Registry);

        BattleState state = BattleState.Create(
            new BattleField(rules.FieldWidth, rules.FieldHeight),
            rules,
            new MoraleSystem(BattleContentAdapter.CreateMoraleRules(Registry)),
            new DamageCalculator(rules),
            new SeededRandom(42),
            new BattleSideSetup(attackers, attackerHeroFaction, false, false, attackerHero),
            new BattleSideSetup(defenders, null, false, false, defenderHero),
            logger,
            catalog ?? RealCatalog);

        beforeStart?.Invoke(state);
        state.Start();
        return state;
    }

    private static Hex At(BattleState state, int column, int row) => state.Field.Grid.HexAt(column, row);

    [Fact]
    public void Catalog_FromRealContent_HasAbilitiesAndSpells()
    {
        BattleCatalog catalog = RealCatalog;

        Assert.Equal(21, catalog.Abilities.Count);
        Assert.Equal(3, catalog.Spells.Count);

        AbilityDef curse = catalog.FindAbility(Curse)!;
        Assert.Equal(BattleEffectTypes.KindActive, curse.Kind);
        Assert.Equal(5, curse.Range);
        Assert.Equal(3, curse.CooldownRounds);
        Assert.Equal(3, curse.DurationRounds);
        Assert.Equal(BattleEffectTypes.AttackModifier, curse.Effects[0].Type);
        Assert.Equal(-2, curse.Effects[0].Value);

        SpellDef thorns = catalog.FindSpell(Thorns)!;
        Assert.Equal(8, thorns.ManaCost);
        Assert.Equal(DamageType.Magic, thorns.Effects[0].DamageType);
        Assert.Null(catalog.FindAbility("missing"));
        Assert.Null(catalog.FindSpell(null));
    }

    [Fact]
    public void UseAbility_Curse_DebuffsTargetStartsCooldownAndEndsTurn()
    {
        BattleState state = Create(
            new[] { Make(Def("a0", 9, abilities: new[] { Curse })) },
            new[] { Make(Def("d0", 5)) },
            beforeStart: s => s.FindUnit("D0")!.MoveTo(At(s, 3, 0)));
        BattleUnit target = state.FindUnit("D0")!;

        BattleCommandResult result = state.Apply(BattleCommand.UseAbility("A0", Curse, "D0"));

        Assert.True(result.IsApplied, result.ToString());
        Assert.Equal(8, target.Attack);
        Assert.Equal(3, state.FindUnit("A0")!.GetCooldown(Curse));
        Assert.Equal("D0", state.ActiveUnit!.Id);
    }

    [Fact]
    public void UseAbility_OutOfRangeUnknownAndWrongTarget_AreRejected()
    {
        BattleState state = Create(
            new[] { Make(Def("a0", 9, abilities: new[] { Curse })), Make(Def("a1", 8)) },
            new[] { Make(Def("d0", 5)) });

        Assert.Equal(BattleCommandOutcome.OutOfRange, state.Apply(BattleCommand.UseAbility("A0", Curse, "D0")).Outcome);
        Assert.Equal(BattleCommandOutcome.InvalidTarget, state.Apply(BattleCommand.UseAbility("A0", Curse, "A1")).Outcome);
        Assert.Equal(BattleCommandOutcome.InvalidTarget, state.Apply(BattleCommand.UseAbility("A0", Curse)).Outcome);
        Assert.Equal(BattleCommandOutcome.UnknownAbility, state.Apply(BattleCommand.UseAbility("A0", Banner)).Outcome);
        Assert.Equal(BattleCommandOutcome.UnknownAbility, state.Apply(BattleCommand.UseAbility("A0", "missing")).Outcome);
        Assert.Equal("A0", state.ActiveUnit!.Id);
    }

    [Fact]
    public void UseAbility_CooldownAndEffectDuration_ExpireByRounds()
    {
        BattleState state = Create(
            new[] { Make(Def("a0", 9, abilities: new[] { Curse })) },
            new[] { Make(Def("d0", 5)) },
            beforeStart: s => s.FindUnit("D0")!.MoveTo(At(s, 3, 0)));
        BattleUnit target = state.FindUnit("D0")!;

        Assert.True(state.Apply(BattleCommand.UseAbility("A0", Curse, "D0")).IsApplied);
        state.Apply(BattleCommand.Defend("D0"));

        Assert.Equal(2, state.Round);
        Assert.Equal(BattleCommandOutcome.OnCooldown, state.Apply(BattleCommand.UseAbility("A0", Curse, "D0")).Outcome);
        state.Apply(BattleCommand.Defend("A0"));
        state.Apply(BattleCommand.Defend("D0"));

        Assert.Equal(3, state.Round);
        Assert.True(target.HasEffect(BattleEffectTypes.AttackModifier));
        Assert.Equal(BattleCommandOutcome.OnCooldown, state.Apply(BattleCommand.UseAbility("A0", Curse, "D0")).Outcome);
        state.Apply(BattleCommand.Defend("A0"));
        state.Apply(BattleCommand.Defend("D0"));

        Assert.Equal(4, state.Round);
        Assert.False(target.HasEffect(BattleEffectTypes.AttackModifier));
        Assert.True(state.Apply(BattleCommand.UseAbility("A0", Curse, "D0")).IsApplied);
    }

    [Fact]
    public void UseAbility_AshBurst_DamagesEveryEnemyInRadiusButNotAllies()
    {
        BattleState state = Create(
            new[] { Make(Def("a0", 9, abilities: new[] { AshBurst })), Make(Def("a1", 8)) },
            new[] { Make(Def("d0", 5)), Make(Def("d1", 4)) },
            beforeStart: s => s.FindUnit("D1")!.MoveTo(At(s, 11, 0)));

        Assert.True(state.Apply(BattleCommand.UseAbility("A0", AshBurst, "D0")).IsApplied);

        foreach (string id in new[] { "D0", "D1" })
        {
            int lost = 50 - state.FindUnit(id)!.TotalHealth;
            Assert.InRange(lost, 8, 12);
        }

        Assert.Equal(50, state.FindUnit("A0")!.TotalHealth);
        Assert.Equal(50, state.FindUnit("A1")!.TotalHealth);
    }

    [Fact]
    public void UseAbility_Banner_GivesMoraleBonusToAlliesInRadiusOnly()
    {
        BattleState state = Create(
            new[] { Make(Def("a0", 9, abilities: new[] { Banner })), Make(Def("a1", 8)), Make(Def("a2", 7)) },
            new[] { Make(Def("d0", 5)) });

        Assert.True(state.Apply(BattleCommand.UseAbility("A0", Banner)).IsApplied);

        Assert.Equal(1, state.FindUnit("A0")!.GetEffectSum(BattleEffectTypes.MoraleBonus));
        Assert.Equal(1, state.FindUnit("A1")!.GetEffectSum(BattleEffectTypes.MoraleBonus));
        Assert.Equal(1, state.FindUnit("A2")!.GetEffectSum(BattleEffectTypes.MoraleBonus));
        Assert.Equal(0, state.FindUnit("D0")!.GetEffectSum(BattleEffectTypes.MoraleBonus));
    }

    [Fact]
    public void UseAbility_Regeneration_HealsNowAndEveryRound()
    {
        BattleState state = Create(
            new[] { Make(Def("a0", 9, abilities: new[] { Regeneration })), Make(Def("a1", 8)) },
            new[] { Make(Def("d0", 1)) },
            beforeStart: s => s.FindUnit("A1")!.ApplyDamage(20));
        BattleUnit ally = state.FindUnit("A1")!;

        Assert.True(state.Apply(BattleCommand.UseAbility("A0", Regeneration, "A1")).IsApplied);
        Assert.Equal(38, ally.TotalHealth);

        state.Apply(BattleCommand.Defend("A1"));
        state.Apply(BattleCommand.Defend("D0"));

        Assert.Equal(2, state.Round);
        Assert.Equal(41, ally.TotalHealth);
    }

    [Fact]
    public void UnsupportedEffectOrTarget_IsRejectedWithoutSideEffects()
    {
        BattleCatalog catalog = new(
            new[]
            {
                new AbilityDef("t.summon", "active", "onAction", "enemyStack", 0, 0, 2, 0, 0, new[] { new EffectDef("summonUnit") }),
                new AbilityDef("t.corpses", "active", "onAction", "corpses", 0, 0, 2, 0, 0, new[] { new EffectDef(BattleEffectTypes.Heal, 1) })
            },
            Array.Empty<SpellDef>());
        BattleState state = Create(
            new[] { Make(Def("a0", 9, abilities: new[] { "t.summon", "t.corpses" })) },
            new[] { Make(Def("d0", 5)) },
            catalog);

        Assert.Equal(BattleCommandOutcome.UnsupportedEffect, state.Apply(BattleCommand.UseAbility("A0", "t.summon", "D0")).Outcome);
        Assert.Equal(BattleCommandOutcome.UnsupportedEffect, state.Apply(BattleCommand.UseAbility("A0", "t.corpses", "D0")).Outcome);
        Assert.Equal(0, state.FindUnit("A0")!.GetCooldown("t.summon"));
        Assert.Equal("A0", state.ActiveUnit!.Id);
    }

    [Fact]
    public void CastSpell_Thorns_DamagesRootsSpendsManaAndKeepsUnitTurn()
    {
        BattleState state = Create(
            new[] { Make(Def("a0", 9)) },
            new[] { Make(Def("d0", 5)) },
            attackerHero: Hero(20, Thorns));
        BattleUnit target = state.FindUnit("D0")!;

        BattleCommandResult result = state.Apply(BattleCommand.CastSpell(BattleSide.Attacker, "D0"));

        Assert.True(result.IsApplied, result.ToString());
        Assert.InRange(50 - target.TotalHealth, 6, 10);
        Assert.True(target.HasEffect(BattleEffectTypes.Root));
        Assert.Equal(12, state.GetMana(BattleSide.Attacker));
        Assert.Equal("A0", state.ActiveUnit!.Id);

        state.Apply(BattleCommand.Defend("A0"));
        Assert.Equal("D0", state.ActiveUnit!.Id);
        Assert.Empty(state.ComputeReachable(target));
        Assert.Equal(BattleCommandOutcome.NoPath, state.Apply(BattleCommand.Move("D0", At(state, 11, 0))).Outcome);
    }

    [Fact]
    public void CastSpell_OncePerRoundAndLimitedByMana()
    {
        BattleState state = Create(
            new[] { Make(Def("a0", 9)), Make(Def("a1", 8)) },
            new[] { Make(Def("d0", 5)) },
            attackerHero: Hero(16, Thorns));

        Assert.True(state.Apply(BattleCommand.CastSpell(BattleSide.Attacker, "D0")).IsApplied);
        Assert.Equal(BattleCommandOutcome.SpellAlreadyCast, state.Apply(BattleCommand.CastSpell(BattleSide.Attacker, "D0")).Outcome);

        state.Apply(BattleCommand.Defend("A0"));
        state.Apply(BattleCommand.Defend("A1"));
        state.Apply(BattleCommand.Defend("D0"));

        Assert.Equal(2, state.Round);
        Assert.True(state.Apply(BattleCommand.CastSpell(BattleSide.Attacker, "D0")).IsApplied);
        Assert.Equal(0, state.GetMana(BattleSide.Attacker));
        state.Apply(BattleCommand.Defend("A0"));
        state.Apply(BattleCommand.Defend("A1"));
        state.Apply(BattleCommand.Defend("D0"));

        Assert.Equal(3, state.Round);
        Assert.Equal(BattleCommandOutcome.NotEnoughMana, state.Apply(BattleCommand.CastSpell(BattleSide.Attacker, "D0")).Outcome);
    }

    [Fact]
    public void CastSpell_Rejections()
    {
        BattleState state = Create(
            new[] { Make(Def("a0", 9)), Make(Def("a1", 8)) },
            new[] { Make(Def("d0", 5)) },
            attackerHero: Hero(20, Thorns),
            defenderHero: Hero(20, Thorns));

        Assert.Equal(BattleCommandOutcome.InvalidTarget, state.Apply(BattleCommand.CastSpell(BattleSide.Attacker, "A1")).Outcome);
        Assert.Equal(BattleCommandOutcome.InvalidTarget, state.Apply(BattleCommand.CastSpell(BattleSide.Attacker, "missing")).Outcome);
        Assert.Equal(BattleCommandOutcome.NotActiveUnit, state.Apply(BattleCommand.CastSpell(BattleSide.Defender, "A0")).Outcome);
        Assert.Equal(20, state.GetMana(BattleSide.Attacker));

        BattleState heroless = Create(new[] { Make(Def("a0", 9)) }, new[] { Make(Def("d0", 5)) });
        Assert.Equal(BattleCommandOutcome.NoHero, heroless.Apply(BattleCommand.CastSpell(BattleSide.Attacker, "D0")).Outcome);
    }

    [Fact]
    public void CastSpell_Blessing_AndHeroPassiveBonuses_RaiseStackStats()
    {
        BattleState state = Create(
            new[] { Make(Def("a0", 9)) },
            new[] { Make(Def("d0", 5)) },
            attackerHero: Hero(20, Blessing, attack: 3, defense: 2));
        BattleUnit unit = state.FindUnit("A0")!;

        Assert.Equal(13, unit.Attack);
        Assert.Equal(12, unit.Defense);

        Assert.True(state.Apply(BattleCommand.CastSpell(BattleSide.Attacker, "A0")).IsApplied);

        Assert.Equal(15, unit.Attack);
        Assert.Equal(12, unit.Defense);
    }

    [Fact]
    public void CommandAbility_IsStubUsedOncePerBattle()
    {
        BattleState state = Create(
            new[] { Make(Def("a0", 9)) },
            new[] { Make(Def("d0", 5)) },
            attackerHero: Hero(command: "core.ability.hero.iron_order"));

        Assert.True(state.Apply(BattleCommand.UseCommandAbility(BattleSide.Attacker)).IsApplied);
        Assert.True(state.IsCommandAbilityUsed(BattleSide.Attacker));
        Assert.Equal(
            BattleCommandOutcome.CommandAbilityAlreadyUsed,
            state.Apply(BattleCommand.UseCommandAbility(BattleSide.Attacker)).Outcome);
        Assert.Equal(BattleCommandOutcome.NotActiveUnit, state.Apply(BattleCommand.UseCommandAbility(BattleSide.Defender)).Outcome);
        Assert.Equal(10, state.FindUnit("A0")!.Defense);
    }

    [Fact]
    public void HeroPassive_Discipline_RaisesMoraleAndTrailsRaiseLowTierInitiative()
    {
        BattleState plain = Create(
            new[] { Make(Def("a0", 9)) },
            new[] { Make(Def("d0", 5)) },
            attackerHeroFaction: "test.faction");
        BattleState withDiscipline = Create(
            new[] { Make(Def("a0", 9)) },
            new[] { Make(Def("d0", 5)) },
            attackerHero: Hero(passive: "core.ability.class.discipline"),
            attackerHeroFaction: "test.faction");

        Assert.Equal(1, plain.GetMoraleLevel(BattleSide.Attacker));
        Assert.Equal(2, withDiscipline.GetMoraleLevel(BattleSide.Attacker));

        BattleState trails = Create(
            new[] { Make(Def("a0", 9, tier: 1)), Make(Def("a1", 9, tier: 3)) },
            new[] { Make(Def("d0", 5)) },
            attackerHero: Hero(passive: "core.ability.class.trails"));

        Assert.Equal(10, trails.FindUnit("A0")!.Initiative);
        Assert.Equal(9, trails.FindUnit("A1")!.Initiative);
    }

    private static BattleState StrikeScene(
        UnitDef attacker,
        UnitDef defender,
        int attackerCount = 5,
        Action<BattleState>? beforeStart = null,
        BattleCatalog? catalog = null)
    {
        return Create(
            new[] { Make(attacker, attackerCount) },
            new[] { Make(defender) },
            catalog,
            beforeStart: state =>
            {
                state.FindUnit("D0")!.MoveTo(At(state, 1, 0));
                beforeStart?.Invoke(state);
            });
    }

    [Fact]
    public void Passive_Piercing_IgnoresPartOfDefense()
    {
        BattleState state = StrikeScene(Def("a0", 9, abilities: new[] { "core.ability.brass.piercing" }), Def("d0", 5));

        state.Apply(BattleCommand.Attack("A0", "D0"));

        Assert.Equal(22, state.FindUnit("D0")!.TotalHealth);
    }

    [Fact]
    public void Passive_BoneArmor_ReducesPhysicalDamage()
    {
        BattleState state = StrikeScene(Def("a0", 9), Def("d0", 5, abilities: new[] { "core.ability.ash.bone_armor" }));

        state.Apply(BattleCommand.Attack("A0", "D0"));

        Assert.Equal(31, state.FindUnit("D0")!.TotalHealth);
    }

    [Fact]
    public void Passive_UndeadImmunity_DoesNotBlockPhysical()
    {
        BattleState state = StrikeScene(Def("a0", 9), Def("d0", 5, abilities: new[] { "core.ability.ash.undead" }));

        state.Apply(BattleCommand.Attack("A0", "D0"));

        Assert.Equal(25, state.FindUnit("D0")!.TotalHealth);
    }

    [Fact]
    public void Passive_Bloodthirst_HealsAttackerByPercentOfDamageDealt()
    {
        BattleState state = Create(
            new[] { Make(Def("a0", 9, abilities: new[] { "core.ability.ash.bloodthirst" })) },
            new[] { Make(Def("d0", 5), count: 1) },
            beforeStart: s =>
            {
                s.FindUnit("D0")!.MoveTo(At(s, 1, 0));
                s.FindUnit("A0")!.ApplyDamage(10);
            });

        state.Apply(BattleCommand.Attack("A0", "D0"));

        Assert.False(state.FindUnit("D0")!.IsAlive);
        Assert.Equal(42, state.FindUnit("A0")!.TotalHealth);
    }

    [Fact]
    public void Passive_Charge_AddsDamageOnlyAfterEnoughSteps()
    {
        UnitDef charger = Def("a0", 9, speed: 6, abilities: new[] { "core.ability.brass.charge" });

        BattleState charged = Create(
            new[] { Make(charger) },
            new[] { Make(Def("d0", 5)) },
            beforeStart: s => s.FindUnit("D0")!.MoveTo(At(s, 4, 0)));
        charged.Apply(BattleCommand.Move("A0", At(charged, 3, 0)));
        charged.Apply(BattleCommand.Attack("A0", "D0"));

        BattleState slow = Create(
            new[] { Make(charger) },
            new[] { Make(Def("d0", 5)) },
            beforeStart: s => s.FindUnit("D0")!.MoveTo(At(s, 3, 0)));
        slow.Apply(BattleCommand.Move("A0", At(slow, 2, 0)));
        slow.Apply(BattleCommand.Attack("A0", "D0"));

        Assert.Equal(19, charged.FindUnit("D0")!.TotalHealth);
        Assert.Equal(25, slow.FindUnit("D0")!.TotalHealth);
    }

    [Fact]
    public void Passive_Pack_AddsAttackPerAdjacentSameUnit()
    {
        UnitDef pack = Def("a0", 9, abilities: new[] { "core.ability.root.pack" });
        BattleState state = Create(
            new[] { Make(pack), Make(pack) },
            new[] { Make(Def("d0", 1)) },
            beforeStart: s =>
            {
                s.FindUnit("A1")!.MoveTo(At(s, 1, 0));
                s.FindUnit("D0")!.MoveTo(At(s, 0, 1));
            });

        state.Apply(BattleCommand.Attack("A0", "D0"));

        Assert.Equal(22, state.FindUnit("D0")!.TotalHealth);
    }

    [Fact]
    public void Passive_Formation_DefenseBonusNeedsAdjacentAlly()
    {
        BattleCatalog catalog = new(
            new[]
            {
                new AbilityDef(
                    "t.formation", "passive", "onCalculateDefense", null, 0, 0, 0, 0, 0,
                    new[] { new EffectDef(BattleEffectTypes.DefenseBonusIfAdjacentAlly, 2) })
            },
            Array.Empty<SpellDef>());
        UnitDef defender = Def("d0", 5, abilities: new[] { "t.formation" });

        BattleState guarded = Create(
            new[] { Make(Def("a0", 9, speed: 12)) },
            new[] { Make(defender), Make(Def("d1", 4)) },
            catalog,
            beforeStart: s =>
            {
                s.FindUnit("D0")!.MoveTo(At(s, 3, 0));
                s.FindUnit("D1")!.MoveTo(At(s, 4, 0));
            });
        guarded.Apply(BattleCommand.Attack("A0", "D0"));

        BattleState alone = Create(
            new[] { Make(Def("a0", 9, speed: 12)) },
            new[] { Make(defender) },
            catalog,
            beforeStart: s => s.FindUnit("D0")!.MoveTo(At(s, 3, 0)));
        alone.Apply(BattleCommand.Attack("A0", "D0"));

        Assert.Equal(27, guarded.FindUnit("D0")!.TotalHealth);
        Assert.Equal(25, alone.FindUnit("D0")!.TotalHealth);
    }

    [Fact]
    public void ClassPassive_AttritionAccounting_ReportsRaisedUndead()
    {
        BattleState state = Create(
            new[] { Make(Def("a0", 9)) },
            new[] { Make(Def("d0", 1), count: 5), Make(Def("d1", 1), count: 2) },
            attackerHero: Hero(passive: "core.ability.class.attrition_accounting"));

        state.FindUnit("D0")!.ApplyDamage(10_000);
        state.FindUnit("D1")!.ApplyDamage(10_000);
        Assert.True(state.Apply(BattleCommand.Defend("A0")).IsApplied);

        BattleResult result = state.CreateResult();

        BattleRaisedUnit raised = Assert.Single(result.RaisedUnits);
        Assert.Equal(BattleSide.Attacker, raised.Side);
        Assert.Equal("core.unit.ash.bone_servant", raised.UnitDefId);
        Assert.Equal(5, raised.Count);
    }

    [Fact]
    public void AbilityUse_IsLogged()
    {
        TestLogger logger = new();
        BattleState state = Create(
            new[] { Make(Def("a0", 9, abilities: new[] { Banner })) },
            new[] { Make(Def("d0", 5)) },
            logger: logger);

        state.Apply(BattleCommand.UseAbility("A0", Banner));

        Assert.Contains(logger.Events, e => e.Category == "Battle" && e.Message == "Стак применил способность." && e.Fields["abilityId"] == Banner);
    }
}
