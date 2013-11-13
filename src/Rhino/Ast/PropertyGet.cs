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
	/// <summary>AST node for the '.' operator.</summary>
	/// <remarks>
	/// AST node for the '.' operator.  Node type is
	/// <see cref="Rhino.Token.GETPROP">Rhino.Token.GETPROP</see>
	/// .
	/// </remarks>
	public class PropertyGet : InfixExpression
	{
		public PropertyGet()
		{
			{
				type = Token.GETPROP;
			}
		}

		public PropertyGet(int pos) : base(pos)
		{
			{
				type = Token.GETPROP;
			}
		}

		public PropertyGet(int pos, int len) : base(pos, len)
		{
			{
				type = Token.GETPROP;
			}
		}

		public PropertyGet(int pos, int len, AstNode target, Name property) : base(pos, len, target, property)
		{
			{
				type = Token.GETPROP;
			}
		}

		/// <summary>Constructor.</summary>
		/// <remarks>
		/// Constructor.  Updates bounds to include left (
		/// <code>target</code>
		/// ) and
		/// right (
		/// <code>property</code>
		/// ) nodes.
		/// </remarks>
		public PropertyGet(AstNode target, Name property) : base(target, property)
		{
			{
				type = Token.GETPROP;
			}
		}

		public PropertyGet(AstNode target, Name property, int dotPosition) : base(Token.GETPROP, target, property, dotPosition)
		{
			{
				type = Token.GETPROP;
			}
		}

		/// <summary>Returns the object on which the property is being fetched.</summary>
		/// <remarks>
		/// Returns the object on which the property is being fetched.
		/// Should never be
		/// <code>null</code>
		/// .
		/// </remarks>
		public virtual AstNode GetTarget()
		{
			return GetLeft();
		}

		/// <summary>Sets target object, and sets its parent to this node.</summary>
		/// <remarks>Sets target object, and sets its parent to this node.</remarks>
		/// <param name="target">
		/// expression evaluating to the object upon which
		/// to do the property lookup
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// } if
		/// <code>target</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void SetTarget(AstNode target)
		{
			SetLeft(target);
		}

		/// <summary>Returns the property being accessed.</summary>
		/// <remarks>Returns the property being accessed.</remarks>
		public virtual Name GetProperty()
		{
			return (Name)GetRight();
		}

		/// <summary>Sets the property being accessed, and sets its parent to this node.</summary>
		/// <remarks>Sets the property being accessed, and sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// } if
		/// <code>property</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void SetProperty(Name property)
		{
			SetRight(property);
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append(GetLeft().ToSource(0));
			sb.Append(".");
			sb.Append(GetRight().ToSource(0));
			return sb.ToString();
		}

		/// <summary>Visits this node, the target expression, and the property name.</summary>
		/// <remarks>Visits this node, the target expression, and the property name.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				GetTarget().Visit(v);
				GetProperty().Visit(v);
			}
		}
	}
}
