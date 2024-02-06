namespace Vimfinity;

internal class KeysArgs : EventArgs
{
	public Keys Key { get; private set; }
	public KeyPressedState PressedState { get; private set; }

	public KeysArgs(Keys key, KeyPressedState pressedState)
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

[Flags]
internal enum KeyModifierFlags
{
	None = 0,
	Control = 1 << 0,
	Shift = 1 << 1,
	Alt = 1 << 2,
	Unspecified = 1 << 3,
}

internal static class KeysExtensions
{
	public static readonly IEnumerable<Keys> ModifierKeys = [Keys.ShiftKey, Keys.ControlKey, Keys.Menu];

	private static readonly HashSet<Keys> _EscapedNameKeys = [
		Keys.CapsLock,
		Keys.Delete,
		Keys.End,
		Keys.Enter,
		Keys.Escape,
		Keys.Help,
		Keys.Home,
		Keys.Insert,
		Keys.NumLock,
		Keys.Tab,
		Keys.Add,
		Keys.Subtract,
		Keys.Multiply,
		Keys.Divide,
		.. Enumerable.Range((int)Keys.F1, (int)Keys.F24 - (int)Keys.F1 + 1).Select(k => (Keys)k)
	];

	public static string ToSendKeysString(this Keys key)
	{
		if (_EscapedNameKeys.Contains(key))
		{
			return $"{{{key.ToString().ToUpper()}}}";
		}

		// https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys.send?view=windowsdesktop-8.0&redirectedfrom=MSDN#System_Windows_Forms_SendKeys_Send_System_String_
		return key switch
		{
			Keys.PageDown => "{PGDN}",
			Keys.PageUp => "{PGUP}",
			Keys.Back => "{BACKSPACE}",
			Keys.Scroll => "{SCROLLLOCK}",
			Keys.OemSemicolon => ";",
			Keys.OemPeriod => ".",
			Keys.Oemcomma => ",",
			Keys.OemOpenBrackets => "[",
			Keys.OemCloseBrackets => "]",
			Keys.OemBackslash => "\\",
			Keys.OemQuestion => "/",
			Keys.OemMinus => "-",
			Keys.ShiftKey or Keys.Shift => "+",
			Keys.ControlKey or Keys.Control => "^",
			Keys.Menu or Keys.Alt => "%",
			_ => key.ToString(),
		};
	}
}
