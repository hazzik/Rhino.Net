/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	public class GeneratorExpressionLoop : ForInLoop
	{
		public GeneratorExpressionLoop()
		{
		}

		public GeneratorExpressionLoop(int pos) : base(pos)
		{
		}

		public GeneratorExpressionLoop(int pos, int len) : base(pos, len)
		{
		}

		/// <summary>Returns whether the loop is a for-each loop</summary>
		public override bool IsForEach()
		{
			return false;
		}

		/// <summary>Sets whether the loop is a for-each loop</summary>
		public override void SetIsForEach(bool isForEach)
		{
			throw new NotSupportedException("this node type does not support for each");
		}

		public override string ToSource(int depth)
		{
			return MakeIndent(depth) + " for " + (IsForEach() ? "each " : string.Empty) + "(" + iterator.ToSource(0) + " in " + iteratedObject.ToSource(0) + ")";
		}

		/// <summary>Visits the iterator expression and the iterated object expression.</summary>
		/// <remarks>
		/// Visits the iterator expression and the iterated object expression.
		/// There is no body-expression for this loop type.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				iterator.Visit(v);
				iteratedObject.Visit(v);
			}
		}
	}
}
