using System.Text.Json.Serialization;

namespace Vimfinity;

internal class Settings
{
	public TimeSpan LayerKeyTappedTimeout { get; set; } = TimeSpan.FromSeconds(.2f);
	public TimeSpan ModifierReleasedRecentlyTimeout { get; set; } = TimeSpan.FromSeconds(.1f);

	[JsonConverter(typeof(JsonStringEnumConverter<Keys>))]
	public Keys LayerKey { get; set; } = Keys.OemSemicolon;

	public static readonly IReadOnlyDictionary<KeyCombo, IBindingAction> DefaultVimBindings = new Dictionary<KeyCombo, IBindingAction>()
	{
		{ new(Keys.H, KeyModifierFlags.Unspecified), new SendKeysBindingAction("{Left}") },
		{ new(Keys.J, KeyModifierFlags.Unspecified), new SendKeysBindingAction("{Down}") },
		{ new(Keys.K, KeyModifierFlags.Unspecified), new SendKeysBindingAction("{Up}") },
		{ new(Keys.L, KeyModifierFlags.Unspecified), new SendKeysBindingAction("{Right}") },
		{ new(Keys.N, KeyModifierFlags.Unspecified), new SendKeysBindingAction("{Home}") },
		{ new(Keys.M, KeyModifierFlags.Unspecified), new SendKeysBindingAction("{End}") },
		{ new(Keys.X, KeyModifierFlags.Shift), new SendKeysBindingAction("{Backspace}") },
		{ new(Keys.X, KeyModifierFlags.None), new SendKeysBindingAction("{Delete}") },
	};

	[JsonIgnore]
	public IReadOnlyDictionary<KeyCombo, IBindingAction> VimBindings { get; set; } = DefaultVimBindings;

	[JsonPropertyName("VimBindings")]
	public IList<KeyValuePair<KeyCombo, IBindingAction>> SerializedVimBindings
	{
		get => VimBindings.ToList();
		set => VimBindings = value.ToDictionary(x => x.Key, x => x.Value);
	}

	public override bool Equals(object? obj)
	{
		if (obj is not Settings other)
		{
			return false;
		}

		return
			LayerKey == other.LayerKey &&
			LayerKeyTappedTimeout == other.LayerKeyTappedTimeout &&
			ModifierReleasedRecentlyTimeout == other.ModifierReleasedRecentlyTimeout &&
			SerializedVimBindings.Count == other.SerializedVimBindings.Count &&
			SerializedVimBindings.Zip(other.SerializedVimBindings).All(x => x.First.Key.Equals(x.Second.Key) && x.First.Value.Equals(x.Second.Value));
	}

	public override int GetHashCode() =>
		LayerKey.GetHashCode() ^
		LayerKeyTappedTimeout.GetHashCode() ^
		ModifierReleasedRecentlyTimeout.GetHashCode() ^
		SerializedVimBindings.GetHashCode();
}

