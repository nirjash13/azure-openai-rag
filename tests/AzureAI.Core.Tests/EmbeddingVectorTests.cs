using AzureAI.Core.Domain.ValueObjects;
using FluentAssertions;

namespace AzureAI.Core.Tests;

public sealed class EmbeddingVectorTests
{
    [Fact]
    public void Constructor_ThrowsOnEmptyArray()
    {
        var act = () => new EmbeddingVector([]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNull()
    {
        var act = () => new EmbeddingVector(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Dimension_ReturnsCorrectValue()
    {
        var vector = new EmbeddingVector([1f, 2f, 3f]);
        vector.Dimension.Should().Be(3);
    }

    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        var v = new EmbeddingVector([1f, 2f, 3f]);
        v.CosineSimilarity(v).Should().BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        var a = new EmbeddingVector([1f, 0f]);
        var b = new EmbeddingVector([0f, 1f]);
        a.CosineSimilarity(b).Should().BeApproximately(0.0, 1e-6);
    }

    [Fact]
    public void CosineSimilarity_ThrowsWhenDimensionsMismatch()
    {
        var a = new EmbeddingVector([1f, 2f]);
        var b = new EmbeddingVector([1f, 2f, 3f]);
        var act = () => a.CosineSimilarity(b);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CosineSimilarity_ZeroVector_ReturnsZero()
    {
        var zero = new EmbeddingVector([0f, 0f, 0f]);
        var other = new EmbeddingVector([1f, 2f, 3f]);
        zero.CosineSimilarity(other).Should().Be(0.0);
    }

    [Fact]
    public void ToArray_ReturnsCopy_MutatingDoesNotAffectInternalState()
    {
        var vector = new EmbeddingVector([1f, 2f, 3f]);
        var copy = vector.ToArray();
        copy[0] = 99f;

        vector.ToArray()[0].Should().Be(1f);
    }
}
