using System.Diagnostics;

namespace Vimfinity;

class Program
{
	public static void Main()
	{
		Debug.WriteLine("Hello World");
		using KeyInterceptor interceptor = new VimKeyInterceptor(new Win32KeyboardHookManager());
		Application.Run();
		Debug.WriteLine("Goodbye cruel world");
	}
}
