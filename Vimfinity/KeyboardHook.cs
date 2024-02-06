using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Vimfinity;

internal class KeyboardHookManager
{
	private const int WH_KEYBOARD_LL = 13;

	private static IntPtr _HookHandle = IntPtr.Zero;
	private static Func<HookArgs, HookAction>? _Hook = null;

	public static void RemoveHook()
	{
		if (_HookHandle != IntPtr.Zero)
		{
			UnhookWindowsHookEx(_HookHandle);
			_HookHandle = IntPtr.Zero;
			_Hook = null;
		}
	}

	public static void AddHook(Func<HookArgs, HookAction> hook)
	{
		if (_HookHandle == IntPtr.Zero)
		{
			using Process currentProcess = Process.GetCurrentProcess();
			using ProcessModule currentModule = currentProcess.MainModule!;
			IntPtr moduleHandle = GetModuleHandle(currentModule.ModuleName);

			_HookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, HookCallback, moduleHandle, 0);
		}
		else
		{
			throw new InvalidOperationException("Only one hook can be added at a time.");
		}

		_Hook = hook;
	}

	private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	{
		KeyPressedState pressedState = (KeyPressedState)wParam;

		if (nCode < 0 || !Enum.IsDefined(pressedState))
		{
			return CallNextHookEx(_HookHandle, nCode, wParam, lParam);
		}

		var keyboardStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam)!;

		if (keyboardStruct.IsInjected)
		{
			return CallNextHookEx(_HookHandle, nCode, wParam, lParam);
		}

		HookArgs args = new(
			keyboardStruct.Key,
			pressedState
		);

		HookAction action = _Hook?.Invoke(args) ?? HookAction.ForwardKey;

		if (action == HookAction.ForwardKey)
		{
			return CallNextHookEx(_HookHandle, nCode, wParam, lParam);
		}
		else
		{
			return 1; // Swallow key
		}
	}

	private delegate IntPtr LowLevelKeyboardProc(
		int nCode, IntPtr wParam, IntPtr lParam);

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
		return $"vkCode: {vkCode} " +
			$"scanCode: {scanCode} " +
			$"IsExtended: {IsExtended} " +
			$"IsLowIntegrityInjected: {IsLowIntegrityInjected} " +
			$"IsInjected: {IsInjected} " +
			$"IsAltDown: {IsAltDown} " +
			$"IsReleasing: {IsReleasing}";
	}
}

internal class HookArgs : EventArgs
{
	public Keys Key { get; private set; }
	public KeyPressedState PressedState { get; private set; }

	public HookArgs(Keys key, KeyPressedState pressedState)
	{
		Key = key;
		PressedState = pressedState;
	}

	public bool IsKeyDown(Keys key) => Key == key && PressedState == KeyPressedState.Down;

	public bool IsKeyUp(Keys key) => Key == key && PressedState == KeyPressedState.Up;
}

internal enum KeyPressedState
{
	/// <summary>
	/// WM_KEYDOWN
	/// </summary>
	Down = 0x0100,
	/// <summary>
	/// WM_KEYUP
	/// </summary>
	Up = 0x0101
}

internal enum HookAction
{
	ForwardKey,
	SwallowKey
}
