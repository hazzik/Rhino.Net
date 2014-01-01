using System;
using Sharpen;

namespace Rhino
{
	public class ClassLoader
	{
	    public virtual Type LoadClass(string name)
		{
			try
			{
				if (name != null)
					return Runtime.GetType(name.Replace("$", "+"));
			}
			catch (Exception)
			{
			}
			return null;
		}
	}
}