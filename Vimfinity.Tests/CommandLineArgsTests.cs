namespace Vimfinity.Tests;
public class CommandLineArgsTests
{
	[Fact]
	public void HasSwitch_Empty_Test()
	{
		Assert.False(CommandLineArgs.HasSwitch([], CommandLineArgs.NoSplashSwitchName));
	}

	[Fact]
	public void HasSwitch_Lowercase_Test()
	{
		Assert.True(CommandLineArgs.HasSwitch(["-nosplash"], CommandLineArgs.NoSplashSwitchName));
	}

	[Fact]
	public void HasSwitch_Uppercase_Test()
	{
		Assert.True(CommandLineArgs.HasSwitch(["-NoSplash"], CommandLineArgs.NoSplashSwitchName));
	}

	[Fact]
	public void HasSwitch_NoHyphen_Test()
	{
		Assert.False(CommandLineArgs.HasSwitch(["NoSplash"], CommandLineArgs.NoSplashSwitchName));
	}

	[Fact]
	public void HasSwitch_NotFirst_Test()
	{
		Assert.True(CommandLineArgs.HasSwitch(["-hello", "-NoSplash", "-world"], CommandLineArgs.NoSplashSwitchName));
	}

	[Fact]
	public void NoSplash_Test()
	{
		Assert.False(new CommandLineArgs([]).NoSplash);
		Assert.True(new CommandLineArgs([$"-{CommandLineArgs.NoSplashSwitchName}"]).NoSplash);
	}

	[Fact]
	public void CreateSettingsFile_Test()
	{
		Assert.False(new CommandLineArgs([]).CreateSettingsFile);
		Assert.True(new CommandLineArgs([$"-{CommandLineArgs.CreateSettingsFileSwitchName}"]).CreateSettingsFile);
	}
}
