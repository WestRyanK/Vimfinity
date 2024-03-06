using System.Text.Json.Serialization;

namespace Vimfinity;

[JsonDerivedType(typeof(SendKeysBindingAction), nameof(SendKeysBindingAction))]
[JsonDerivedType(typeof(RunCommandBindingAction), nameof(RunCommandBindingAction))]
internal interface IBindingAction
{
	void Invoke();
}
