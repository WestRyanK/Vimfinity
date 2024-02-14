namespace Vimfinity;

internal static class EnumerableUtilities
{
	public static bool AllEqual<T>(this IEnumerable<T>? first, IEnumerable<T>? second)
	{
		if ((first == null) ^ (second == null))
		{
			return false;
		}

		if (first == null && second == null)
		{
			return true;
		}

		IEnumerator<T> enumerator1 = first!.GetEnumerator();
		IEnumerator<T> enumerator2 = second!.GetEnumerator();

		bool hasNext1, hasNext2;
		while (
			(hasNext1 = enumerator1.MoveNext()) &
			(hasNext2 = enumerator2.MoveNext()))
		{
			T current1 = enumerator1.Current;
			T current2 = enumerator2.Current;

			if ((current1 == null) ^ (current2 == null))
			{
				return false;
			}

			if (current1 == null && current2 == null)
			{
				continue;
			}

			if (!current1!.Equals(current2))
			{
				return false;
			}
		}

		return !hasNext1 && !hasNext2;
	}
}
