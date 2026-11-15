using Game.Core.Domain;

namespace Game.Core.Tests;

public class ResourcePoolTests
{
    [Fact]
    public void Empty_HasSevenZeroResources()
    {
        for (int i = 0; i < 7; i++)
        {
            Assert.Equal(0, ResourcePool.Empty.Get((ResourceKind)i));
        }
    }

    [Fact]
    public void Constructor_ExposesAllSevenResourcesByKind()
    {
        ResourcePool pool = new(1, 2, 3, 4, 5, 6, 7);

        Assert.Equal(1, pool[ResourceKind.Gold]);
        Assert.Equal(2, pool[ResourceKind.Wood]);
        Assert.Equal(3, pool[ResourceKind.Ore]);
        Assert.Equal(4, pool[ResourceKind.Crystal]);
        Assert.Equal(5, pool[ResourceKind.Sulfur]);
        Assert.Equal(6, pool[ResourceKind.Mercury]);
        Assert.Equal(7, pool[ResourceKind.Essence]);
    }

    [Fact]
    public void Constructor_RejectsNegativeResource()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResourcePool(0, -1, 0, 0, 0, 0, 0));
    }

    [Fact]
    public void Add_ReturnsNewPoolAndKeepsOriginalImmutable()
    {
        ResourcePool original = new(10, 0, 0, 0, 0, 0, 0);
        ResourcePool updated = original.Add(ResourceKind.Gold, 5).Add(ResourceKind.Ore, 3);

        Assert.Equal(10, original[ResourceKind.Gold]);
        Assert.Equal(0, original[ResourceKind.Ore]);
        Assert.Equal(15, updated[ResourceKind.Gold]);
        Assert.Equal(3, updated[ResourceKind.Ore]);
    }

    [Fact]
    public void Add_RejectsNegativeAmount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ResourcePool.Empty.Add(ResourceKind.Gold, -1));
    }

    [Fact]
    public void AddPool_SumsComponentwiseAndSupportsOperator()
    {
        ResourcePool left = new(1, 2, 3, 4, 5, 6, 7);
        ResourcePool right = new(7, 6, 5, 4, 3, 2, 1);

        ResourcePool sum = left + right;

        Assert.Equal(new ResourcePool(8, 8, 8, 8, 8, 8, 8), sum);
    }

    [Fact]
    public void CanAfford_ChecksEveryResource()
    {
        ResourcePool treasury = new(100, 0, 10, 0, 0, 0, 0);

        Assert.True(treasury.CanAfford(new ResourcePool(100, 0, 10, 0, 0, 0, 0)));
        Assert.False(treasury.CanAfford(new ResourcePool(101, 0, 0, 0, 0, 0, 0)));
        Assert.False(treasury.CanAfford(new ResourcePool(0, 1, 0, 0, 0, 0, 0)));
    }

    [Fact]
    public void TrySpend_LeavesPoolUnchangedWhenCannotPay()
    {
        ResourcePool treasury = new(10, 0, 0, 0, 0, 0, 0);

        bool spent = treasury.TrySpend(new ResourcePool(20, 0, 0, 0, 0, 0, 0), out ResourcePool remaining);

        Assert.False(spent);
        Assert.Equal(treasury, remaining);
    }

    [Fact]
    public void TrySpend_DeductsCostWhenEnoughResources()
    {
        ResourcePool treasury = new(10, 5, 0, 0, 0, 0, 0);

        bool spent = treasury.TrySpend(new ResourcePool(4, 5, 0, 0, 0, 0, 0), out ResourcePool remaining);

        Assert.True(spent);
        Assert.Equal(new ResourcePool(6, 0, 0, 0, 0, 0, 0), remaining);
    }

    [Fact]
    public void Spend_ThrowsWithOperationContextWhenCannotPay()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ResourcePool.Empty.Spend(new ResourcePool(1, 0, 0, 0, 0, 0, 0), "найм отряда"));

        Assert.Contains("найм отряда", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Get_RejectsUnknownKind()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ResourcePool.Empty.Get((ResourceKind)99));
    }
}
