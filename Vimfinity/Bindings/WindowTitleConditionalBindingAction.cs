using System.Diagnostics;

namespace Vimfinity;
internal class WindowTitleConditionalBindingAction : ConditionalBindingAction
{
	public string? WindowTitle { get; set; }

	protected override bool Condition()
	{
		var processes = Process.GetProcesses()
			.Where(p => !string.IsNullOrEmpty(p.MainWindowTitle));

		return processes.Any(p => p.MainWindowTitle.Equals(WindowTitle, StringComparison.OrdinalIgnoreCase));
	}
}
