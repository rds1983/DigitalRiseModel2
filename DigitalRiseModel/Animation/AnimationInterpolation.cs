namespace DigitalRiseModel.Animation;

/// <summary>
/// Defines how keyframes are interpolated.
/// </summary>
public enum AnimationInterpolation
{
	/// <summary>Values are held until the next keyframe.</summary>
	Step,

	/// <summary>Values are interpolated linearly between keyframes.</summary>
	Linear,

	/// <summary>Values use cubic spline tangents stored by the sampler.</summary>
	CubicSpline
}
