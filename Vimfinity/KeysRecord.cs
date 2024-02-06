namespace Vimfinity;

internal class KeysRecord
{
	private Dictionary<Keys, DateTime> _downStartTimesUtc = new();
	private Dictionary<Keys, DateTime> _upTimesUtc = new();

	public void Record(KeysArgs args, DateTime nowUtc)
	{
		if (args.PressedState == KeyPressedState.Up)
		{
			_upTimesUtc[args.Key] = nowUtc;

			if (_downStartTimesUtc.ContainsKey(args.Key))
			{
				_downStartTimesUtc.Remove(args.Key);
			}
		}
		else if (args.PressedState == KeyPressedState.Down && !_downStartTimesUtc.ContainsKey(args.Key))
		{
			_downStartTimesUtc[args.Key] = nowUtc;
		}
	}

	public TimeSpan? GetKeyDownDuration(Keys key, DateTime nowUtc)
	{
		if (GetKeyRecord(_downStartTimesUtc, key, true) is not DateTime downStartUtc)
		{
			return null;
		}

		return nowUtc - downStartUtc;
	}

	public TimeSpan? GetKeyUpDuration(Keys key, DateTime nowUtc)
	{
		if (GetKeyRecord(_upTimesUtc, key, false) is not DateTime upTimeUtc)
		{
			return null;
		}

		return nowUtc - upTimeUtc;
	}

	public bool IsKeyDown(Keys key) => GetKeyRecord(_downStartTimesUtc, key, true) != null;

	public KeyModifierFlags GetKeyModifiersDown()
	{
		KeyModifierFlags modifiers = KeyModifierFlags.None;
		modifiers |= IsKeyDown(Keys.Control) ? KeyModifierFlags.Control : KeyModifierFlags.None;
		modifiers |= IsKeyDown(Keys.Shift) ? KeyModifierFlags.Shift : KeyModifierFlags.None;
		modifiers |= IsKeyDown(Keys.Alt) ? KeyModifierFlags.Alt : KeyModifierFlags.None;
		return modifiers;
	}

	private DateTime? GetKeyRecord(Dictionary<Keys, DateTime> events, Keys key, bool useOldest)
	{
		DateTime? getRecord(IEnumerable<Keys> keys)
		{
			var selectedEvents = keys
				.Select(k => events.TryGetValue(k, out DateTime eventTime) ? eventTime : (DateTime?)null)
				.OfType<DateTime>()
				.ToList();

			if (selectedEvents.Any())
			{
				return useOldest ? selectedEvents.Min() : selectedEvents.Max();
			}
			return null;
		}

		return key switch
		{
			Keys.Modifiers => getRecord([Keys.LMenu, Keys.RMenu, Keys.LShiftKey, Keys.RShiftKey, Keys.LControlKey, Keys.RControlKey]),
			Keys.Menu or Keys.Alt => getRecord([Keys.LMenu, Keys.RMenu]),
			Keys.ShiftKey or Keys.Shift => getRecord([Keys.LShiftKey, Keys.RShiftKey]),
			Keys.ControlKey or Keys.Control => getRecord([Keys.LControlKey, Keys.RControlKey]),
			_ => getRecord([key]),
		};
	}
}
