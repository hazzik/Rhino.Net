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
	/// <summary>AST node representing an expression in a statement context.</summary>
	/// <remarks>
	/// AST node representing an expression in a statement context.  The node type is
	/// <see cref="Rhino.Token.EXPR_VOID">Rhino.Token.EXPR_VOID</see>
	/// if inside a function, or else
	/// <see cref="Rhino.Token.EXPR_RESULT">Rhino.Token.EXPR_RESULT</see>
	/// if inside a script.
	/// </remarks>
	public class ExpressionStatement : AstNode
	{
		private AstNode expr;

		/// <summary>
		/// Called by the parser to set node type to EXPR_RESULT
		/// if this node is not within a Function.
		/// </summary>
		/// <remarks>
		/// Called by the parser to set node type to EXPR_RESULT
		/// if this node is not within a Function.
		/// </remarks>
		public virtual void SetHasResult()
		{
			type = Token.EXPR_RESULT;
		}

		public ExpressionStatement()
		{
			{
				type = Token.EXPR_VOID;
			}
		}

		/// <summary>
		/// Constructs a new
		/// <code>ExpressionStatement</code>
		/// wrapping
		/// the specified expression.  Sets this node's position to the
		/// position of the wrapped node, and sets the wrapped node's
		/// position to zero.  Sets this node's length to the length of
		/// the wrapped node.
		/// </summary>
		/// <param name="expr">the wrapped expression</param>
		/// <param name="hasResult">
		/// 
		/// <code>true</code>
		/// if this expression has side
		/// effects.  If true, sets node type to EXPR_RESULT, else to EXPR_VOID.
		/// </param>
		public ExpressionStatement(AstNode expr, bool hasResult) : this(expr)
		{
			if (hasResult)
			{
				SetHasResult();
			}
		}

		/// <summary>
		/// Constructs a new
		/// <code>ExpressionStatement</code>
		/// wrapping
		/// the specified expression.  Sets this node's position to the
		/// position of the wrapped node, and sets the wrapped node's
		/// position to zero.  Sets this node's length to the length of
		/// the wrapped node.
		/// </summary>
		/// <param name="expr">the wrapped expression</param>
		public ExpressionStatement(AstNode expr) : this(expr.GetPosition(), expr.GetLength(), expr)
		{
		}

		public ExpressionStatement(int pos, int len) : base(pos, len)
		{
			{
				type = Token.EXPR_VOID;
			}
		}

		/// <summary>
		/// Constructs a new
		/// <code>ExpressionStatement</code>
		/// </summary>
		/// <param name="expr">
		/// the wrapped
		/// <see cref="AstNode">AstNode</see>
		/// .
		/// The
		/// <code>ExpressionStatement</code>
		/// 's bounds are set to those of expr,
		/// and expr's parent is set to this node.
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <code>expr</code>
		/// is null
		/// </exception>
		public ExpressionStatement(int pos, int len, AstNode expr) : base(pos, len)
		{
			{
				type = Token.EXPR_VOID;
			}
			SetExpression(expr);
		}

		/// <summary>Returns the wrapped expression</summary>
		public virtual AstNode GetExpression()
		{
			return expr;
		}

		/// <summary>Sets the wrapped expression, and sets its parent to this node.</summary>
		/// <remarks>Sets the wrapped expression, and sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// } if expression is
		/// <code>null</code>
		/// </exception>
		public virtual void SetExpression(AstNode expression)
		{
			AssertNotNull(expression);
			expr = expression;
			expression.SetParent(this);
			SetLineno(expression.GetLineno());
		}

		/// <summary>Returns true if this node has side effects</summary>
		/// <exception cref="System.InvalidOperationException">
		/// if expression has not yet
		/// been set.
		/// </exception>
		public override bool HasSideEffects()
		{
			return type == Token.EXPR_RESULT || expr.HasSideEffects();
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(expr.ToSource(depth));
			sb.Append(";\n");
			return sb.ToString();
		}

		/// <summary>Visits this node, then the wrapped statement.</summary>
		/// <remarks>Visits this node, then the wrapped statement.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				expr.Visit(v);
			}
		}
	}
}
