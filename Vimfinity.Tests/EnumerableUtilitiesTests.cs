namespace Vimfinity.Tests;
public class EnumerableUtilitiesTests
{
	[Fact]
	public void AllEqual_Null()
	{
		Assert.False(EnumerableUtilities.AllEqual<string?>(null, []));
		Assert.False(EnumerableUtilities.AllEqual<string?>([], null));
		Assert.True(EnumerableUtilities.AllEqual<string?>(null, null));
	}

	[Fact]
	public void AllEqual_Equal()
	{
		Assert.True(EnumerableUtilities.AllEqual<string?>([], []));
		Assert.True(EnumerableUtilities.AllEqual(["A"], ["A"]));
		Assert.True(EnumerableUtilities.AllEqual(["A", "B", "C"], ["A", "B", "C"]));
	}

	[Fact]
	public void AllEqual_NullElement()
	{
		Assert.True(EnumerableUtilities.AllEqual([null, "B"], [null, "B"]));
		Assert.False(EnumerableUtilities.AllEqual([null, "B"], ["A", "B"]));
		Assert.False(EnumerableUtilities.AllEqual(["A", "B", "C"], ["A", null, "C"]));
	}

	[Fact]
	public void AllEqual_DifferentCounts()
	{
		Assert.False(EnumerableUtilities.AllEqual(["A"], []));
		Assert.False(EnumerableUtilities.AllEqual(["A"], ["A", "B"]));
		Assert.False(EnumerableUtilities.AllEqual(["A", "B", "C"], ["A", "B"]));
	}
}
