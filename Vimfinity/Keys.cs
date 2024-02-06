﻿namespace Vimfinity;


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


internal static class KeysExtensions
{
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
			_ => key.ToString(),
		};
	}
}
