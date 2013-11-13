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
	/// AST node for an indexed property reference, such as
	/// <code>foo['bar']</code>
	/// or
	/// <code>foo[2]</code>
	/// .  This is sometimes called an "element-get" operation, hence
	/// the name of the node.<p>
	/// Node type is
	/// <see cref="Rhino.Token.GETELEM">Rhino.Token.GETELEM</see>
	/// .<p>
	/// The node bounds extend from the beginning position of the target through the
	/// closing right-bracket.  In the presence of a syntax error, the right bracket
	/// position is -1, and the node ends at the end of the element expression.
	/// </summary>
	public class ElementGet : AstNode
	{
		private AstNode target;

		private AstNode element;

		private int lb = -1;

		private int rb = -1;

		public ElementGet()
		{
			{
				type = Token.GETELEM;
			}
		}

		public ElementGet(int pos) : base(pos)
		{
			{
				type = Token.GETELEM;
			}
		}

		public ElementGet(int pos, int len) : base(pos, len)
		{
			{
				type = Token.GETELEM;
			}
		}

		public ElementGet(AstNode target, AstNode element)
		{
			{
				type = Token.GETELEM;
			}
			SetTarget(target);
			SetElement(element);
		}

		/// <summary>Returns the object on which the element is being fetched.</summary>
		/// <remarks>Returns the object on which the element is being fetched.</remarks>
		public virtual AstNode GetTarget()
		{
			return target;
		}

		/// <summary>Sets target object, and sets its parent to this node.</summary>
		/// <remarks>Sets target object, and sets its parent to this node.</remarks>
		/// <param name="target">
		/// expression evaluating to the object upon which
		/// to do the element lookup
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// if target is
		/// <code>null</code>
		/// </exception>
		public virtual void SetTarget(AstNode target)
		{
			AssertNotNull(target);
			this.target = target;
			target.SetParent(this);
		}

		/// <summary>Returns the element being accessed</summary>
		public virtual AstNode GetElement()
		{
			return element;
		}

		/// <summary>Sets the element being accessed, and sets its parent to this node.</summary>
		/// <remarks>Sets the element being accessed, and sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// if element is
		/// <code>null</code>
		/// </exception>
		public virtual void SetElement(AstNode element)
		{
			AssertNotNull(element);
			this.element = element;
			element.SetParent(this);
		}

		/// <summary>Returns left bracket position</summary>
		public virtual int GetLb()
		{
			return lb;
		}

		/// <summary>Sets left bracket position</summary>
		public virtual void SetLb(int lb)
		{
			this.lb = lb;
		}

		/// <summary>Returns right bracket position, -1 if missing</summary>
		public virtual int GetRb()
		{
			return rb;
		}

		/// <summary>Sets right bracket position, -1 if not present</summary>
		public virtual void SetRb(int rb)
		{
			this.rb = rb;
		}

		public virtual void SetParens(int lb, int rb)
		{
			this.lb = lb;
			this.rb = rb;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append(target.ToSource(0));
			sb.Append("[");
			sb.Append(element.ToSource(0));
			sb.Append("]");
			return sb.ToString();
		}

		/// <summary>Visits this node, the target, and the index expression.</summary>
		/// <remarks>Visits this node, the target, and the index expression.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				target.Visit(v);
				element.Visit(v);
			}
		}
	}
}
