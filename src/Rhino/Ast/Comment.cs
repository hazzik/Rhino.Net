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
	/// <summary>Node representing comments.</summary>
	/// <remarks>
	/// Node representing comments.
	/// Node type is
	/// <see cref="Rhino.Token.COMMENT">Rhino.Token.COMMENT</see>
	/// .<p>
	/// <p>JavaScript effectively has five comment types:
	/// <ol>
	/// <li>// line comments</li>
	/// <li>/<span class="none">* block comments *\/</li>
	/// <li>/<span class="none">** jsdoc comments *\/</li>
	/// <li>&lt;!-- html-open line comments</li>
	/// <li>^\\s*--&gt; html-close line comments</li>
	/// </ol>
	/// <p>The first three should be familiar to Java programmers.  JsDoc comments
	/// are really just block comments with some conventions about the formatting
	/// within the comment delimiters.  Line and block comments are described in the
	/// Ecma-262 specification. <p>
	/// <p>SpiderMonkey and Rhino also support HTML comment syntax, but somewhat
	/// counterintuitively, the syntax does not produce a block comment.  Instead,
	/// everything from the string &lt;!-- through the end of the line is considered
	/// a comment, and if the token --&gt; is the first non-whitespace on the line,
	/// then the line is considered a line comment.  This is to support parsing
	/// JavaScript in &lt;script&gt; HTML tags that has been "hidden" from very old
	/// browsers by surrounding it with HTML comment delimiters. <p>
	/// Note the node start position for Comment nodes is still relative to the
	/// parent, but Comments are always stored directly in the AstRoot node, so
	/// they are also effectively absolute offsets.
	/// </remarks>
	public class Comment : AstNode
	{
		private string value;

		private Token.CommentType commentType;

		/// <summary>Constructs a new Comment</summary>
		/// <param name="pos">the start position</param>
		/// <param name="len">the length including delimiter(s)</param>
		/// <param name="type">the comment type</param>
		/// <param name="value">the value of the comment, as a string</param>
		public Comment(int pos, int len, Token.CommentType type, string value) : base(pos, len)
		{
			{
				type = Token.COMMENT;
			}
			commentType = type;
			this.value = value;
		}

		/// <summary>Returns the comment style</summary>
		public virtual Token.CommentType GetCommentType()
		{
			return commentType;
		}

		/// <summary>Sets the comment style</summary>
		/// <param name="type">
		/// the comment style, a
		/// <see cref="Rhino.Token.CommentType">Rhino.Token.CommentType</see>
		/// </param>
		public virtual void SetCommentType(Token.CommentType type)
		{
			this.commentType = type;
		}

		/// <summary>Returns a string of the comment value.</summary>
		/// <remarks>Returns a string of the comment value.</remarks>
		public virtual string GetValue()
		{
			return value;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder(GetLength() + 10);
			sb.Append(MakeIndent(depth));
			sb.Append(value);
			return sb.ToString();
		}

		/// <summary>
		/// Comment nodes are not visited during normal visitor traversals,
		/// but comply with the
		/// <see cref="AstNode.Visit(NodeVisitor)">AstNode.Visit(NodeVisitor)</see>
		/// interface.
		/// </summary>
		public override void Visit(NodeVisitor v)
		{
			v.Visit(this);
		}
	}
}
