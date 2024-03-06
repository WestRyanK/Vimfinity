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
		Settings settings = new()
		{
			Layers = new Dictionary<string, LayerSettings>()
			{
				{ "Test", new() {
						LayerKey = Keys.OemSemicolon,
						LayerKeyTappedTimeout = TimeSpan.FromSeconds(.5f),
						ModifierReleasedRecentlyTimeout = TimeSpan.FromSeconds(2.25f),
						VimBindings = new Dictionary<KeyCombo, IBindingAction>()
						{
							{ new(Keys.J, KeyModifierFlags.Unspecified), new SendKeysBindingAction("{Down}") },
							{ new(Keys.X, KeyModifierFlags.Shift), new SendKeysBindingAction("{Backspace}") },
							{ new(Keys.T, KeyModifierFlags.None), new RunCommandBindingAction("notepad.txt", "file.txt") },
						},
					}
				}
			}
		};

		JsonStringPersistenceProvider provider = new();
		string json = provider.Save(settings);

		string expectedJson =
			"""
			{
			  "Layers": [
			    {
			      "LayerName": "Test",
			      "Settings": {
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
			              "$type": "SendKeysBindingAction",
			              "Text": "{Down}"
			            }
			          },
			          {
			            "Key": {
			              "Key": "X",
			              "Modifiers": "Shift"
			            },
			            "Value": {
			              "$type": "SendKeysBindingAction",
			              "Text": "{Backspace}"
			            }
			          },
			          {
			            "Key": {
			              "Key": "T",
			              "Modifiers": "None"
			            },
			            "Value": {
			              "$type": "RunCommandBindingAction",
			              "Command": "notepad.txt",
			              "Arguments": "file.txt"
			            }
			          }
			        ]
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
