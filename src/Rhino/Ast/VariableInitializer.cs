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
	/// <summary>
	/// A variable declaration or initializer, part of a
	/// <see cref="VariableDeclaration">VariableDeclaration</see>
	/// expression.  The variable "target" can be a simple name or a destructuring
	/// form.  The initializer, if present, can be any expression.<p>
	/// Node type is one of
	/// <see cref="Rhino.Token.VAR">Rhino.Token.VAR</see>
	/// ,
	/// <see cref="Rhino.Token.CONST">Rhino.Token.CONST</see>
	/// , or
	/// <see cref="Rhino.Token.LET">Rhino.Token.LET</see>
	/// .<p>
	/// </summary>
	public class VariableInitializer : AstNode
	{
		private AstNode target;

		private AstNode initializer;

		/// <summary>Sets the node type.</summary>
		/// <remarks>Sets the node type.</remarks>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <code>nodeType</code>
		/// is not one of
		/// <see cref="Rhino.Token.VAR">Rhino.Token.VAR</see>
		/// ,
		/// <see cref="Rhino.Token.CONST">Rhino.Token.CONST</see>
		/// , or
		/// <see cref="Rhino.Token.LET">Rhino.Token.LET</see>
		/// </exception>
		public virtual void SetNodeType(int nodeType)
		{
			if (nodeType != Token.VAR && nodeType != Token.CONST && nodeType != Token.LET)
			{
				throw new ArgumentException("invalid node type");
			}
			SetType(nodeType);
		}

		public VariableInitializer()
		{
			{
				type = Token.VAR;
			}
		}

		public VariableInitializer(int pos) : base(pos)
		{
			{
				type = Token.VAR;
			}
		}

		public VariableInitializer(int pos, int len) : base(pos, len)
		{
			{
				type = Token.VAR;
			}
		}

		/// <summary>Returns true if this is a destructuring assignment.</summary>
		/// <remarks>
		/// Returns true if this is a destructuring assignment.  If so, the
		/// initializer must be non-
		/// <code>null</code>
		/// .
		/// </remarks>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if the
		/// <code>target</code>
		/// field is a destructuring form
		/// (an
		/// <see cref="ArrayLiteral">ArrayLiteral</see>
		/// or
		/// <see cref="ObjectLiteral">ObjectLiteral</see>
		/// node)
		/// </returns>
		public virtual bool IsDestructuring()
		{
			return !(target is Name);
		}

		/// <summary>Returns the variable name or destructuring form</summary>
		public virtual AstNode GetTarget()
		{
			return target;
		}

		/// <summary>
		/// Sets the variable name or destructuring form, and sets
		/// its parent to this node.
		/// </summary>
		/// <remarks>
		/// Sets the variable name or destructuring form, and sets
		/// its parent to this node.
		/// </remarks>
		/// <exception cref="System.ArgumentException">
		/// if target is
		/// <code>null</code>
		/// </exception>
		public virtual void SetTarget(AstNode target)
		{
			// Don't throw exception if target is an "invalid" node type.
			// See mozilla/js/tests/js1_7/block/regress-350279.js
			if (target == null)
			{
				throw new ArgumentException("invalid target arg");
			}
			this.target = target;
			target.SetParent(this);
		}

		/// <summary>
		/// Returns the initial value, or
		/// <code>null</code>
		/// if not provided
		/// </summary>
		public virtual AstNode GetInitializer()
		{
			return initializer;
		}

		/// <summary>Sets the initial value expression, and sets its parent to this node.</summary>
		/// <remarks>Sets the initial value expression, and sets its parent to this node.</remarks>
		/// <param name="initializer">
		/// the initial value.  May be
		/// <code>null</code>
		/// .
		/// </param>
		public virtual void SetInitializer(AstNode initializer)
		{
			this.initializer = initializer;
			if (initializer != null)
			{
				initializer.SetParent(this);
			}
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append(target.ToSource(0));
			if (initializer != null)
			{
				sb.Append(" = ");
				sb.Append(initializer.ToSource(0));
			}
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, then the target expression, then the initializer
		/// expression if present.
		/// </summary>
		/// <remarks>
		/// Visits this node, then the target expression, then the initializer
		/// expression if present.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				target.Visit(v);
				if (initializer != null)
				{
					initializer.Visit(v);
				}
			}
		}
	}
}
