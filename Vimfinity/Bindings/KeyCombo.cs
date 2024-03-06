using System.Text.Json.Serialization;

namespace Vimfinity;

internal class KeyCombo
{
	[JsonConverter(typeof(JsonStringEnumConverter<Keys>))]
	public Keys Key { get; private set; }
	[JsonConverter(typeof(JsonStringEnumConverter<KeyModifierFlags>))]
	public KeyModifierFlags Modifiers { get; private set; }

	[JsonConstructor]
	public KeyCombo(Keys key, KeyModifierFlags modifiers)
	{
		Key = key;
		Modifiers = modifiers;
	}

	public override bool Equals(object? obj)
	{
		if (obj is not KeyCombo other)
		{
			return false;
		}

		return
			this.Key == other.Key &&
			this.Modifiers == other.Modifiers;
	}

	public override int GetHashCode() => Key.GetHashCode() ^ Modifiers.GetHashCode();

	public override string ToString() => $"{{{Key}, {Modifiers}}}";
}

