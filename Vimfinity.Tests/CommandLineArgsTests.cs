namespace Vimfinity.Tests;
public class CommandLineArgsTests
{
	[Fact]
	public void NoSplash_Empty_Test()
	{
		CommandLineArgs args = new([]);
		Assert.False(args.NoSplash);
	}

	[Fact]
	public void NoSplash_Lowercase_Test()
	{
		CommandLineArgs args = new(["-nosplash"]);
		Assert.True(args.NoSplash);
	}

	[Fact]
	public void NoSplash_Uppercase_Test()
	{
		CommandLineArgs args = new(["-NoSplash"]);
		Assert.True(args.NoSplash);
	}

	[Fact]
	public void NoSplash_NoHyphen_Test()
	{
		CommandLineArgs args = new(["nosplash"]);
		Assert.False(args.NoSplash);
	}

	[Fact]
	public void NoSplash_NotFirst_Test()
	{
		CommandLineArgs args = new(["-hello", "-nosplash", "-world"]);
		Assert.True(args.NoSplash);
	}

}
