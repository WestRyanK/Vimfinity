using System.Diagnostics;

namespace Vimfinity;

internal abstract class KeyInterceptor : IDisposable
{
	protected abstract HookAction Intercept(HookArgs args);

	public KeyInterceptor()
	{
		KeyboardHookManager.AddHook(Intercept);
	}

	public void Dispose()
	{
		KeyboardHookManager.RemoveHook();
	}
}

internal class VimKeyInterceptor : KeyInterceptor
{
	public TimeSpan VimKeyDownMinDuration { get; set; } = TimeSpan.FromSeconds(.15f);
	public Keys VimKey { get; set; } = Keys.OemSemicolon;

	private static readonly IDictionary<Keys, string> _InputKeysToOutput = new Dictionary<Keys, string>() {
		{ Keys.H, "{Left}" },
		{ Keys.J, "{Down}" },
		{ Keys.K, "{Up}" },
		{ Keys.L, "{Right}" },
		{ Keys.X, "{Backspace}" },
	};

	private KeysHistory _keysHistory = new();

	protected override HookAction Intercept(HookArgs args)
	{
		TimeSpan? vimKeyDownDuration = _keysHistory.GetKeyDownDuration(VimKey);
		_keysHistory.Record(args);

		if (vimKeyDownDuration >= VimKeyDownMinDuration)
		{
			if (args.PressedState == KeyPressedState.Down &&
				_InputKeysToOutput.TryGetValue(args.Key, out var output))
			{
				SendKeys.Send(output);
				return HookAction.SwallowKey;
			}
		}

		if (args.Key == VimKey)
		{
			Debug.WriteLine($"Key {args.Key} State {args.PressedState} Duration {vimKeyDownDuration}");
			if (args.PressedState == KeyPressedState.Up && vimKeyDownDuration < VimKeyDownMinDuration)
			{
				SendKeys.Send(VimKey.ToSendKeysString());
			}
			return HookAction.SwallowKey;
		}

		return HookAction.ForwardKey;
	}
}

internal class KeysHistory
{
	private Dictionary<Keys, DateTime> _keysToStartDownUtc = new();

	public void Record(HookArgs args, DateTime nowUtc)
	{
		if (args.PressedState == KeyPressedState.Up && _keysToStartDownUtc.ContainsKey(args.Key))
		{
			_keysToStartDownUtc.Remove(args.Key);
		}
		else if (args.PressedState == KeyPressedState.Down && !_keysToStartDownUtc.ContainsKey(args.Key))
		{
			_keysToStartDownUtc[args.Key] = nowUtc;
		}
	}

	public void Record(HookArgs args)
	{
		Record(args, DateTime.UtcNow);
	}

	public TimeSpan? GetKeyDownDuration(Keys key, DateTime nowUtc)
	{
		if (!_keysToStartDownUtc.TryGetValue(key, out DateTime startDownUtc))
		{
			return null;
		}

		return nowUtc - startDownUtc;
	}

	public TimeSpan? GetKeyDownDuration(Keys key)
	{
		return GetKeyDownDuration(key, DateTime.UtcNow);
	}
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
