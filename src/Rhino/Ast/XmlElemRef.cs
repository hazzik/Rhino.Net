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
	/// AST node for an E4X XML
	/// <code>[expr]</code>
	/// member-ref expression.
	/// The node type is
	/// <see cref="Rhino.Token.REF_MEMBER">Rhino.Token.REF_MEMBER</see>
	/// .<p>
	/// Syntax:<p>
	/// <pre> @<i><sub>opt</sub></i> ns:: <i><sub>opt</sub></i> [ expr ]</pre>
	/// Examples include
	/// <code>ns::[expr]</code>
	/// ,
	/// <code>@ns::[expr]</code>
	/// ,
	/// <code>@[expr]</code>
	/// ,
	/// <code>*::[expr]</code>
	/// and
	/// <code>@*::[expr]</code>
	/// .<p>
	/// Note that the form
	/// <code>[expr]</code>
	/// (i.e. no namespace or
	/// attribute-qualifier) is not a legal
	/// <code>XmlElemRef</code>
	/// expression,
	/// since it's already used for standard JavaScript
	/// <see cref="ElementGet">ElementGet</see>
	/// array-indexing.  Hence, an
	/// <code>XmlElemRef</code>
	/// node always has
	/// either the attribute-qualifier, a non-
	/// <code>null</code>
	/// namespace node,
	/// or both.<p>
	/// The node starts at the
	/// <code>@</code>
	/// token, if present.  Otherwise it starts
	/// at the namespace name.  The node bounds extend through the closing
	/// right-bracket, or if it is missing due to a syntax error, through the
	/// end of the index expression.<p>
	/// </summary>
	public class XmlElemRef : XmlRef
	{
		private AstNode indexExpr;

		private int lb = -1;

		private int rb = -1;

		public XmlElemRef()
		{
			{
				type = Token.REF_MEMBER;
			}
		}

		public XmlElemRef(int pos) : base(pos)
		{
			{
				type = Token.REF_MEMBER;
			}
		}

		public XmlElemRef(int pos, int len) : base(pos, len)
		{
			{
				type = Token.REF_MEMBER;
			}
		}

		/// <summary>
		/// Returns index expression: the 'expr' in
		/// <code>@[expr]</code>
		/// or
		/// <code>@*::[expr]</code>
		/// .
		/// </summary>
		public virtual AstNode GetExpression()
		{
			return indexExpr;
		}

		/// <summary>Sets index expression, and sets its parent to this node.</summary>
		/// <remarks>Sets index expression, and sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <code>expr</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void SetExpression(AstNode expr)
		{
			AssertNotNull(expr);
			indexExpr = expr;
			expr.SetParent(this);
		}

		/// <summary>Returns left bracket position, or -1 if missing.</summary>
		/// <remarks>Returns left bracket position, or -1 if missing.</remarks>
		public virtual int GetLb()
		{
			return lb;
		}

		/// <summary>Sets left bracket position, or -1 if missing.</summary>
		/// <remarks>Sets left bracket position, or -1 if missing.</remarks>
		public virtual void SetLb(int lb)
		{
			this.lb = lb;
		}

		/// <summary>Returns left bracket position, or -1 if missing.</summary>
		/// <remarks>Returns left bracket position, or -1 if missing.</remarks>
		public virtual int GetRb()
		{
			return rb;
		}

		/// <summary>Sets right bracket position, -1 if missing.</summary>
		/// <remarks>Sets right bracket position, -1 if missing.</remarks>
		public virtual void SetRb(int rb)
		{
			this.rb = rb;
		}

		/// <summary>Sets both bracket positions.</summary>
		/// <remarks>Sets both bracket positions.</remarks>
		public virtual void SetBrackets(int lb, int rb)
		{
			this.lb = lb;
			this.rb = rb;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			if (IsAttributeAccess())
			{
				sb.Append("@");
			}
			if (@namespace != null)
			{
				sb.Append(@namespace.ToSource(0));
				sb.Append("::");
			}
			sb.Append("[");
			sb.Append(indexExpr.ToSource(0));
			sb.Append("]");
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, then the namespace if provided, then the
		/// index expression.
		/// </summary>
		/// <remarks>
		/// Visits this node, then the namespace if provided, then the
		/// index expression.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				if (@namespace != null)
				{
					@namespace.Visit(v);
				}
				indexExpr.Visit(v);
			}
		}
	}
}
