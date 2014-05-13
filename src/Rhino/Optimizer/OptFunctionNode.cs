/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Ast;

namespace Rhino.Optimizer
{
	public sealed class OptFunctionNode
	{
		internal OptFunctionNode(FunctionNode fnode)
		{
			this.fnode = fnode;
			fnode.SetCompilerData(this);
		}

		public static Rhino.Optimizer.OptFunctionNode Get(ScriptNode scriptOrFn, int i)
		{
			FunctionNode fnode = scriptOrFn.GetFunctionNode(i);
			return (Rhino.Optimizer.OptFunctionNode)fnode.GetCompilerData();
		}

		public static Rhino.Optimizer.OptFunctionNode Get(ScriptNode scriptOrFn)
		{
			return (Rhino.Optimizer.OptFunctionNode)scriptOrFn.GetCompilerData();
		}

		public bool IsTargetOfDirectCall()
		{
			return directTargetIndex >= 0;
		}

		public int GetDirectTargetIndex()
		{
			return directTargetIndex;
		}

		internal void SetDirectTargetIndex(int directTargetIndex)
		{
			// One time action
			if (directTargetIndex < 0 || this.directTargetIndex >= 0)
			{
				Kit.CodeBug();
			}
			this.directTargetIndex = directTargetIndex;
		}

		internal void SetParameterNumberContext(bool b)
		{
			itsParameterNumberContext = b;
		}

		public bool GetParameterNumberContext()
		{
			return itsParameterNumberContext;
		}

		public int GetVarCount()
		{
			return fnode.GetParamAndVarCount();
		}

		public bool IsParameter(int varIndex)
		{
			return varIndex < fnode.GetParamCount();
		}

		public bool IsNumberVar(int varIndex)
		{
			varIndex -= fnode.GetParamCount();
			if (varIndex >= 0 && numberVarFlags != null)
			{
				return numberVarFlags[varIndex];
			}
			return false;
		}

		internal void SetIsNumberVar(int varIndex)
		{
			varIndex -= fnode.GetParamCount();
			// Can only be used with non-parameters
			if (varIndex < 0)
			{
				Kit.CodeBug();
			}
			if (numberVarFlags == null)
			{
				int size = fnode.GetParamAndVarCount() - fnode.GetParamCount();
				numberVarFlags = new bool[size];
			}
			numberVarFlags[varIndex] = true;
		}

		public int GetVarIndex(Node n)
		{
			int index = n.GetIntProp(Node.VARIABLE_PROP, -1);
			if (index == -1)
			{
				Node node;
				int type = n.GetType();
				if (type == Token.GETVAR)
				{
					node = n;
				}
				else
				{
					if (type == Token.SETVAR || type == Token.SETCONSTVAR)
					{
						node = n.GetFirstChild();
					}
					else
					{
						throw Kit.CodeBug();
					}
				}
				index = fnode.GetIndexForNameNode(node);
				if (index < 0)
				{
					throw Kit.CodeBug();
				}
				n.PutIntProp(Node.VARIABLE_PROP, index);
			}
			return index;
		}

		public readonly FunctionNode fnode;

		private bool[] numberVarFlags;

		private int directTargetIndex = -1;

		private bool itsParameterNumberContext;

		internal bool itsContainsCalls0;

		internal bool itsContainsCalls1;
	}
}
