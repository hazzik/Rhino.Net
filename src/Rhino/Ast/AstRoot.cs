/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>Node for the root of a parse tree.</summary>
	/// <remarks>
	/// Node for the root of a parse tree.  It contains the statements and functions
	/// in the script, and a list of
	/// <see cref="Comment">Comment</see>
	/// nodes associated with the script
	/// as a whole.  Node type is
	/// <see cref="Rhino.Token.SCRIPT">Rhino.Token.SCRIPT</see>
	/// . <p>
	/// Note that the tree itself does not store errors.  To collect the parse errors
	/// and warnings, pass an
	/// <see cref="Rhino.ErrorReporter">Rhino.ErrorReporter</see>
	/// to the
	/// <see cref="Rhino.Parser">Rhino.Parser</see>
	/// via the
	/// <see cref="Rhino.CompilerEnvirons">Rhino.CompilerEnvirons</see>
	/// .
	/// </remarks>
	public class AstRoot : ScriptNode
	{
		private ICollection<Comment> comments;

		private bool inStrictMode;

		public AstRoot()
		{
			{
				type = Token.SCRIPT;
			}
		}

		public AstRoot(int pos) : base(pos)
		{
			{
				type = Token.SCRIPT;
			}
		}

		/// <summary>Returns comment set</summary>
		/// <returns>
		/// comment set, sorted by start position. Can be
		/// <code>null</code>
		/// .
		/// </returns>
		public virtual ICollection<Comment> GetComments()
		{
			return comments;
		}

		/// <summary>
		/// Sets comment list, and updates the parent of each entry to point
		/// to this node.
		/// </summary>
		/// <remarks>
		/// Sets comment list, and updates the parent of each entry to point
		/// to this node.  Replaces any existing comments.
		/// </remarks>
		/// <param name="comments">
		/// comment list.  can be
		/// <code>null</code>
		/// .
		/// </param>
		public virtual void SetComments(ICollection<Comment> comments)
		{
			if (comments == null)
			{
				this.comments = null;
			}
			else
			{
				if (this.comments != null)
				{
					this.comments.Clear();
				}
				foreach (Comment c in comments)
				{
					AddComment(c);
				}
			}
		}

		/// <summary>Add a comment to the comment set.</summary>
		/// <remarks>Add a comment to the comment set.</remarks>
		/// <param name="comment">the comment node.</param>
		/// <exception cref="System.ArgumentException">
		/// if comment is
		/// <code>null</code>
		/// </exception>
		public virtual void AddComment(Comment comment)
		{
			AssertNotNull(comment);
			if (comments == null)
			{
				comments = new TreeSet<Comment>(new AstNode.PositionComparator());
			}
			comments.AddItem(comment);
			comment.SetParent(this);
		}

		public virtual void SetInStrictMode(bool inStrictMode)
		{
			this.inStrictMode = inStrictMode;
		}

		public virtual bool IsInStrictMode()
		{
			return inStrictMode;
		}

		/// <summary>Visits the comment nodes in the order they appear in the source code.</summary>
		/// <remarks>
		/// Visits the comment nodes in the order they appear in the source code.
		/// The comments are not visited by the
		/// <see cref="ScriptNode.Visit(NodeVisitor)">ScriptNode.Visit(NodeVisitor)</see>
		/// function - you must
		/// use this function to visit them.
		/// </remarks>
		/// <param name="visitor">
		/// the callback object.  It is passed each comment node.
		/// The return value is ignored.
		/// </param>
		public virtual void VisitComments(NodeVisitor visitor)
		{
			if (comments != null)
			{
				foreach (Comment c in comments)
				{
					visitor.Visit(c);
				}
			}
		}

		/// <summary>Visits the AST nodes, then the comment nodes.</summary>
		/// <remarks>
		/// Visits the AST nodes, then the comment nodes.
		/// This method is equivalent to calling
		/// <see cref="ScriptNode.Visit(NodeVisitor)">ScriptNode.Visit(NodeVisitor)</see>
		/// , then
		/// <see cref="VisitComments(NodeVisitor)">VisitComments(NodeVisitor)</see>
		/// .  The return value
		/// is ignored while visiting comment nodes.
		/// </remarks>
		/// <param name="visitor">the callback object.</param>
		public virtual void VisitAll(NodeVisitor visitor)
		{
			Visit(visitor);
			VisitComments(visitor);
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			foreach (Node node in this)
			{
				sb.Append(((AstNode)node).ToSource(depth));
			}
			return sb.ToString();
		}

		/// <summary>A debug-printer that includes comments (at the end).</summary>
		/// <remarks>A debug-printer that includes comments (at the end).</remarks>
		public override string DebugPrint()
		{
			AstNode.DebugPrintVisitor dpv = new AstNode.DebugPrintVisitor(new StringBuilder(1000));
			VisitAll(dpv);
			return dpv.ToString();
		}

		/// <summary>
		/// Debugging function to check that the parser has set the parent
		/// link for every node in the tree.
		/// </summary>
		/// <remarks>
		/// Debugging function to check that the parser has set the parent
		/// link for every node in the tree.
		/// </remarks>
		/// <exception cref="System.InvalidOperationException">if a parent link is missing</exception>
		public virtual void CheckParentLinks()
		{
			this.Visit(new _NodeVisitor_139());
		}

		private sealed class _NodeVisitor_139 : NodeVisitor
		{
			public _NodeVisitor_139()
			{
			}

			public bool Visit(AstNode node)
			{
				int type = node.GetType();
				if (type == Token.SCRIPT)
				{
					return true;
				}
				if (node.GetParent() == null)
				{
					throw new InvalidOperationException("No parent for node: " + node + "\n" + node.ToSource(0));
				}
				return true;
			}
		}
	}
}
