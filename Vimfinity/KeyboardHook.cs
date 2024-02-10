using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Vimfinity;

internal interface IKeyboardHookManager
{
	public void AddHook(Func<KeysArgs, HookAction> hook);
	public void RemoveHook();
}

internal class Win32KeyboardHookManager : IKeyboardHookManager, IDisposable
{
	private const int WH_KEYBOARD_LL = 13;

	private IntPtr _hookHandle = IntPtr.Zero;
	private Func<KeysArgs, HookAction>? _hook = null;
	private GCHandle _callbackHandle;

	public bool ShouldHandleInjectedInputs { get; set; } = false;

	~Win32KeyboardHookManager()
	{
		RemoveHook();
	}

	public void Dispose()
	{
		RemoveHook();
		GC.SuppressFinalize(this);
	}

    public void RemoveHook()
	{
		if (_hookHandle != IntPtr.Zero)
		{
			UnhookWindowsHookEx(_hookHandle);
			_callbackHandle.Free();
			_hookHandle = IntPtr.Zero;
			_hook = null;
		}
	}

	public void AddHook(Func<KeysArgs, HookAction> hook)
	{
		if (_hookHandle == IntPtr.Zero)
		{
			using Process currentProcess = Process.GetCurrentProcess();
			using ProcessModule currentModule = currentProcess.MainModule!;
			IntPtr moduleHandle = GetModuleHandle(currentModule.ModuleName);

			// We have to create a GCHandle for the HookCallback, otherwise it gets
			// garbage collected and we get an ExecutionEngineException.
			// See KeyboardHookTests.GarbageCollectionCrash_Test for more info.
			LowLevelKeyboardProc callback = HookCallback;
			_callbackHandle = GCHandle.Alloc(callback);
			_hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, callback, moduleHandle, 0);
		}
		else
		{
			throw new InvalidOperationException("Only one hook can be added at a time.");
		}

		_hook = hook;
	}

	private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	{
		KeyPressedState pressedState = (KeyPressedState)wParam;

		if (nCode < 0 || !Enum.IsDefined(pressedState))
		{
			return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
		}

		var keyboardStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam)!;
		Debug.WriteLine(keyboardStruct);

		if (!ShouldHandleInjectedInputs && keyboardStruct.IsInjected)

		{
			return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
		}

		KeysArgs args = new(
			keyboardStruct.Key,
			pressedState
		);

		HookAction action = _hook?.Invoke(args) ?? HookAction.ForwardKey;

		if (action == HookAction.ForwardKey)
		{
			return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
		}
		else
		{
			return 1; // Swallow key
		}
	}

	private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr SetWindowsHookEx(int idHook,
		LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnhookWindowsHookEx(IntPtr hhk);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
		IntPtr wParam, IntPtr lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr GetModuleHandle(string lpModuleName);
}

[StructLayout(LayoutKind.Sequential)]
public class KBDLLHOOKSTRUCT
{
    public uint vkCode;
    public uint scanCode;
    public uint flags;
    public uint time;
    public UIntPtr dwExtraInfo;

	public Keys Key => (Keys)vkCode;
	public bool IsExtended => (flags & (1 << 0)) != 0;
	public bool IsLowIntegrityInjected => (flags & (1 << 1)) != 0;
	public bool IsInjected => (flags & (1 << 4)) != 0;
	public bool IsAltDown => (flags & (1 << 5)) != 0;
	public bool IsReleasing => (flags & (1 << 7)) != 0;

	public override string ToString()
	{
		return $"Key: {Key, -13}" +
			$"vkCode: {vkCode, -3} " +
			$"scanCode: {scanCode, -3} " +
			$"IsExtended: {IsExtended, -5} " +
			$"IsLowIntegrityInjected: {IsLowIntegrityInjected, -5} " +
			$"IsInjected: {IsInjected, -5} " +
			$"IsAltDown: {IsAltDown, -5} " +
			$"IsReleasing: {IsReleasing, -5}";
	}
}

internal enum HookAction
{
	ForwardKey,
	SwallowKey
}
