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
	/// <summary>For-in or for-each-in statement.</summary>
	/// <remarks>
	/// For-in or for-each-in statement.  Node type is
	/// <see cref="Rhino.Token.FOR">Rhino.Token.FOR</see>
	/// .<p>
	/// <pre><b>for</b> [<b>each</b>] ( LeftHandSideExpression <b>in</b> Expression ) Statement</pre>
	/// <pre><b>for</b> [<b>each</b>] ( <b>var</b> VariableDeclarationNoIn <b>in</b> Expression ) Statement</pre>
	/// </remarks>
	public class ForInLoop : Loop
	{
		protected internal AstNode iterator;

		protected internal AstNode iteratedObject;

		protected internal int inPosition = -1;

		protected internal int eachPosition = -1;

		protected internal bool isForEach;

		public ForInLoop()
		{
			{
				type = Token.FOR;
			}
		}

		public ForInLoop(int pos) : base(pos)
		{
			{
				type = Token.FOR;
			}
		}

		public ForInLoop(int pos, int len) : base(pos, len)
		{
			{
				type = Token.FOR;
			}
		}

		/// <summary>Returns loop iterator expression</summary>
		public virtual AstNode GetIterator()
		{
			return iterator;
		}

		/// <summary>Sets loop iterator expression:  the part before the "in" keyword.</summary>
		/// <remarks>
		/// Sets loop iterator expression:  the part before the "in" keyword.
		/// Also sets its parent to this node.
		/// </remarks>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <code>iterator</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void SetIterator(AstNode iterator)
		{
			AssertNotNull(iterator);
			this.iterator = iterator;
			iterator.SetParent(this);
		}

		/// <summary>Returns object being iterated over</summary>
		public virtual AstNode GetIteratedObject()
		{
			return iteratedObject;
		}

		/// <summary>Sets object being iterated over, and sets its parent to this node.</summary>
		/// <remarks>Sets object being iterated over, and sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <code>object</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void SetIteratedObject(AstNode @object)
		{
			AssertNotNull(@object);
			this.iteratedObject = @object;
			@object.SetParent(this);
		}

		/// <summary>Returns whether the loop is a for-each loop</summary>
		public virtual bool IsForEach()
		{
			return isForEach;
		}

		/// <summary>Sets whether the loop is a for-each loop</summary>
		public virtual void SetIsForEach(bool isForEach)
		{
			this.isForEach = isForEach;
		}

		/// <summary>Returns position of "in" keyword</summary>
		public virtual int GetInPosition()
		{
			return inPosition;
		}

		/// <summary>Sets position of "in" keyword</summary>
		/// <param name="inPosition">
		/// position of "in" keyword,
		/// or -1 if not present (e.g. in presence of a syntax error)
		/// </param>
		public virtual void SetInPosition(int inPosition)
		{
			this.inPosition = inPosition;
		}

		/// <summary>Returns position of "each" keyword</summary>
		public virtual int GetEachPosition()
		{
			return eachPosition;
		}

		/// <summary>Sets position of "each" keyword</summary>
		/// <param name="eachPosition">
		/// position of "each" keyword,
		/// or -1 if not present.
		/// </param>
		public virtual void SetEachPosition(int eachPosition)
		{
			this.eachPosition = eachPosition;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("for ");
			if (IsForEach())
			{
				sb.Append("each ");
			}
			sb.Append("(");
			sb.Append(iterator.ToSource(0));
			sb.Append(" in ");
			sb.Append(iteratedObject.ToSource(0));
			sb.Append(") ");
			if (body.GetType() == Token.BLOCK)
			{
				sb.Append(body.ToSource(depth).Trim()).Append("\n");
			}
			else
			{
				sb.Append("\n").Append(body.ToSource(depth + 1));
			}
			return sb.ToString();
		}

		/// <summary>Visits this node, the iterator, the iterated object, and the body.</summary>
		/// <remarks>Visits this node, the iterator, the iterated object, and the body.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				iterator.Visit(v);
				iteratedObject.Visit(v);
				body.Visit(v);
			}
		}
	}
}
