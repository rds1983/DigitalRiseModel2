using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace DigitalRiseModel;

/// <summary>
/// Represents a read-only collection of <see cref="DrModelBone"/> objects.
/// </summary>
public sealed class DrModelBoneCollection : ReadOnlyCollection<DrModelBone>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DrModelBoneCollection"/> class.
	/// </summary>
	/// <param name="list">The list to wrap.</param>
	public DrModelBoneCollection(IList<DrModelBone> list)
		: base(list)
	{
	}

	/// <summary>
	/// Gets the bone with the specified name.
	/// </summary>
	/// <param name="boneName">The name of the bone.</param>
	/// <returns>The bone with the specified name.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="boneName"/> is <see langword="null"/>.</exception>
	/// <exception cref="KeyNotFoundException">No bone with the specified name exists in the collection.</exception>
	public DrModelBone this[string boneName]
	{
		get
		{
			if (TryGetValue(boneName, out DrModelBone? bone))
			{
				return bone;
			}

			throw new KeyNotFoundException($"No bone named '{boneName}' exists in the collection.");
		}
	}

	/// <summary>
	/// Attempts to find a bone by name.
	/// </summary>
	/// <param name="boneName">The name of the bone.</param>
	/// <param name="value">The matching bone, or <see langword="null"/> if no match was found.</param>
	/// <returns><see langword="true"/> if a bone with the specified name was found; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="boneName"/> is <see langword="null"/>.</exception>
	public bool TryGetValue(string boneName, [NotNullWhen(true)] out DrModelBone? value)
	{
		ArgumentNullException.ThrowIfNull(boneName);

		foreach (DrModelBone bone in this)
		{
			if (string.Equals(bone.Name, boneName, StringComparison.Ordinal))
			{
				value = bone;
				return true;
			}
		}

		value = null;
		return false;
	}
}
