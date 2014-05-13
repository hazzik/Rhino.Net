/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using Rhino;
using Rhino.Ast;
using Rhino.Optimizer;
using Sharpen;

namespace Rhino.Optimizer
{
	internal class Optimizer
	{
		internal const int NoType = 0;

		internal const int NumberType = 1;

		internal const int AnyType = 3;

		// It is assumed that (NumberType | AnyType) == AnyType
		internal virtual void Optimize(ScriptNode scriptOrFn)
		{
			//  run on one function at a time for now
			int functionCount = scriptOrFn.GetFunctionCount();
			for (int i = 0; i != functionCount; ++i)
			{
				OptFunctionNode f = OptFunctionNode.Get(scriptOrFn, i);
				OptimizeFunction(f);
			}
		}

		private void OptimizeFunction(OptFunctionNode theFunction)
		{
			if (theFunction.fnode.RequiresActivation())
			{
				return;
			}
			inDirectCallFunction = theFunction.IsTargetOfDirectCall();
			this.theFunction = theFunction;
			var statementsArray = new List<Node>();
			BuildStatementList_r(theFunction.fnode, statementsArray);
			Node[] theStatementNodes = statementsArray.ToArray();
			Block.RunFlowAnalyzes(theFunction, theStatementNodes);
			if (!theFunction.fnode.RequiresActivation())
			{
				parameterUsedInNumberContext = false;
				foreach (Node theStatementNode in theStatementNodes)
				{
					RewriteForNumberVariables(theStatementNode, NumberType);
				}
				theFunction.SetParameterNumberContext(parameterUsedInNumberContext);
			}
		}

		private void MarkDCPNumberContext(Node n)
		{
			if (inDirectCallFunction && n.GetType() == Token.GETVAR)
			{
				int varIndex = theFunction.GetVarIndex(n);
				if (theFunction.IsParameter(varIndex))
				{
					parameterUsedInNumberContext = true;
				}
			}
		}

		private bool ConvertParameter(Node n)
		{
			if (inDirectCallFunction && n.GetType() == Token.GETVAR)
			{
				int varIndex = theFunction.GetVarIndex(n);
				if (theFunction.IsParameter(varIndex))
				{
					n.RemoveProp(Node.ISNUMBER_PROP);
					return true;
				}
			}
			return false;
		}

		private int RewriteForNumberVariables(Node n, int desired)
		{
			switch (n.GetType())
			{
				case Token.EXPR_VOID:
				{
					Node child = n.GetFirstChild();
					int type = RewriteForNumberVariables(child, NumberType);
					if (type == NumberType)
					{
						n.PutIntProp(Node.ISNUMBER_PROP, Node.BOTH);
					}
					return NoType;
				}

				case Token.NUMBER:
				{
					n.PutIntProp(Node.ISNUMBER_PROP, Node.BOTH);
					return NumberType;
				}

				case Token.GETVAR:
				{
					int varIndex = theFunction.GetVarIndex(n);
					if (inDirectCallFunction && theFunction.IsParameter(varIndex) && desired == NumberType)
					{
						n.PutIntProp(Node.ISNUMBER_PROP, Node.BOTH);
						return NumberType;
					}
					else
					{
						if (theFunction.IsNumberVar(varIndex))
						{
							n.PutIntProp(Node.ISNUMBER_PROP, Node.BOTH);
							return NumberType;
						}
					}
					return NoType;
				}

				case Token.INC:
				case Token.DEC:
				{
					Node child = n.GetFirstChild();
					int type = RewriteForNumberVariables(child, NumberType);
					if (child.GetType() == Token.GETVAR)
					{
						if (type == NumberType && !ConvertParameter(child))
						{
							n.PutIntProp(Node.ISNUMBER_PROP, Node.BOTH);
							MarkDCPNumberContext(child);
							return NumberType;
						}
						return NoType;
					}
					else
					{
						if (child.GetType() == Token.GETELEM || child.GetType() == Token.GETPROP)
						{
							return type;
						}
					}
					return NoType;
				}

				case Token.SETVAR:
				{
					Node lChild = n.GetFirstChild();
					Node rChild = lChild.GetNext();
					int rType = RewriteForNumberVariables(rChild, NumberType);
					int varIndex = theFunction.GetVarIndex(n);
					if (inDirectCallFunction && theFunction.IsParameter(varIndex))
					{
						if (rType == NumberType)
						{
							if (!ConvertParameter(rChild))
							{
								n.PutIntProp(Node.ISNUMBER_PROP, Node.BOTH);
								return NumberType;
							}
							MarkDCPNumberContext(rChild);
							return NoType;
						}
						else
						{
							return rType;
						}
					}
					else
					{
						if (theFunction.IsNumberVar(varIndex))
						{
							if (rType != NumberType)
							{
								n.RemoveChild(rChild);
								n.AddChildToBack(new Node(Token.TO_DOUBLE, rChild));
							}
							n.PutIntProp(Node.ISNUMBER_PROP, Node.BOTH);
							MarkDCPNumberContext(rChild);
							return NumberType;
						}
						else
						{
							if (rType == NumberType)
							{
								if (!ConvertParameter(rChild))
								{
									n.RemoveChild(rChild);
									n.AddChildToBack(new Node(Token.TO_OBJECT, rChild));
								}
							}
							return NoType;
						}
					}
					goto case Token.LE;
				}

				case Token.LE:
				case Token.LT:
				case Token.GE:
				case Token.GT:
				{
					Node lChild = n.GetFirstChild();
					Node rChild = lChild.GetNext();
					int lType = RewriteForNumberVariables(lChild, NumberType);
					int rType = RewriteForNumberVariables(rChild, NumberType);
					MarkDCPNumberContext(lChild);
					MarkDCPNumberContext(rChild);
					if (ConvertParameter(lChild))
					{
						if (ConvertParameter(rChild))
						{
							return NoType;
						}
						else
						{
							if (rType == NumberType)
							{
								n.PutIntProp(Node.ISNUMBER_PROP, Node.RIGHT);
							}
						}
					}
					else
					{
						if (ConvertParameter(rChild))
						{
							if (lType == NumberType)
							{
								n.PutIntProp(Node.ISNUMBER_PROP, Node.LEFT);
							}
						}
						else
						{
							if (lType == NumberType)
							{
								if (rType == NumberType)
								{
									n.PutIntProp(Node.ISNUMBER_PROP, Node.BOTH);
								}
								else
								{
									n.PutIntProp(Node.ISNUMBER_PROP, Node.LEFT);
								}
							}
							else
							{
								if (rType == NumberType)
								{
									n.PutIntProp(Node.ISNUMBER_PROP, Node.RIGHT);
								}
							}
						}
					}
					// we actually build a boolean value
					return NoType;
				}

				case Token.ADD:
				{
					Node lChild = n.GetFirstChild();
					Node rChild = lChild.GetNext();
					int lType = RewriteForNumberVariables(lChild, NumberType);
					int rType = RewriteForNumberVariables(rChild, NumberType);
					if (ConvertParameter(lChild))
					{
						if (ConvertParameter(rChild))
						{
							return NoType;
						}
						else
						{
							if (rType == NumberType)
							{
								n.PutIntProp(Node.ISNUMBER_PROP, Node.RIGHT);
							}
						}
					}
					else
					{
						if (ConvertParameter(rChild))
						{
							if (lType == NumberType)
							{
								n.PutIntProp(Node.ISNUMBER_PROP, Node.LEFT);
							}
						}
						else
						{
							if (lType == NumberType)
							{
								if (rType == NumberType)
								{
									n.PutIntProp(Node.ISNUMBER_PROP, Node.BOTH);
									return NumberType;
								}
								else
								{
									n.PutIntProp(Node.ISNUMBER_PROP, Node.LEFT);
								}
							}
							else
							{
								if (rType == NumberType)
								{
									n.PutIntProp(Node.ISNUMBER_PROP, Node.RIGHT);
								}
							}
						}
					}
					return NoType;
				}

				case Token.BITXOR:
				case Token.BITOR:
				case Token.BITAND:
				case Token.RSH:
				case Token.LSH:
				case Token.SUB:
				case Token.MUL:
				case Token.DIV:
				case Token.MOD:
				{
					Node lChild = n.GetFirstChild();
					Node rChild = lChild.GetNext();
					int lType = RewriteForNumberVariables(lChild, NumberType);
					int rType = RewriteForNumberVariables(rChild, NumberType);
					MarkDCPNumberContext(lChild);
					MarkDCPNumberContext(rChild);
					if (lType == NumberType)
					{
						if (rType == NumberType)
						{
							n.PutIntProp(Node.ISNUMBER_PROP, Node.BOTH);
							return NumberType;
						}
						else
						{
							if (!ConvertParameter(rChild))
							{
								n.RemoveChild(rChild);
								n.AddChildToBack(new Node(Token.TO_DOUBLE, rChild));
								n.PutIntProp(Node.ISNUMBER_PROP, Node.BOTH);
							}
							return NumberType;
						}
					}
					else
					{
						if (rType == NumberType)
						{
							if (!ConvertParameter(lChild))
							{
								n.RemoveChild(lChild);
								n.AddChildToFront(new Node(Token.TO_DOUBLE, lChild));
								n.PutIntProp(Node.ISNUMBER_PROP, Node.BOTH);
							}
							return NumberType;
						}
						else
						{
							if (!ConvertParameter(lChild))
							{
								n.RemoveChild(lChild);
								n.AddChildToFront(new Node(Token.TO_DOUBLE, lChild));
							}
							if (!ConvertParameter(rChild))
							{
								n.RemoveChild(rChild);
								n.AddChildToBack(new Node(Token.TO_DOUBLE, rChild));
							}
							n.PutIntProp(Node.ISNUMBER_PROP, Node.BOTH);
							return NumberType;
						}
					}
					goto case Token.SETELEM;
				}

				case Token.SETELEM:
				case Token.SETELEM_OP:
				{
					Node arrayBase = n.GetFirstChild();
					Node arrayIndex = arrayBase.GetNext();
					Node rValue = arrayIndex.GetNext();
					int baseType = RewriteForNumberVariables(arrayBase, NumberType);
					if (baseType == NumberType)
					{
						if (!ConvertParameter(arrayBase))
						{
							n.RemoveChild(arrayBase);
							n.AddChildToFront(new Node(Token.TO_OBJECT, arrayBase));
						}
					}
					int indexType = RewriteForNumberVariables(arrayIndex, NumberType);
					if (indexType == NumberType)
					{
						if (!ConvertParameter(arrayIndex))
						{
							// setting the ISNUMBER_PROP signals the codegen
							// to use the OptRuntime.setObjectIndex that takes
							// a double index
							n.PutIntProp(Node.ISNUMBER_PROP, Node.LEFT);
						}
					}
					int rValueType = RewriteForNumberVariables(rValue, NumberType);
					if (rValueType == NumberType)
					{
						if (!ConvertParameter(rValue))
						{
							n.RemoveChild(rValue);
							n.AddChildToBack(new Node(Token.TO_OBJECT, rValue));
						}
					}
					return NoType;
				}

				case Token.GETELEM:
				{
					Node arrayBase = n.GetFirstChild();
					Node arrayIndex = arrayBase.GetNext();
					int baseType = RewriteForNumberVariables(arrayBase, NumberType);
					if (baseType == NumberType)
					{
						if (!ConvertParameter(arrayBase))
						{
							n.RemoveChild(arrayBase);
							n.AddChildToFront(new Node(Token.TO_OBJECT, arrayBase));
						}
					}
					int indexType = RewriteForNumberVariables(arrayIndex, NumberType);
					if (indexType == NumberType)
					{
						if (!ConvertParameter(arrayIndex))
						{
							// setting the ISNUMBER_PROP signals the codegen
							// to use the OptRuntime.getObjectIndex that takes
							// a double index
							n.PutIntProp(Node.ISNUMBER_PROP, Node.RIGHT);
						}
					}
					return NoType;
				}

				case Token.CALL:
				{
					Node child = n.GetFirstChild();
					// the function node
					// must be an object
					RewriteAsObjectChildren(child, child.GetFirstChild());
					child = child.GetNext();
					// the first arg
					OptFunctionNode target = (OptFunctionNode)n.GetProp(Node.DIRECTCALL_PROP);
					if (target != null)
					{
						while (child != null)
						{
							int type = RewriteForNumberVariables(child, NumberType);
							if (type == NumberType)
							{
								MarkDCPNumberContext(child);
							}
							child = child.GetNext();
						}
					}
					else
					{
						RewriteAsObjectChildren(n, child);
					}
					return NoType;
				}

				default:
				{
					RewriteAsObjectChildren(n, n.GetFirstChild());
					return NoType;
				}
			}
		}

		private void RewriteAsObjectChildren(Node n, Node child)
		{
			// Force optimized children to be objects
			while (child != null)
			{
				Node nextChild = child.GetNext();
				int type = RewriteForNumberVariables(child, NoType);
				if (type == NumberType)
				{
					if (!ConvertParameter(child))
					{
						n.RemoveChild(child);
						Node nuChild = new Node(Token.TO_OBJECT, child);
						if (nextChild == null)
						{
							n.AddChildToBack(nuChild);
						}
						else
						{
							n.AddChildBefore(nuChild, nextChild);
						}
					}
				}
				child = nextChild;
			}
		}

		private static void BuildStatementList_r(Node node, ICollection<Node> statements)
		{
			int type = node.GetType();
			if (type == Token.BLOCK || type == Token.LOCAL_BLOCK || type == Token.LOOP || type == Token.FUNCTION)
			{
				Node child = node.GetFirstChild();
				while (child != null)
				{
					BuildStatementList_r(child, statements);
					child = child.GetNext();
				}
			}
			else
			{
				statements.Add(node);
			}
		}

		private bool inDirectCallFunction;

		internal OptFunctionNode theFunction;

		private bool parameterUsedInNumberContext;
	}
}
