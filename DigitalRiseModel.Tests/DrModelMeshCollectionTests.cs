using DigitalRiseModel;

namespace DigitalRiseModel.Tests;

public sealed class DrModelMeshCollectionTests
{
	[Fact]
	public void Indexer_ByName_ReturnsMatchingMesh()
	{
		var mesh = new DrModelMesh([]) { Name = "Body" };
		var collection = new DrModelMeshCollection(new List<DrModelMesh> { mesh });

		Assert.Same(mesh, collection["Body"]);
	}

	[Fact]
	public void Indexer_UnknownName_ThrowsKeyNotFoundException()
	{
		var collection = new DrModelMeshCollection(new List<DrModelMesh>());

		Assert.Throws<KeyNotFoundException>(() => collection["Missing"]);
	}

	[Fact]
	public void TryGetValue_UnknownName_ReturnsFalseAndNull()
	{
		var collection = new DrModelMeshCollection(new List<DrModelMesh>());

		bool found = collection.TryGetValue("Missing", out DrModelMesh? mesh);

		Assert.False(found);
		Assert.Null(mesh);
	}
}
