namespace Sharpen
{
	using System;
	using System.IO;

	public class FileOutputStream : OutputStream
	{
		public FileOutputStream (FilePath file): this (file.GetPath (), false)
		{
		}

		public FileOutputStream (string file): this (file, false)
		{
		}

	    public FileOutputStream (string file, bool append)
		{
			try {
				if (append) {
					base.Wrapped = File.Open (file, System.IO.FileMode.Append, FileAccess.Write);
				} else {
					base.Wrapped = File.Open (file, System.IO.FileMode.Create, FileAccess.Write);
				}
			} catch (DirectoryNotFoundException) {
				throw new FileNotFoundException ("File not found: " + file);
			}
		}
	}
}
