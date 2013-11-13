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
	/// <summary>AST node for a function call.</summary>
	/// <remarks>
	/// AST node for a function call.  Node type is
	/// <see cref="Rhino.Token.CALL">Rhino.Token.CALL</see>
	/// .<p>
	/// </remarks>
	public class FunctionCall : AstNode
	{
		protected internal static readonly IList<AstNode> NO_ARGS = Sharpen.Collections.UnmodifiableList(new AList<AstNode>());

		protected internal AstNode target;

		protected internal IList<AstNode> arguments;

		protected internal int lp = -1;

		protected internal int rp = -1;

		public FunctionCall()
		{
			{
				type = Token.CALL;
			}
		}

		public FunctionCall(int pos) : base(pos)
		{
			{
				type = Token.CALL;
			}
		}

		public FunctionCall(int pos, int len) : base(pos, len)
		{
			{
				type = Token.CALL;
			}
		}

		/// <summary>Returns node evaluating to the function to call</summary>
		public virtual AstNode GetTarget()
		{
			return target;
		}

		/// <summary>
		/// Sets node evaluating to the function to call, and sets
		/// its parent to this node.
		/// </summary>
		/// <remarks>
		/// Sets node evaluating to the function to call, and sets
		/// its parent to this node.
		/// </remarks>
		/// <param name="target">node evaluating to the function to call.</param>
		/// <exception cref="System.ArgumentException">
		/// } if target is
		/// <code>null</code>
		/// </exception>
		public virtual void SetTarget(AstNode target)
		{
			AssertNotNull(target);
			this.target = target;
			target.SetParent(this);
		}

		/// <summary>Returns function argument list</summary>
		/// <returns>
		/// function argument list, or an empty immutable list if
		/// there are no arguments.
		/// </returns>
		public virtual IList<AstNode> GetArguments()
		{
			return arguments != null ? arguments : NO_ARGS;
		}

		/// <summary>Sets function argument list</summary>
		/// <param name="arguments">
		/// function argument list.  Can be
		/// <code>null</code>
		/// ,
		/// in which case any existing args are removed.
		/// </param>
		public virtual void SetArguments(IList<AstNode> arguments)
		{
			if (arguments == null)
			{
				this.arguments = null;
			}
			else
			{
				if (this.arguments != null)
				{
					this.arguments.Clear();
				}
				foreach (AstNode arg in arguments)
				{
					AddArgument(arg);
				}
			}
		}

		/// <summary>Adds an argument to the list, and sets its parent to this node.</summary>
		/// <remarks>Adds an argument to the list, and sets its parent to this node.</remarks>
		/// <param name="arg">the argument node to add to the list</param>
		/// <exception cref="System.ArgumentException">
		/// } if arg is
		/// <code>null</code>
		/// </exception>
		public virtual void AddArgument(AstNode arg)
		{
			AssertNotNull(arg);
			if (arguments == null)
			{
				arguments = new AList<AstNode>();
			}
			arguments.AddItem(arg);
			arg.SetParent(this);
		}

		/// <summary>Returns left paren position, -1 if missing</summary>
		public virtual int GetLp()
		{
			return lp;
		}

		/// <summary>Sets left paren position</summary>
		/// <param name="lp">left paren position</param>
		public virtual void SetLp(int lp)
		{
			this.lp = lp;
		}

		/// <summary>Returns right paren position, -1 if missing</summary>
		public virtual int GetRp()
		{
			return rp;
		}

		/// <summary>Sets right paren position</summary>
		public virtual void SetRp(int rp)
		{
			this.rp = rp;
		}

		/// <summary>Sets both paren positions</summary>
		public virtual void SetParens(int lp, int rp)
		{
			this.lp = lp;
			this.rp = rp;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append(target.ToSource(0));
			sb.Append("(");
			if (arguments != null)
			{
				PrintList(arguments, sb);
			}
			sb.Append(")");
			return sb.ToString();
		}

		/// <summary>Visits this node, the target object, and the arguments.</summary>
		/// <remarks>Visits this node, the target object, and the arguments.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				target.Visit(v);
				foreach (AstNode arg in GetArguments())
				{
					arg.Visit(v);
				}
			}
		}
	}
}
