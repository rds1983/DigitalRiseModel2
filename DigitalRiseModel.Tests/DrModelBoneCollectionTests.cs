using DigitalRiseModel;

namespace DigitalRiseModel.Tests;

public sealed class DrModelBoneCollectionTests
{
	[Fact]
	public void Indexer_ByName_ReturnsMatchingBone()
	{
		var bone = new DrModelBone { Name = "Spine" };
		var collection = new DrModelBoneCollection(new List<DrModelBone> { bone });

		Assert.Same(bone, collection["Spine"]);
	}

	[Fact]
	public void Indexer_NameIsCaseSensitive_ThrowsKeyNotFoundException()
	{
		var bone = new DrModelBone { Name = "Spine" };
		var collection = new DrModelBoneCollection(new List<DrModelBone> { bone });

		Assert.Throws<KeyNotFoundException>(() => collection["spine"]);
	}

	[Fact]
	public void Indexer_UnknownName_ThrowsKeyNotFoundException()
	{
		var collection = new DrModelBoneCollection(new List<DrModelBone>());

		Assert.Throws<KeyNotFoundException>(() => collection["Missing"]);
	}

	[Fact]
	public void TryGetValue_UnknownName_ReturnsFalseAndNull()
	{
		var collection = new DrModelBoneCollection(new List<DrModelBone>());

		bool found = collection.TryGetValue("Missing", out DrModelBone? bone);

		Assert.False(found);
		Assert.Null(bone);
	}

	[Fact]
	public void TryGetValue_NullName_ThrowsArgumentNullException()
	{
		var collection = new DrModelBoneCollection(new List<DrModelBone>());

		Assert.Throws<ArgumentNullException>(() => collection.TryGetValue(null!, out _));
	}
}
