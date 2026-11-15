using Game.Core.Domain;

namespace Game.Core.Tests;

public class HexTests
{
    [Fact]
    public void Constructor_SetsAxialCoordinates_AndDerivesS()
    {
        Hex hex = new(2, -3);

        Assert.Equal(2, hex.Q);
        Assert.Equal(-3, hex.R);
        Assert.Equal(1, hex.S);
    }

    [Fact]
    public void Zero_IsOrigin()
    {
        Assert.Equal(0, Hex.Zero.Q);
        Assert.Equal(0, Hex.Zero.R);
        Assert.Equal(0, Hex.Zero.S);
    }

    [Fact]
    public void GetNeighbors_ReturnsSixUniqueHexesAtDistanceOne()
    {
        Hex center = new(3, -1);

        Hex[] neighbors = center.GetNeighbors();

        Assert.Equal(Hex.DirectionCount, neighbors.Length);
        Assert.Equal(Hex.DirectionCount, neighbors.Distinct().Count());
        Assert.All(neighbors, neighbor => Assert.Equal(1, center.DistanceTo(neighbor)));
        Assert.DoesNotContain(center, neighbors);
    }

    [Fact]
    public void GetNeighbors_ReturnsStableDirectionOrder()
    {
        Hex center = Hex.Zero;

        Hex[] neighbors = center.GetNeighbors();

        Assert.Equal(new Hex(1, 0), neighbors[0]);
        Assert.Equal(new Hex(1, -1), neighbors[1]);
        Assert.Equal(new Hex(0, -1), neighbors[2]);
        Assert.Equal(new Hex(-1, 0), neighbors[3]);
        Assert.Equal(new Hex(-1, 1), neighbors[4]);
        Assert.Equal(new Hex(0, 1), neighbors[5]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void Neighbor_RejectsDirectionOutsideRange(int direction)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => Hex.Zero.Neighbor(direction));

        Assert.Equal("direction", exception.ParamName);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(0, 0, 3, 0, 3)]
    [InlineData(0, 0, 0, 4, 4)]
    [InlineData(0, 0, 1, 1, 2)]
    [InlineData(2, -1, -1, 2, 3)]
    [InlineData(-2, 3, 2, -3, 6)]
    public void Distance_MatchesKnownHexValues(int aq, int ar, int bq, int br, int expected)
    {
        Hex a = new(aq, ar);
        Hex b = new(bq, br);

        Assert.Equal(expected, a.DistanceTo(b));
        Assert.Equal(expected, b.DistanceTo(a));
        Assert.Equal(expected, Hex.Distance(a, b));
    }

    [Fact]
    public void Distance_ToSelf_IsZero()
    {
        Hex hex = new(7, -4);

        Assert.Equal(0, hex.DistanceTo(hex));
    }

    [Fact]
    public void Distance_ToNeighbor_IsOne()
    {
        Hex origin = Hex.Zero;

        foreach (Hex neighbor in origin.GetNeighbors())
        {
            Assert.Equal(1, origin.DistanceTo(neighbor));
        }
    }

    [Fact]
    public void AdditionAndSubtraction_WorkPerCoordinate()
    {
        Hex left = new(2, -5);
        Hex right = new(-1, 3);

        Assert.Equal(new Hex(1, -2), left + right);
        Assert.Equal(new Hex(3, -8), left - right);
    }

    [Fact]
    public void Equality_ComparesCoordinates()
    {
        Hex first = new(4, -2);
        Hex second = new(4, -2);
        Hex different = new(-2, 4);

        Assert.True(first.Equals(second));
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.False(first.Equals(different));
        Assert.True(first != different);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equality_WorksInHashSet()
    {
        HashSet<Hex> hexes = new() { new Hex(1, 2), new Hex(1, 2), new Hex(2, 1) };

        Assert.Equal(2, hexes.Count);
    }

    [Fact]
    public void ToString_ReturnsReadableCoordinates()
    {
        Assert.Equal("Hex(3, -4)", new Hex(3, -4).ToString());
    }

    [Theory]
    [InlineData(HexOffsetLayout.OddR)]
    [InlineData(HexOffsetLayout.EvenR)]
    public void OffsetCoordinates_RoundTrip(HexOffsetLayout layout)
    {
        for (int q = -12; q <= 12; q++)
        {
            for (int r = -12; r <= 12; r++)
            {
                Hex original = new(q, r);

                (int column, int row) = original.ToOffsetCoordinates(layout);
                Hex restored = Hex.FromOffsetCoordinates(column, row, layout);

                Assert.Equal(original, restored);
                Assert.Equal(r, row);
            }
        }
    }

    [Theory]
    [InlineData(HexOffsetLayout.OddR, 0, 1, 0)]
    [InlineData(HexOffsetLayout.OddR, 2, 1, 2)]
    [InlineData(HexOffsetLayout.EvenR, 0, 1, 1)]
    [InlineData(HexOffsetLayout.EvenR, 2, 1, 3)]
    public void ToOffsetCoordinates_UsesLayoutShift(
        HexOffsetLayout layout,
        int q,
        int r,
        int expectedColumn)
    {
        (int column, int row) = new Hex(q, r).ToOffsetCoordinates(layout);

        Assert.Equal(expectedColumn, column);
        Assert.Equal(r, row);
    }

    [Fact]
    public void OffsetCoordinates_RejectUnknownLayout()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Hex.Zero.ToOffsetCoordinates((HexOffsetLayout)99));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Hex.FromOffsetCoordinates(0, 0, (HexOffsetLayout)99));
    }
}
