using System.Windows.Forms;

namespace Vimfinity.Tests;
public class KeyboardHookTests
{
	/// <summary>
	/// Vimfinity would crash with ExecutionEngineException after running for several minutes.
	/// The problem was that we were passing a delegate for HookCallback directly to the unmanaged
	/// code without holding a reference to it. At some point the garbage collector would run and
	/// it would delete the delegate reference. The next time the unmanaged code tried to call
	/// the delegate, the app would crash because it no longer existed.
	/// If your tests are crashing and not completing, it is probably because this test is failing.
	/// Unfortunately, it is impossible to catch ExecutionEngineException, so this is the best that
	/// can be done.
	/// https://stackoverflow.com/questions/69102624/executionengineexception-on-lowlevel-keyboard-hook
	/// https://stackoverflow.com/questions/17130382/understanding-garbage-collection-in-net/17131389#17131389
	/// https://stackoverflow.com/questions/16544511/prevent-delegate-from-being-garbage-collected
	/// https://learn.microsoft.com/en-us/cpp/dotnet/how-to-marshal-callbacks-and-delegates-by-using-cpp-interop
	/// https://stackoverflow.com/questions/11400476/pin-a-function-pointer
	/// https://stackoverflow.com/questions/45786857/does-a-c-sharp-method-need-to-be-pinned-when-used-as-win32-callback
	/// </summary>

	[Fact]
	public void GarbageCollectionCrash_Test()
	{
		Win32KeyboardHookManager hookManager = new();
		hookManager.ShouldHandleInjectedInputs = true;
		hookManager.AddHook(KeyArgs => HookAction.SwallowKey);
		GC.Collect();
		SendKeys.SendWait("x"); // Crash
	}
}
