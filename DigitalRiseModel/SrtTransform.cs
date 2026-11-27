using DigitalRiseModel.Animation;
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

	/// <summary>
	/// Interpolates between two transforms.
	/// </summary>
	/// <param name="start">The transform at <paramref name="amount"/> 0.</param>
	/// <param name="end">The transform at <paramref name="amount"/> 1.</param>
	/// <param name="amount">The interpolation amount, normally between 0 and 1.</param>
	/// <param name="translationInterpolation">The interpolation mode for translation.</param>
	/// <param name="rotationInterpolation">The interpolation mode for rotation.</param>
	/// <param name="scaleInterpolation">The interpolation mode for scale.</param>
	/// <returns>The interpolated transform.</returns>
	/// <remarks>
	/// <see cref="AnimationInterpolation.Step"/> holds the start component. All other modes
	/// interpolate linearly; rotation uses shortest-path spherical interpolation. Baked
	/// transforms do not retain cubic tangents, so <see cref="AnimationInterpolation.CubicSpline"/>
	/// is treated like <see cref="AnimationInterpolation.Linear"/>.
	/// </remarks>
	public static SrtTransform Interpolate(
		SrtTransform start,
		SrtTransform end,
		float amount,
		AnimationInterpolation translationInterpolation,
		AnimationInterpolation rotationInterpolation,
		AnimationInterpolation scaleInterpolation)
	{
		return new SrtTransform(
			InterpolateVector(start.Translation, end.Translation, amount, translationInterpolation),
			InterpolateRotation(start.Rotation, end.Rotation, amount, rotationInterpolation),
			InterpolateVector(start.Scale, end.Scale, amount, scaleInterpolation));
	}

	/// <summary>
	/// Converts this transform to a matrix.
	/// </summary>
	/// <returns>
	/// The scale, rotation and translation composed as
	/// <c>scale * rotation * translation</c> using row-vector conventions.
	/// </returns>
	public readonly Matrix ToMatrix()
	{
		return Matrix.CreateScale(Scale)
			* Matrix.CreateFromQuaternion(Rotation)
			* Matrix.CreateTranslation(Translation);
	}

	private static Vector3 InterpolateVector(
		Vector3 start,
		Vector3 end,
		float amount,
		AnimationInterpolation interpolation)
	{
		return interpolation == AnimationInterpolation.Step
			? start
			: Vector3.Lerp(start, end, amount);
	}

	private static Quaternion InterpolateRotation(
		Quaternion start,
		Quaternion end,
		float amount,
		AnimationInterpolation interpolation)
	{
		return interpolation == AnimationInterpolation.Step
			? start
			: Quaternion.Normalize(Quaternion.Slerp(start, end, amount));
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
