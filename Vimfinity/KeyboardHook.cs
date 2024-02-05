using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Vimfinity;

internal class KeyboardHookManager
{
	private const int WH_KEYBOARD_LL = 13;

	private static IntPtr _HookHandle = IntPtr.Zero;
	private static Func<HookArgs, HookArgs>? _Hook = null;

	public static void RemoveHook()
	{
		UnhookWindowsHookEx(_HookHandle);
		_HookHandle = IntPtr.Zero;
		_Hook = null;
	}

	public static void AddHook(Func<HookArgs, HookArgs> hook)
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

		HookArgs args = new(
			(Keys)Marshal.ReadInt32(lParam),
			pressedState
		);

		args = _Hook?.Invoke(args) ?? args;

		if (args.Key == Keys.None || args.PressedState == KeyPressedState.None)
		{
			return 1; // Swallow key
		}
		else
		{
			Marshal.WriteInt32(lParam, (int)args.Key);
			return CallNextHookEx(_HookHandle, nCode, (IntPtr)args.PressedState, lParam);
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

internal class HookArgs : EventArgs
{
	public static readonly HookArgs Swallow = new HookArgs();

	public Keys Key { get; private set; } = Keys.None;
	public KeyPressedState PressedState { get; private set; } = KeyPressedState.None;

	public HookArgs() { }

	public HookArgs(Keys key, KeyPressedState pressedState)
	{
		Key = key;
		PressedState = pressedState;
	}
}

internal enum KeyPressedState
{
	/// <summary>
	/// Undefined
	/// </summary>
	None = 0,
	/// <summary>
	/// WM_KEYDOWN
	/// </summary>
	Down = 0x0100,
	/// <summary>
	/// WM_KEYUP
	/// </summary>
	Up = 0x0101
}
