/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>AST node for a single- or double-quoted string literal.</summary>
	/// <remarks>
	/// AST node for a single- or double-quoted string literal.
	/// Node type is
	/// <see cref="Rhino.Token.STRING">Rhino.Token.STRING</see>
	/// .<p>
	/// </remarks>
	public class StringLiteral : AstNode
	{
		private string value;

		private char quoteChar;

		public StringLiteral()
		{
			{
				type = Token.STRING;
			}
		}

		public StringLiteral(int pos) : base(pos)
		{
			{
				type = Token.STRING;
			}
		}

		/// <summary>Creates a string literal node at the specified position.</summary>
		/// <remarks>Creates a string literal node at the specified position.</remarks>
		/// <param name="len">the length <em>including</em> the enclosing quotes</param>
		public StringLiteral(int pos, int len) : base(pos, len)
		{
			{
				type = Token.STRING;
			}
		}

		/// <summary>Returns the node's value:  the parsed string without the enclosing quotes</summary>
		/// <returns>
		/// the node's value, a
		/// <see cref="string">string</see>
		/// of unescaped characters
		/// that includes the delimiter quotes.
		/// </returns>
		public virtual string GetValue()
		{
			return value;
		}

		/// <summary>Returns the string value, optionally including the enclosing quotes.</summary>
		/// <remarks>Returns the string value, optionally including the enclosing quotes.</remarks>
		public virtual string GetValue(bool includeQuotes)
		{
			if (!includeQuotes)
			{
				return value;
			}
			return quoteChar + value + quoteChar;
		}

		/// <summary>Sets the node's value.</summary>
		/// <remarks>Sets the node's value.  Do not include the enclosing quotes.</remarks>
		/// <param name="value">the node's value</param>
		/// <exception cref="System.ArgumentException">
		/// } if value is
		/// <code>null</code>
		/// </exception>
		public virtual void SetValue(string value)
		{
			AssertNotNull(value);
			this.value = value;
		}

		/// <summary>Returns the character used as the delimiter for this string.</summary>
		/// <remarks>Returns the character used as the delimiter for this string.</remarks>
		public virtual char GetQuoteCharacter()
		{
			return quoteChar;
		}

		public virtual void SetQuoteCharacter(char c)
		{
			quoteChar = c;
		}

		public override string ToSource(int depth)
		{
			return new StringBuilder(MakeIndent(depth)).Append(quoteChar).Append(ScriptRuntime.EscapeString(value, quoteChar)).Append(quoteChar).ToString();
		}

		/// <summary>Visits this node.</summary>
		/// <remarks>Visits this node.  There are no children to visit.</remarks>
		public override void Visit(NodeVisitor v)
		{
			v.Visit(this);
		}
	}
}
