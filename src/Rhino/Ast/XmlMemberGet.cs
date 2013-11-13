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
	/// <summary>
	/// AST node for E4X ".@" and ".." expressions, such as
	/// <code>foo..bar</code>
	/// ,
	/// <code>foo..@bar</code>
	/// ,
	/// <code>@foo.@bar</code>
	/// , and
	/// <code>foo..@ns::*</code>
	/// .  The right-hand node is always an
	/// <see cref="XmlRef">XmlRef</see>
	/// . <p>
	/// Node type is
	/// <see cref="Rhino.Token.DOT">Rhino.Token.DOT</see>
	/// or
	/// <see cref="Rhino.Token.DOTDOT">Rhino.Token.DOTDOT</see>
	/// .
	/// </summary>
	public class XmlMemberGet : InfixExpression
	{
		public XmlMemberGet()
		{
			{
				type = Token.DOTDOT;
			}
		}

		public XmlMemberGet(int pos) : base(pos)
		{
			{
				type = Token.DOTDOT;
			}
		}

		public XmlMemberGet(int pos, int len) : base(pos, len)
		{
			{
				type = Token.DOTDOT;
			}
		}

		public XmlMemberGet(int pos, int len, AstNode target, XmlRef @ref) : base(pos, len, target, @ref)
		{
			{
				type = Token.DOTDOT;
			}
		}

		/// <summary>
		/// Constructs a new
		/// <code>XmlMemberGet</code>
		/// node.
		/// Updates bounds to include
		/// <code>target</code>
		/// and
		/// <code>ref</code>
		/// nodes.
		/// </summary>
		public XmlMemberGet(AstNode target, XmlRef @ref) : base(target, @ref)
		{
			{
				type = Token.DOTDOT;
			}
		}

		public XmlMemberGet(AstNode target, XmlRef @ref, int opPos) : base(Token.DOTDOT, target, @ref, opPos)
		{
			{
				type = Token.DOTDOT;
			}
		}

		/// <summary>
		/// Returns the object on which the XML member-ref expression
		/// is being evaluated.
		/// </summary>
		/// <remarks>
		/// Returns the object on which the XML member-ref expression
		/// is being evaluated.  Should never be
		/// <code>null</code>
		/// .
		/// </remarks>
		public virtual AstNode GetTarget()
		{
			return GetLeft();
		}

		/// <summary>Sets target object, and sets its parent to this node.</summary>
		/// <remarks>Sets target object, and sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <code>target</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void SetTarget(AstNode target)
		{
			SetLeft(target);
		}

		/// <summary>Returns the right-side XML member ref expression.</summary>
		/// <remarks>
		/// Returns the right-side XML member ref expression.
		/// Should never be
		/// <code>null</code>
		/// unless the code is malformed.
		/// </remarks>
		public virtual XmlRef GetMemberRef()
		{
			return (XmlRef)GetRight();
		}

		/// <summary>
		/// Sets the XML member-ref expression, and sets its parent
		/// to this node.
		/// </summary>
		/// <remarks>
		/// Sets the XML member-ref expression, and sets its parent
		/// to this node.
		/// </remarks>
		/// <exception cref="System.ArgumentException">
		/// if property is
		/// <code>null</code>
		/// </exception>
		public virtual void SetProperty(XmlRef @ref)
		{
			SetRight(@ref);
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append(GetLeft().ToSource(0));
			sb.Append(OperatorToString(GetType()));
			sb.Append(GetRight().ToSource(0));
			return sb.ToString();
		}
	}
}
