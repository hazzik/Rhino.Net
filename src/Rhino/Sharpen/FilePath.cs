namespace Sharpen
{
	using System;
	using System.IO;

    public class FilePath
	{
		private string path;
		private static long tempCounter;

        public FilePath (string path)
			: this ((string) null, path)
		{

		}

		public FilePath (FilePath other, string child)
			: this ((string) other, child)
		{

		}

		public FilePath (string other, string child)
		{
			if (other == null) {
				this.path = child;
			} else {
				while (!string.IsNullOrEmpty(child) && (child[0] == Path.DirectorySeparatorChar || child[0] == Path.AltDirectorySeparatorChar))
					child = child.Substring (1);

				if (!string.IsNullOrEmpty(other) && other[other.Length - 1] == Path.VolumeSeparatorChar)
					other += Path.DirectorySeparatorChar;

				this.path = Path.Combine (other, child);
			}
		}
		
		public static implicit operator FilePath (string name)
		{
			return new FilePath (name);
		}

		public static implicit operator string (FilePath filePath)
		{
			return filePath == null ? null : filePath.path;
		}
		
		public override bool Equals (object obj)
		{
			FilePath other = obj as FilePath;
			if (other == null)
				return false;
			return GetCanonicalPath () == other.GetCanonicalPath ();
		}
		
		public override int GetHashCode ()
		{
			return path.GetHashCode ();
		}

        public bool Exists ()
		{
			return File.Exists (this) || Directory.Exists (this);
		}

        public string GetCanonicalPath ()
		{
			string p = Path.GetFullPath (path);
			p.TrimEnd (Path.DirectorySeparatorChar);
			return p;
		}

        public string GetPath ()
		{
			return path;
		}

        public bool Mkdirs ()
		{
			try {
				if (Directory.Exists (path))
					return false;
				Directory.CreateDirectory (this.path);
				return true;
			} catch {
				return false;
			}
		}

        public Uri ToURI ()
		{
			return new Uri (path);
		}

        public string GetParent ()
		{
			string p = Path.GetDirectoryName (path);
			if (string.IsNullOrEmpty(p) || p == path)
				return null;
			else
				return p;
		}

		public override string ToString ()
		{
			return path;
		}
	}
}
