using System.Windows.Forms;

namespace Vimfinity.Tests;

public class SerializationTests
{
	[Fact]
	public void KeyComboSerialization_Test()
	{
		KeyCombo combo = new(Keys.W, KeyModifierFlags.Control);

		JsonStringPersistenceProvider provider = new();
		string json = provider.Save(combo);

		string expectedJson =
			"""
			{
			  "Key": "W",
			  "Modifiers": "Control"
			}
			""";
		Assert.Equal(expectedJson, json);

		KeyCombo back = provider.Load<KeyCombo>(json);
		Assert.Equal(combo, back);
	}

	[Fact]
	public void KeyComboSerialization_MultipleModifiers_Test()
	{
		KeyCombo combo = new(Keys.W, KeyModifierFlags.Control | KeyModifierFlags.Shift);

		JsonStringPersistenceProvider provider = new();
		string json = provider.Save(combo);

		string expectedJson =
			"""
			{
			  "Key": "W",
			  "Modifiers": "Control, Shift"
			}
			""";
		Assert.Equal(expectedJson, json);

		KeyCombo back = provider.Load<KeyCombo>(json);
		Assert.Equal(combo, back);
	}

	[Fact]
	public void SettingsSerialization_Test()
	{
		Settings settings = new();
		settings.LayerKey = Keys.OemSemicolon;
		settings.LayerKeyTappedTimeout = TimeSpan.FromSeconds(.5f);
		settings.ModifierReleasedRecentlyTimeout = TimeSpan.FromSeconds(2.25f);
		settings.VimBindings = new Dictionary<KeyCombo, IBindingAction>()
		{
			{ new(Keys.J, KeyModifierFlags.Unspecified), new SendKeysActionBinding("{Down}") },
			{ new(Keys.X, KeyModifierFlags.Shift), new SendKeysActionBinding("{Backspace}") },
			{ new(Keys.T, KeyModifierFlags.None), new RunCommandActionBinding("notepad.txt")},
		};

		JsonStringPersistenceProvider provider = new();
		string json = provider.Save(settings);

		string expectedJson =
			"""
			{
			  "LayerKeyTappedTimeout": "00:00:00.5000000",
			  "ModifierReleasedRecentlyTimeout": "00:00:02.2500000",
			  "LayerKey": "Oem1",
			  "VimBindings": [
			    {
			      "Key": {
			        "Key": "J",
			        "Modifiers": "Unspecified"
			      },
			      "Value": {
			        "$type": "SendKeysActionBinding",
			        "Text": "{Down}"
			      }
			    },
			    {
			      "Key": {
			        "Key": "X",
			        "Modifiers": "Shift"
			      },
			      "Value": {
			        "$type": "SendKeysActionBinding",
			        "Text": "{Backspace}"
			      }
			    },
			    {
			      "Key": {
			        "Key": "T",
			        "Modifiers": "None"
			      },
			      "Value": {
			        "$type": "RunCommandActionBinding",
			        "Command": "notepad.txt"
			      }
			    }
			  ]
			}
			""";
		Assert.Equal(expectedJson, json);

		Settings back = provider.Load<Settings>(json);
		Assert.Equal(settings, back);
	}
}
