/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>Try/catch/finally statement.</summary>
	/// <remarks>
	/// Try/catch/finally statement.  Node type is
	/// <see cref="Rhino.Token.TRY">Rhino.Token.TRY</see>
	/// .<p>
	/// <pre><i>TryStatement</i> :
	/// <b>try</b> Block Catch
	/// <b>try</b> Block Finally
	/// <b>try</b> Block Catch Finally
	/// <i>Catch</i> :
	/// <b>catch</b> ( <i><b>Identifier</b></i> ) Block
	/// <i>Finally</i> :
	/// <b>finally</b> Block</pre>
	/// </remarks>
	public class TryStatement : AstNode
	{
		private static readonly IList<CatchClause> NO_CATCHES = Sharpen.Collections.UnmodifiableList(new List<CatchClause>());

		private AstNode tryBlock;

		private IList<CatchClause> catchClauses;

		private AstNode finallyBlock;

		private int finallyPosition = -1;

		public TryStatement()
		{
			{
				type = Token.TRY;
			}
		}

		public TryStatement(int pos) : base(pos)
		{
			{
				type = Token.TRY;
			}
		}

		public TryStatement(int pos, int len) : base(pos, len)
		{
			{
				type = Token.TRY;
			}
		}

		public virtual AstNode GetTryBlock()
		{
			return tryBlock;
		}

		/// <summary>Sets try block.</summary>
		/// <remarks>Sets try block.  Also sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// } if
		/// <code>tryBlock</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void SetTryBlock(AstNode tryBlock)
		{
			AssertNotNull(tryBlock);
			this.tryBlock = tryBlock;
			tryBlock.SetParent(this);
		}

		/// <summary>
		/// Returns list of
		/// <see cref="CatchClause">CatchClause</see>
		/// nodes.  If there are no catch
		/// clauses, returns an immutable empty list.
		/// </summary>
		public virtual IList<CatchClause> GetCatchClauses()
		{
			return catchClauses != null ? catchClauses : NO_CATCHES;
		}

		/// <summary>
		/// Sets list of
		/// <see cref="CatchClause">CatchClause</see>
		/// nodes.  Also sets their parents
		/// to this node.  May be
		/// <code>null</code>
		/// .  Replaces any existing catch
		/// clauses for this node.
		/// </summary>
		public virtual void SetCatchClauses(IList<CatchClause> catchClauses)
		{
			if (catchClauses == null)
			{
				this.catchClauses = null;
			}
			else
			{
				if (this.catchClauses != null)
				{
					this.catchClauses.Clear();
				}
				foreach (CatchClause cc in catchClauses)
				{
					AddCatchClause(cc);
				}
			}
		}

		/// <summary>
		/// Add a catch-clause to the end of the list, and sets its parent to
		/// this node.
		/// </summary>
		/// <remarks>
		/// Add a catch-clause to the end of the list, and sets its parent to
		/// this node.
		/// </remarks>
		/// <exception cref="System.ArgumentException">
		/// } if
		/// <code>clause</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void AddCatchClause(CatchClause clause)
		{
			AssertNotNull(clause);
			if (catchClauses == null)
			{
				catchClauses = new List<CatchClause>();
			}
			catchClauses.AddItem(clause);
			clause.SetParent(this);
		}

		/// <summary>
		/// Returns finally block, or
		/// <code>null</code>
		/// if not present
		/// </summary>
		public virtual AstNode GetFinallyBlock()
		{
			return finallyBlock;
		}

		/// <summary>Sets finally block, and sets its parent to this node.</summary>
		/// <remarks>
		/// Sets finally block, and sets its parent to this node.
		/// May be
		/// <code>null</code>
		/// .
		/// </remarks>
		public virtual void SetFinallyBlock(AstNode finallyBlock)
		{
			this.finallyBlock = finallyBlock;
			if (finallyBlock != null)
			{
				finallyBlock.SetParent(this);
			}
		}

		/// <summary>
		/// Returns position of
		/// <code>finally</code>
		/// keyword, if present, or -1
		/// </summary>
		public virtual int GetFinallyPosition()
		{
			return finallyPosition;
		}

		/// <summary>
		/// Sets position of
		/// <code>finally</code>
		/// keyword, if present, or -1
		/// </summary>
		public virtual void SetFinallyPosition(int finallyPosition)
		{
			this.finallyPosition = finallyPosition;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder(250);
			sb.Append(MakeIndent(depth));
			sb.Append("try ");
			sb.Append(tryBlock.ToSource(depth).Trim());
			foreach (CatchClause cc in GetCatchClauses())
			{
				sb.Append(cc.ToSource(depth));
			}
			if (finallyBlock != null)
			{
				sb.Append(" finally ");
				sb.Append(finallyBlock.ToSource(depth));
			}
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, then the try-block, then any catch clauses,
		/// and then any finally block.
		/// </summary>
		/// <remarks>
		/// Visits this node, then the try-block, then any catch clauses,
		/// and then any finally block.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				tryBlock.Visit(v);
				foreach (CatchClause cc in GetCatchClauses())
				{
					cc.Visit(v);
				}
				if (finallyBlock != null)
				{
					finallyBlock.Visit(v);
				}
			}
		}
	}
}
