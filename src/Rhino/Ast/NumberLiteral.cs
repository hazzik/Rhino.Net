/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>AST node for a Number literal.</summary>
	/// <remarks>
	/// AST node for a Number literal. Node type is
	/// <see cref="Rhino.Token.NUMBER">Rhino.Token.NUMBER</see>
	/// .<p>
	/// </remarks>
	public class NumberLiteral : AstNode
	{
		private string value;

		private double number;

		public NumberLiteral()
		{
			{
				type = Token.NUMBER;
			}
		}

		public NumberLiteral(int pos) : base(pos)
		{
			{
				type = Token.NUMBER;
			}
		}

		public NumberLiteral(int pos, int len) : base(pos, len)
		{
			{
				type = Token.NUMBER;
			}
		}

		/// <summary>Constructor.</summary>
		/// <remarks>
		/// Constructor.  Sets the length to the length of the
		/// <code>value</code>
		/// string.
		/// </remarks>
		public NumberLiteral(int pos, string value) : base(pos)
		{
			{
				type = Token.NUMBER;
			}
			SetValue(value);
			SetLength(value.Length);
		}

		/// <summary>Constructor.</summary>
		/// <remarks>
		/// Constructor.  Sets the length to the length of the
		/// <code>value</code>
		/// string.
		/// </remarks>
		public NumberLiteral(int pos, string value, double number) : this(pos, value)
		{
			SetDouble(number);
		}

		public NumberLiteral(double number)
		{
			{
				type = Token.NUMBER;
			}
			SetDouble(number);
			SetValue(double.ToString(number));
		}

		/// <summary>Returns the node's string value (the original source token)</summary>
		public virtual string GetValue()
		{
			return value;
		}

		/// <summary>Sets the node's value</summary>
		/// <exception cref="System.ArgumentException">
		/// } if value is
		/// <code>null</code>
		/// </exception>
		public virtual void SetValue(string value)
		{
			AssertNotNull(value);
			this.value = value;
		}

		/// <summary>
		/// Gets the
		/// <code>double</code>
		/// value.
		/// </summary>
		public virtual double GetNumber()
		{
			return number;
		}

		/// <summary>
		/// Sets the node's
		/// <code>double</code>
		/// value.
		/// </summary>
		public virtual void SetNumber(double value)
		{
			number = value;
		}

		public override string ToSource(int depth)
		{
			return MakeIndent(depth) + (value == null ? "<null>" : value);
		}

		/// <summary>Visits this node.</summary>
		/// <remarks>Visits this node.  There are no children to visit.</remarks>
		public override void Visit(NodeVisitor v)
		{
			v.Visit(this);
		}
	}
}
