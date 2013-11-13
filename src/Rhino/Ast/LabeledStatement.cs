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
	/// <summary>A labeled statement.</summary>
	/// <remarks>
	/// A labeled statement.  A statement can have more than one label.  In
	/// this AST representation, all labels for a statement are collapsed into
	/// the "labels" list of a single
	/// <see cref="LabeledStatement">LabeledStatement</see>
	/// node. <p>
	/// Node type is
	/// <see cref="Rhino.Token.EXPR_VOID">Rhino.Token.EXPR_VOID</see>
	/// . <p>
	/// </remarks>
	public class LabeledStatement : AstNode
	{
		private IList<Label> labels = new AList<Label>();

		private AstNode statement;

		public LabeledStatement()
		{
			{
				// always at least 1
				type = Token.EXPR_VOID;
			}
		}

		public LabeledStatement(int pos) : base(pos)
		{
			{
				type = Token.EXPR_VOID;
			}
		}

		public LabeledStatement(int pos, int len) : base(pos, len)
		{
			{
				type = Token.EXPR_VOID;
			}
		}

		/// <summary>Returns label list</summary>
		public virtual IList<Label> GetLabels()
		{
			return labels;
		}

		/// <summary>
		/// Sets label list, setting the parent of each label
		/// in the list.
		/// </summary>
		/// <remarks>
		/// Sets label list, setting the parent of each label
		/// in the list.  Replaces any existing labels.
		/// </remarks>
		/// <exception cref="System.ArgumentException">
		/// } if labels is
		/// <code>null</code>
		/// </exception>
		public virtual void SetLabels(IList<Label> labels)
		{
			AssertNotNull(labels);
			if (this.labels != null)
			{
				this.labels.Clear();
			}
			foreach (Label l in labels)
			{
				AddLabel(l);
			}
		}

		/// <summary>Adds a label and sets its parent to this node.</summary>
		/// <remarks>Adds a label and sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// } if label is
		/// <code>null</code>
		/// </exception>
		public virtual void AddLabel(Label label)
		{
			AssertNotNull(label);
			labels.AddItem(label);
			label.SetParent(this);
		}

		/// <summary>Returns the labeled statement</summary>
		public virtual AstNode GetStatement()
		{
			return statement;
		}

		/// <summary>
		/// Returns label with specified name from the label list for
		/// this labeled statement.
		/// </summary>
		/// <remarks>
		/// Returns label with specified name from the label list for
		/// this labeled statement.  Returns
		/// <code>null</code>
		/// if there is no
		/// label with that name in the list.
		/// </remarks>
		public virtual Label GetLabelByName(string name)
		{
			foreach (Label label in labels)
			{
				if (name.Equals(label.GetName()))
				{
					return label;
				}
			}
			return null;
		}

		/// <summary>Sets the labeled statement, and sets its parent to this node.</summary>
		/// <remarks>Sets the labeled statement, and sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <code>statement</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void SetStatement(AstNode statement)
		{
			AssertNotNull(statement);
			this.statement = statement;
			statement.SetParent(this);
		}

		public virtual Label GetFirstLabel()
		{
			return labels[0];
		}

		public override bool HasSideEffects()
		{
			// just to avoid the default case for EXPR_VOID in AstNode
			return true;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			foreach (Label label in labels)
			{
				sb.Append(label.ToSource(depth));
			}
			// prints newline
			sb.Append(statement.ToSource(depth + 1));
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, then each label in the label-list, and finally the
		/// statement.
		/// </summary>
		/// <remarks>
		/// Visits this node, then each label in the label-list, and finally the
		/// statement.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				foreach (AstNode label in labels)
				{
					label.Visit(v);
				}
				statement.Visit(v);
			}
		}
	}
}
