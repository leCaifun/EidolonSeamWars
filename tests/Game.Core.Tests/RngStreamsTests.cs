using Game.Core.Rng;

namespace Game.Core.Tests;

public class RngStreamsTests
{
    [Fact]
    public void SameMasterSeed_ProducesReproducibleStreamSequence()
    {
        RngStreams first = new(777UL);
        RngStreams second = new(777UL);

        for (int i = 0; i < 128; i++)
        {
            Assert.Equal(
                first.Get(RngStreamKind.Battle).NextUInt64(),
                second.Get(RngStreamKind.Battle).NextUInt64());
        }
    }

    [Fact]
    public void Get_ReturnsSameInstance_SoSequenceContinues()
    {
        RngStreams streams = new(5UL);

        SeededRandom firstCall = streams.Get(RngStreamKind.Map);
        firstCall.NextUInt64();

        SeededRandom secondCall = streams.Get(RngStreamKind.Map);

        Assert.Same(firstCall, secondCall);
    }

    [Fact]
    public void Streams_AreIndependentForSameMasterSeed()
    {
        RngStreams streams = new(31UL);

        ulong mapValue = streams.Get(RngStreamKind.Map).NextUInt64();
        ulong battleValue = streams.Get(RngStreamKind.Battle).NextUInt64();
        ulong lootValue = streams.Get(RngStreamKind.Loot).NextUInt64();
        ulong levelUpValue = streams.Get(RngStreamKind.LevelUp).NextUInt64();

        ulong[] values = { mapValue, battleValue, lootValue, levelUpValue };
        Assert.Equal(values.Length, values.Distinct().Count());
    }

    [Fact]
    public void MapStream_IsNotConsumedByBattleStream()
    {
        RngStreams first = new(1234UL);
        RngStreams second = new(1234UL);

        first.Get(RngStreamKind.Battle).NextUInt64();
        first.Get(RngStreamKind.Battle).NextUInt64();

        ulong expectedMapValue = second.Get(RngStreamKind.Map).NextUInt64();
        ulong actualMapValue = first.Get(RngStreamKind.Map).NextUInt64();

        Assert.Equal(expectedMapValue, actualMapValue);
    }

    [Fact]
    public void DeriveSeed_IsStableAndNameSensitive()
    {
        ulong first = RngStreams.DeriveSeed(100UL, "battle");
        ulong second = RngStreams.DeriveSeed(100UL, "battle");
        ulong differentName = RngStreams.DeriveSeed(100UL, "loot");
        ulong differentMaster = RngStreams.DeriveSeed(101UL, "battle");

        Assert.Equal(first, second);
        Assert.NotEqual(first, differentName);
        Assert.NotEqual(first, differentMaster);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DeriveSeed_RejectsEmptyName(string streamName)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => RngStreams.DeriveSeed(1UL, streamName));

        Assert.Equal("streamName", exception.ParamName);
    }

    [Fact]
    public void MasterSeed_IsExposed()
    {
        RngStreams streams = new(4242UL);

        Assert.Equal(4242UL, streams.MasterSeed);
    }
}
