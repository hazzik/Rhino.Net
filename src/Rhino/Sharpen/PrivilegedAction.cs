namespace Sharpen
{
	public interface PrivilegedAction<T>
	{
		T Run ();
	}

	public interface PrivilegedExceptionAction<T>
	{
		T Run();
	}
}
