namespace Vimfinity;

internal abstract class ConditionalBindingAction : IBindingAction
{
	public IBindingAction? Action { get; set; }

	public bool InvertCondition { get; set; }

	public void Invoke()
	{
		if (Condition() ^ InvertCondition)
		{
			Action?.Invoke();
		}
	}

	protected abstract bool Condition();
}
