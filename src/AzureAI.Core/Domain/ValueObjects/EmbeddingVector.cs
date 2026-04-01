namespace AzureAI.Core.Domain.ValueObjects;

/// <summary>Immutable wrapper around a dense float vector produced by an embedding model.</summary>
public sealed class EmbeddingVector
{
    private readonly float[] _values;

    /// <summary>Initializes a new <see cref="EmbeddingVector"/> from the supplied values.</summary>
    /// <param name="values">The raw embedding values. Must be non-empty.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values"/> is empty.</exception>
    public EmbeddingVector(float[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length == 0)
            throw new ArgumentException("Embedding vector must contain at least one element.", nameof(values));

        _values = (float[])values.Clone();
    }

    /// <summary>Gets the values of this vector as a read-only span.</summary>
    public ReadOnlySpan<float> Values => _values;

    /// <summary>Gets the dimensionality of this vector.</summary>
    public int Dimension => _values.Length;

    /// <summary>
    /// Computes the cosine similarity between this vector and <paramref name="other"/>.
    /// Returns a value in [−1, 1]; identical vectors yield 1.
    /// </summary>
    /// <param name="other">The vector to compare against.</param>
    /// <exception cref="ArgumentException">Thrown when the vectors have different dimensions.</exception>
    public double CosineSimilarity(EmbeddingVector other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Dimension != Dimension)
            throw new ArgumentException(
                $"Vector dimensions do not match: {Dimension} vs {other.Dimension}.", nameof(other));

        double dot = 0d, normA = 0d, normB = 0d;
        for (int i = 0; i < _values.Length; i++)
        {
            dot   += _values[i] * other._values[i];
            normA += _values[i] * _values[i];
            normB += other._values[i] * other._values[i];
        }

        double denominator = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denominator == 0d ? 0d : dot / denominator;
    }

    /// <summary>Returns a copy of the underlying float array.</summary>
    public float[] ToArray() => (float[])_values.Clone();
}
