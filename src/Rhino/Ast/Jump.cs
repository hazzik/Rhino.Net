/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>Used for code generation.</summary>
	/// <remarks>
	/// Used for code generation.  During codegen, the AST is transformed
	/// into an Intermediate Representation (IR) in which loops, ifs, switches
	/// and other control-flow statements are rewritten as labeled jumps.
	/// If the parser is set to IDE-mode, the resulting AST will not contain
	/// any instances of this class.
	/// </remarks>
	public class Jump : AstNode
	{
		public Node target;

		private Node target2;

		private Rhino.Ast.Jump jumpNode;

		public Jump()
		{
			type = Token.ERROR;
		}

		public Jump(int nodeType)
		{
			type = nodeType;
		}

		public Jump(int type, int lineno) : this(type)
		{
			SetLineno(lineno);
		}

		public Jump(int type, Node child) : this(type)
		{
			AddChildToBack(child);
		}

		public Jump(int type, Node child, int lineno) : this(type, child)
		{
			SetLineno(lineno);
		}

		public virtual Rhino.Ast.Jump GetJumpStatement()
		{
			if (type != Token.BREAK && type != Token.CONTINUE)
			{
				CodeBug();
			}
			return jumpNode;
		}

		public virtual void SetJumpStatement(Rhino.Ast.Jump jumpStatement)
		{
			if (type != Token.BREAK && type != Token.CONTINUE)
			{
				CodeBug();
			}
			if (jumpStatement == null)
			{
				CodeBug();
			}
			if (this.jumpNode != null)
			{
				CodeBug();
			}
			//only once
			this.jumpNode = jumpStatement;
		}

		public virtual Node GetDefault()
		{
			if (type != Token.SWITCH)
			{
				CodeBug();
			}
			return target2;
		}

		public virtual void SetDefault(Node defaultTarget)
		{
			if (type != Token.SWITCH)
			{
				CodeBug();
			}
			if (defaultTarget.GetType() != Token.TARGET)
			{
				CodeBug();
			}
			if (target2 != null)
			{
				CodeBug();
			}
			//only once
			target2 = defaultTarget;
		}

		public virtual Node GetFinally()
		{
			if (type != Token.TRY)
			{
				CodeBug();
			}
			return target2;
		}

		public virtual void SetFinally(Node finallyTarget)
		{
			if (type != Token.TRY)
			{
				CodeBug();
			}
			if (finallyTarget.GetType() != Token.TARGET)
			{
				CodeBug();
			}
			if (target2 != null)
			{
				CodeBug();
			}
			//only once
			target2 = finallyTarget;
		}

		public virtual Rhino.Ast.Jump GetLoop()
		{
			if (type != Token.LABEL)
			{
				CodeBug();
			}
			return jumpNode;
		}

		public virtual void SetLoop(Rhino.Ast.Jump loop)
		{
			if (type != Token.LABEL)
			{
				CodeBug();
			}
			if (loop == null)
			{
				CodeBug();
			}
			if (jumpNode != null)
			{
				CodeBug();
			}
			//only once
			jumpNode = loop;
		}

		public virtual Node GetContinue()
		{
			if (type != Token.LOOP)
			{
				CodeBug();
			}
			return target2;
		}

		public virtual void SetContinue(Node continueTarget)
		{
			if (type != Token.LOOP)
			{
				CodeBug();
			}
			if (continueTarget.GetType() != Token.TARGET)
			{
				CodeBug();
			}
			if (target2 != null)
			{
				CodeBug();
			}
			//only once
			target2 = continueTarget;
		}

		/// <summary>
		/// Jumps are only used directly during code generation, and do
		/// not support this interface.
		/// </summary>
		/// <remarks>
		/// Jumps are only used directly during code generation, and do
		/// not support this interface.
		/// </remarks>
		/// <exception cref="System.NotSupportedException">System.NotSupportedException</exception>
		public override void Visit(NodeVisitor visitor)
		{
			throw new NotSupportedException(this.ToString());
		}

		public override string ToSource(int depth)
		{
			throw new NotSupportedException(this.ToString());
		}
	}
}
