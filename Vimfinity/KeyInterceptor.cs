using System.Diagnostics;

namespace Vimfinity;

internal abstract class KeyInterceptor : IDisposable
{
	protected abstract HookAction Intercept(KeysArgs args);

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

	private KeysState _keysState = new();

	protected override HookAction Intercept(KeysArgs args)
	{
		TimeSpan? vimKeyDownDuration = _keysState.GetKeyDownDuration(VimKey);
		_keysState.Record(args);

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

internal class KeysState
{
	private Dictionary<Keys, DateTime> _keysToDownStartUtc = new();

	public void Record(KeysArgs args, DateTime nowUtc)
	{
		if (args.PressedState == KeyPressedState.Up && _keysToDownStartUtc.ContainsKey(args.Key))
		{
			_keysToDownStartUtc.Remove(args.Key);
		}
		else if (args.PressedState == KeyPressedState.Down && !_keysToDownStartUtc.ContainsKey(args.Key))
		{
			_keysToDownStartUtc[args.Key] = nowUtc;
		}
	}

	public void Record(KeysArgs args)
	{
		Record(args, DateTime.UtcNow);
	}

	public TimeSpan? GetKeyDownDuration(Keys key, DateTime nowUtc)
	{
		if (!_keysToDownStartUtc.TryGetValue(key, out DateTime downStartUtc))
		{
			return null;
		}

		return nowUtc - downStartUtc;
	}

	public TimeSpan? GetKeyDownDuration(Keys key)
	{
		return GetKeyDownDuration(key, DateTime.UtcNow);
	}

}
