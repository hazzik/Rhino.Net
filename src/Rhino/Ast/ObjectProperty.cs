/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>AST node for a single name:value entry in an Object literal.</summary>
	/// <remarks>
	/// AST node for a single name:value entry in an Object literal.
	/// For simple entries, the node type is
	/// <see cref="Rhino.Token.COLON">Rhino.Token.COLON</see>
	/// , and
	/// the name (left side expression) is either a
	/// <see cref="Name">Name</see>
	/// , a
	/// <see cref="StringLiteral">StringLiteral</see>
	/// or a
	/// <see cref="NumberLiteral">NumberLiteral</see>
	/// .<p>
	/// This node type is also used for getter/setter properties in object
	/// literals.  In this case the node bounds include the "get" or "set"
	/// keyword.  The left-hand expression in this case is always a
	/// <see cref="Name">Name</see>
	/// , and the overall node type is
	/// <see cref="Rhino.Token.GET">Rhino.Token.GET</see>
	/// or
	/// <see cref="Rhino.Token.SET">Rhino.Token.SET</see>
	/// , as appropriate.<p>
	/// The
	/// <code>operatorPosition</code>
	/// field is meaningless if the node is
	/// a getter or setter.<p>
	/// <pre><i>ObjectProperty</i> :
	/// PropertyName <b>:</b> AssignmentExpression
	/// <i>PropertyName</i> :
	/// Identifier
	/// StringLiteral
	/// NumberLiteral</pre>
	/// </remarks>
	public class ObjectProperty : InfixExpression
	{
		/// <summary>Sets the node type.</summary>
		/// <remarks>
		/// Sets the node type.  Must be one of
		/// <see cref="Rhino.Token.COLON">Rhino.Token.COLON</see>
		/// ,
		/// <see cref="Rhino.Token.GET">Rhino.Token.GET</see>
		/// , or
		/// <see cref="Rhino.Token.SET">Rhino.Token.SET</see>
		/// .
		/// </remarks>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <code>nodeType</code>
		/// is invalid
		/// </exception>
		public virtual void SetNodeType(int nodeType)
		{
			if (nodeType != Token.COLON && nodeType != Token.GET && nodeType != Token.SET)
			{
				throw new ArgumentException("invalid node type: " + nodeType);
			}
			SetType(nodeType);
		}

		public ObjectProperty()
		{
			{
				type = Token.COLON;
			}
		}

		public ObjectProperty(int pos) : base(pos)
		{
			{
				type = Token.COLON;
			}
		}

		public ObjectProperty(int pos, int len) : base(pos, len)
		{
			{
				type = Token.COLON;
			}
		}

		/// <summary>Marks this node as a "getter" property.</summary>
		/// <remarks>Marks this node as a "getter" property.</remarks>
		public virtual void SetIsGetter()
		{
			type = Token.GET;
		}

		/// <summary>Returns true if this is a getter function.</summary>
		/// <remarks>Returns true if this is a getter function.</remarks>
		public virtual bool IsGetter()
		{
			return type == Token.GET;
		}

		/// <summary>Marks this node as a "setter" property.</summary>
		/// <remarks>Marks this node as a "setter" property.</remarks>
		public virtual void SetIsSetter()
		{
			type = Token.SET;
		}

		/// <summary>Returns true if this is a setter function.</summary>
		/// <remarks>Returns true if this is a setter function.</remarks>
		public virtual bool IsSetter()
		{
			return type == Token.SET;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			if (IsGetter())
			{
				sb.Append("get ");
			}
			else
			{
				if (IsSetter())
				{
					sb.Append("set ");
				}
			}
			sb.Append(left.ToSource(0));
			if (type == Token.COLON)
			{
				sb.Append(": ");
			}
			sb.Append(right.ToSource(0));
			return sb.ToString();
		}
	}
}
