using Microsoft.Xna.Framework;

namespace DigitalRiseModel;

/// <summary>
/// A transform expressed as scale, rotation and translation (SRT).
/// </summary>
public struct SrtTransform : IEquatable<SrtTransform>
{
	/// <summary>
	/// The translation component.
	/// </summary>
	public Vector3 Translation;

	/// <summary>
	/// The rotation component.
	/// </summary>
	public Quaternion Rotation;

	/// <summary>
	/// The scale component.
	/// </summary>
	public Vector3 Scale;

	/// <summary>
	/// Gets the identity transform.
	/// </summary>
	public static SrtTransform Identity => new(Vector3.Zero, Quaternion.Identity, Vector3.One);

	/// <summary>
	/// Initializes a new instance of the <see cref="SrtTransform"/> structure.
	/// </summary>
	/// <param name="translation">The translation component.</param>
	/// <param name="rotation">The rotation component.</param>
	/// <param name="scale">The scale component.</param>
	public SrtTransform(Vector3 translation, Quaternion rotation, Vector3 scale)
	{
		Translation = translation;
		Rotation = rotation;
		Scale = scale;
	}

	/// <inheritdoc />
	public readonly bool Equals(SrtTransform other)
	{
		return Translation == other.Translation
			&& Rotation == other.Rotation
			&& Scale == other.Scale;
	}

	/// <inheritdoc />
	public override readonly bool Equals(object? obj)
	{
		return obj is SrtTransform other && Equals(other);
	}

	/// <inheritdoc />
	public override readonly int GetHashCode()
	{
		return HashCode.Combine(Translation, Rotation, Scale);
	}

	/// <summary>
	/// Compares two transforms for equality.
	/// </summary>
	/// <param name="left">The first transform.</param>
	/// <param name="right">The second transform.</param>
	/// <returns><see langword="true"/> if the transforms are equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(SrtTransform left, SrtTransform right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Compares two transforms for inequality.
	/// </summary>
	/// <param name="left">The first transform.</param>
	/// <param name="right">The second transform.</param>
	/// <returns><see langword="true"/> if the transforms are not equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(SrtTransform left, SrtTransform right)
	{
		return !left.Equals(right);
	}
}
