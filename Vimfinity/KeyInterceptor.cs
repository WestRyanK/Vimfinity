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
