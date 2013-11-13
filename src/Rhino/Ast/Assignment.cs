/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>
	/// AST node representing the set of assignment operators such as
	/// <code>=</code>
	/// ,
	/// <code>*=</code>
	/// and
	/// <code>+=</code>
	/// .
	/// </summary>
	public class Assignment : InfixExpression
	{
		public Assignment()
		{
		}

		public Assignment(int pos) : base(pos)
		{
		}

		public Assignment(int pos, int len) : base(pos, len)
		{
		}

		public Assignment(int pos, int len, AstNode left, AstNode right) : base(pos, len, left, right)
		{
		}

		public Assignment(AstNode left, AstNode right) : base(left, right)
		{
		}

		public Assignment(int @operator, AstNode left, AstNode right, int operatorPos) : base(@operator, left, right, operatorPos)
		{
		}
	}
}
