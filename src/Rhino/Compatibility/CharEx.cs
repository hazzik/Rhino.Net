using System.Globalization;

namespace Rhino
{
	public static class CharEx
	{
		public static bool IsJavaIdentifierStart(char c)
		{
			if (char.IsLetter(c))
				return true;
			if (char.GetUnicodeCategory(c) == UnicodeCategory.LetterNumber)
				return true;
			if (c == '$')
				return true;
			if (c == '_')
				return true;
			return false;
		}

		public static bool IsJavaIdentifierPart(char c)
		{
			if (IsJavaIdentifierStart(c))
				return true;
			if (char.IsDigit(c))
				return true;
			return false;
		}
	}
}