using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace DigitalRiseModel;

/// <summary>
/// Represents a read-only collection of <see cref="DrModelMesh"/> objects.
/// </summary>
public sealed class DrModelMeshCollection : ReadOnlyCollection<DrModelMesh>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DrModelMeshCollection"/> class.
	/// </summary>
	/// <param name="list">The list to wrap.</param>
	public DrModelMeshCollection(IList<DrModelMesh> list)
		: base(list)
	{
	}

	/// <summary>
	/// Gets the mesh with the specified name.
	/// </summary>
	/// <param name="meshName">The name of the mesh.</param>
	/// <returns>The mesh with the specified name.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="meshName"/> is <see langword="null"/>.</exception>
	/// <exception cref="KeyNotFoundException">No mesh with the specified name exists in the collection.</exception>
	public DrModelMesh this[string meshName]
	{
		get
		{
			if (TryGetValue(meshName, out DrModelMesh? mesh))
			{
				return mesh;
			}

			throw new KeyNotFoundException($"No mesh named '{meshName}' exists in the collection.");
		}
	}

	/// <summary>
	/// Attempts to find a mesh by name.
	/// </summary>
	/// <param name="meshName">The name of the mesh.</param>
	/// <param name="value">The matching mesh, or <see langword="null"/> if no match was found.</param>
	/// <returns><see langword="true"/> if a mesh with the specified name was found; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="meshName"/> is <see langword="null"/>.</exception>
	public bool TryGetValue(string meshName, [NotNullWhen(true)] out DrModelMesh? value)
	{
		ArgumentNullException.ThrowIfNull(meshName);

		foreach (DrModelMesh mesh in this)
		{
			if (string.Equals(mesh.Name, meshName, StringComparison.Ordinal))
			{
				value = mesh;
				return true;
			}
		}

		value = null;
		return false;
	}
}
