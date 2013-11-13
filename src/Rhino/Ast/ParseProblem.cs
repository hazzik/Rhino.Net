/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Text;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>Encapsulates information for a JavaScript parse error or warning.</summary>
	/// <remarks>Encapsulates information for a JavaScript parse error or warning.</remarks>
	public class ParseProblem
	{
		public enum Type
		{
			Error,
			Warning
		}

		private ParseProblem.Type type;

		private string message;

		private string sourceName;

		private int offset;

		private int length;

		/// <summary>Constructs a new ParseProblem.</summary>
		/// <remarks>Constructs a new ParseProblem.</remarks>
		public ParseProblem(ParseProblem.Type type, string message, string sourceName, int offset, int length)
		{
			SetType(type);
			SetMessage(message);
			SetSourceName(sourceName);
			SetFileOffset(offset);
			SetLength(length);
		}

		public virtual ParseProblem.Type GetType()
		{
			return type;
		}

		public virtual void SetType(ParseProblem.Type type)
		{
			this.type = type;
		}

		public virtual string GetMessage()
		{
			return message;
		}

		public virtual void SetMessage(string msg)
		{
			this.message = msg;
		}

		public virtual string GetSourceName()
		{
			return sourceName;
		}

		public virtual void SetSourceName(string name)
		{
			this.sourceName = name;
		}

		public virtual int GetFileOffset()
		{
			return offset;
		}

		public virtual void SetFileOffset(int offset)
		{
			this.offset = offset;
		}

		public virtual int GetLength()
		{
			return length;
		}

		public virtual void SetLength(int length)
		{
			this.length = length;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(200);
			sb.Append(sourceName).Append(":");
			sb.Append("offset=").Append(offset).Append(",");
			sb.Append("length=").Append(length).Append(",");
			sb.Append(type == ParseProblem.Type.Error ? "error: " : "warning: ");
			sb.Append(message);
			return sb.ToString();
		}
	}
}
