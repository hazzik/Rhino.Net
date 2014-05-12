/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using Rhino.Ast;
using Rhino.Utils;
using Sharpen;

namespace Rhino.Optimizer
{
	internal class Block
	{
		private class FatBlock
		{
			private static Block[] ReduceToArray(ICollection<FatBlock> map)
			{
				Block[] result = null;
				if (!(map.Count <= 0))
				{
					result = new Block[map.Count];
					int i = 0;
				    foreach (var fb in map)
				    {
						result[i++] = fb.realBlock;
					}
				}
				return result;
			}

			internal virtual void AddSuccessor(Block.FatBlock b)
			{
			    successors.Add(b);
			}

		    internal virtual void AddPredecessor(Block.FatBlock b)
		    {
		        predecessors.Add(b);
		    }

		    internal virtual Block[] GetSuccessors()
			{
				return ReduceToArray(successors);
			}

			internal virtual Block[] GetPredecessors()
			{
				return ReduceToArray(predecessors);
			}

            private readonly HashSet<FatBlock> successors = new HashSet<FatBlock>();

            private readonly HashSet<FatBlock> predecessors = new HashSet<FatBlock>();

			internal Block realBlock;
			// all the Blocks that come immediately after this
			// all the Blocks that come immediately before this
		}

		internal Block(int startNodeIndex, int endNodeIndex)
		{
			itsStartNodeIndex = startNodeIndex;
			itsEndNodeIndex = endNodeIndex;
		}

		internal static void RunFlowAnalyzes(OptFunctionNode fn, Node[] statementNodes)
		{
			int paramCount = fn.fnode.GetParamCount();
			int varCount = fn.fnode.GetParamAndVarCount();
			int[] varTypes = new int[varCount];
			// If the variable is a parameter, it could have any type.
			for (int i = 0; i != paramCount; ++i)
			{
				varTypes[i] = Rhino.Optimizer.Optimizer.AnyType;
			}
			// If the variable is from a "var" statement, its typeEvent will be set
			// when we see the setVar node.
			for (int i_1 = paramCount; i_1 != varCount; ++i_1)
			{
				varTypes[i_1] = Rhino.Optimizer.Optimizer.NoType;
			}
			Block[] theBlocks = BuildBlocks(statementNodes);
			ReachingDefDataFlow(fn, statementNodes, theBlocks, varTypes);
			TypeFlow(fn, statementNodes, theBlocks, varTypes);
			for (int i_2 = paramCount; i_2 != varCount; i_2++)
			{
				if (varTypes[i_2] == Rhino.Optimizer.Optimizer.NumberType)
				{
					fn.SetIsNumberVar(i_2);
				}
			}
		}

		private static Block[] BuildBlocks(Node[] statementNodes)
		{
			// a mapping from each target node to the block it begins
			IDictionary<Node, Block.FatBlock> theTargetBlocks = new Dictionary<Node, Block.FatBlock>();
			var theBlocks = new List<FatBlock>();
			// there's a block that starts at index 0
			int beginNodeIndex = 0;
			for (int i = 0; i < statementNodes.Length; i++)
			{
				switch (statementNodes[i].GetType())
				{
					case Token.TARGET:
					{
						if (i != beginNodeIndex)
						{
							Block.FatBlock fb = NewFatBlock(beginNodeIndex, i - 1);
							if (statementNodes[beginNodeIndex].GetType() == Token.TARGET)
							{
								theTargetBlocks[statementNodes[beginNodeIndex]] = fb;
							}
							theBlocks.Add(fb);
							// start the next block at this node
							beginNodeIndex = i;
						}
						break;
					}

					case Token.IFNE:
					case Token.IFEQ:
					case Token.GOTO:
					{
						Block.FatBlock fb = NewFatBlock(beginNodeIndex, i);
						if (statementNodes[beginNodeIndex].GetType() == Token.TARGET)
						{
							theTargetBlocks[statementNodes[beginNodeIndex]] = fb;
						}
						theBlocks.Add(fb);
						// start the next block at the next node
						beginNodeIndex = i + 1;
						break;
					}
				}
			}
			if (beginNodeIndex != statementNodes.Length)
			{
				Block.FatBlock fb = NewFatBlock(beginNodeIndex, statementNodes.Length - 1);
				if (statementNodes[beginNodeIndex].GetType() == Token.TARGET)
				{
					theTargetBlocks[statementNodes[beginNodeIndex]] = fb;
				}
				theBlocks.Add(fb);
			}
			// build successor and predecessor links
			for (int i = 0; i < theBlocks.Count; i++)
			{
				Block.FatBlock fb = theBlocks[i];
				Node blockEndNode = statementNodes[fb.realBlock.itsEndNodeIndex];
				int blockEndNodeType = blockEndNode.GetType();
				if ((blockEndNodeType != Token.GOTO) && (i < (theBlocks.Count - 1)))
				{
					Block.FatBlock fallThruTarget = theBlocks[i + 1];
					fb.AddSuccessor(fallThruTarget);
					fallThruTarget.AddPredecessor(fb);
				}
				if ((blockEndNodeType == Token.IFNE) || (blockEndNodeType == Token.IFEQ) || (blockEndNodeType == Token.GOTO))
				{
					Node target = ((Jump)blockEndNode).target;
					Block.FatBlock branchTargetBlock = theTargetBlocks.GetValueOrDefault(target);
					target.PutProp(Node.TARGETBLOCK_PROP, branchTargetBlock.realBlock);
					fb.AddSuccessor(branchTargetBlock);
					branchTargetBlock.AddPredecessor(fb);
				}
			}
			Block[] result = new Block[theBlocks.Count];
			for (int i = 0; i < theBlocks.Count; i++)
			{
				Block.FatBlock fb = theBlocks[i];
				Block b = fb.realBlock;
				b.itsSuccessors = fb.GetSuccessors();
				b.itsPredecessors = fb.GetPredecessors();
				b.itsBlockID = i;
				result[i] = b;
			}
			return result;
		}

		private static Block.FatBlock NewFatBlock(int startNodeIndex, int endNodeIndex)
		{
			Block.FatBlock fb = new Block.FatBlock();
			fb.realBlock = new Block(startNodeIndex, endNodeIndex);
			return fb;
		}

		private static string ToString(Block[] blockList, Node[] statementNodes)
		{
			return null;
			StringWriter sw = new StringWriter();
			PrintWriter pw = new PrintWriter(sw);
			pw.WriteLine(blockList.Length + " Blocks");
			for (int i = 0; i < blockList.Length; i++)
			{
				Block b = blockList[i];
				pw.WriteLine("#" + b.itsBlockID);
				pw.WriteLine("from " + b.itsStartNodeIndex + " " + statementNodes[b.itsStartNodeIndex].ToString());
				pw.WriteLine("thru " + b.itsEndNodeIndex + " " + statementNodes[b.itsEndNodeIndex].ToString());
				pw.Write("Predecessors ");
				if (b.itsPredecessors != null)
				{
					for (int j = 0; j < b.itsPredecessors.Length; j++)
					{
						pw.Write(b.itsPredecessors[j].itsBlockID + " ");
					}
					pw.WriteLine();
				}
				else
				{
					pw.WriteLine("none");
				}
				pw.Write("Successors ");
				if (b.itsSuccessors != null)
				{
					for (int j = 0; j < b.itsSuccessors.Length; j++)
					{
						pw.Write(b.itsSuccessors[j].itsBlockID + " ");
					}
					pw.WriteLine();
				}
				else
				{
					pw.WriteLine("none");
				}
			}
			return sw.ToString();
		}

		private static void ReachingDefDataFlow(OptFunctionNode fn, Node[] statementNodes, Block[] theBlocks, int[] varTypes)
		{
			for (int i = 0; i < theBlocks.Length; i++)
			{
				theBlocks[i].InitLiveOnEntrySets(fn, statementNodes);
			}
			bool[] visit = new bool[theBlocks.Length];
			bool[] doneOnce = new bool[theBlocks.Length];
			int vIndex = theBlocks.Length - 1;
			bool needRescan = false;
			visit[vIndex] = true;
			while (true)
			{
				if (visit[vIndex] || !doneOnce[vIndex])
				{
					doneOnce[vIndex] = true;
					visit[vIndex] = false;
					if (theBlocks[vIndex].DoReachedUseDataFlow())
					{
						Block[] pred = theBlocks[vIndex].itsPredecessors;
						if (pred != null)
						{
							for (int i_1 = 0; i_1 < pred.Length; i_1++)
							{
								int index = pred[i_1].itsBlockID;
								visit[index] = true;
								needRescan |= (index > vIndex);
							}
						}
					}
				}
				if (vIndex == 0)
				{
					if (needRescan)
					{
						vIndex = theBlocks.Length - 1;
						needRescan = false;
					}
					else
					{
						break;
					}
				}
				else
				{
					vIndex--;
				}
			}
			theBlocks[0].MarkAnyTypeVariables(varTypes);
		}

		private static void TypeFlow(OptFunctionNode fn, Node[] statementNodes, Block[] theBlocks, int[] varTypes)
		{
			bool[] visit = new bool[theBlocks.Length];
			bool[] doneOnce = new bool[theBlocks.Length];
			int vIndex = 0;
			bool needRescan = false;
			visit[vIndex] = true;
			while (true)
			{
				if (visit[vIndex] || !doneOnce[vIndex])
				{
					doneOnce[vIndex] = true;
					visit[vIndex] = false;
					if (theBlocks[vIndex].DoTypeFlow(fn, statementNodes, varTypes))
					{
						Block[] succ = theBlocks[vIndex].itsSuccessors;
						if (succ != null)
						{
							for (int i = 0; i < succ.Length; i++)
							{
								int index = succ[i].itsBlockID;
								visit[index] = true;
								needRescan |= (index < vIndex);
							}
						}
					}
				}
				if (vIndex == (theBlocks.Length - 1))
				{
					if (needRescan)
					{
						vIndex = 0;
						needRescan = false;
					}
					else
					{
						break;
					}
				}
				else
				{
					vIndex++;
				}
			}
		}

		private static bool AssignType(int[] varTypes, int index, int type)
		{
			int prev = varTypes[index];
			return prev != (varTypes[index] |= type);
		}

		private void MarkAnyTypeVariables(int[] varTypes)
		{
			for (int i = 0; i != varTypes.Length; i++)
			{
				if (itsLiveOnEntrySet.Get(i))
				{
					AssignType(varTypes, i, Rhino.Optimizer.Optimizer.AnyType);
				}
			}
		}

		private void LookForVariableAccess(OptFunctionNode fn, Node n)
		{
			switch (n.GetType())
			{
				case Token.TYPEOFNAME:
				{
					// TYPEOFNAME may be used with undefined names, which is why
					// this is handled separately from GETVAR above.
					int varIndex = fn.fnode.GetIndexForNameNode(n);
					if (varIndex > -1 && !itsNotDefSet.Get(varIndex))
					{
						itsUseBeforeDefSet.Set(varIndex);
					}
					break;
				}

				case Token.DEC:
				case Token.INC:
				{
					Node child = n.GetFirstChild();
					if (child.GetType() == Token.GETVAR)
					{
						int varIndex = fn.GetVarIndex(child);
						if (!itsNotDefSet.Get(varIndex))
						{
							itsUseBeforeDefSet.Set(varIndex);
						}
						itsNotDefSet.Set(varIndex);
					}
					else
					{
						LookForVariableAccess(fn, child);
					}
					break;
				}

				case Token.SETVAR:
				{
					Node lhs = n.GetFirstChild();
					Node rhs = lhs.GetNext();
					LookForVariableAccess(fn, rhs);
					itsNotDefSet.Set(fn.GetVarIndex(n));
					break;
				}

				case Token.GETVAR:
				{
					int varIndex = fn.GetVarIndex(n);
					if (!itsNotDefSet.Get(varIndex))
					{
						itsUseBeforeDefSet.Set(varIndex);
					}
					break;
				}

				default:
				{
					Node child_1 = n.GetFirstChild();
					while (child_1 != null)
					{
						LookForVariableAccess(fn, child_1);
						child_1 = child_1.GetNext();
					}
					break;
				}
			}
		}

		private void InitLiveOnEntrySets(OptFunctionNode fn, Node[] statementNodes)
		{
			int listLength = fn.GetVarCount();
			itsUseBeforeDefSet = new BitArray(listLength);
			itsNotDefSet = new BitArray(listLength);
			itsLiveOnEntrySet = new BitArray(listLength);
			itsLiveOnExitSet = new BitArray(listLength);
			for (int i = itsStartNodeIndex; i <= itsEndNodeIndex; i++)
			{
				Node n = statementNodes[i];
				LookForVariableAccess(fn, n);
			}
			itsNotDefSet.Not();
		}

		// truth in advertising
		private bool DoReachedUseDataFlow()
		{
			itsLiveOnExitSet.SetAll(false);
			if (itsSuccessors != null)
			{
				for (int i = 0; i < itsSuccessors.Length; i++)
				{
					itsLiveOnExitSet.Or(itsSuccessors[i].itsLiveOnEntrySet);
				}
			}
			return UpdateEntrySet(itsLiveOnEntrySet, itsLiveOnExitSet, itsUseBeforeDefSet, itsNotDefSet);
		}

		private bool UpdateEntrySet(BitArray entrySet, BitArray exitSet, BitArray useBeforeDef, BitArray notDef)
		{
			int card = entrySet.Cardinality();
			entrySet.Or(exitSet);
			entrySet.And(notDef);
			entrySet.Or(useBeforeDef);
			return entrySet.Cardinality() != card;
		}

		private static int FindExpressionType(OptFunctionNode fn, Node n, int[] varTypes)
		{
			switch (n.GetType())
			{
				case Token.NUMBER:
				{
					return Rhino.Optimizer.Optimizer.NumberType;
				}

				case Token.CALL:
				case Token.NEW:
				case Token.REF_CALL:
				{
					return Rhino.Optimizer.Optimizer.AnyType;
				}

				case Token.GETELEM:
				case Token.GETPROP:
				case Token.NAME:
				case Token.THIS:
				{
					return Rhino.Optimizer.Optimizer.AnyType;
				}

				case Token.GETVAR:
				{
					return varTypes[fn.GetVarIndex(n)];
				}

				case Token.INC:
				case Token.DEC:
				case Token.MUL:
				case Token.DIV:
				case Token.MOD:
				case Token.BITOR:
				case Token.BITXOR:
				case Token.BITAND:
				case Token.BITNOT:
				case Token.LSH:
				case Token.RSH:
				case Token.URSH:
				case Token.SUB:
				case Token.POS:
				case Token.NEG:
				{
					return Rhino.Optimizer.Optimizer.NumberType;
				}

				case Token.VOID:
				{
					// NYI: undefined type
					return Rhino.Optimizer.Optimizer.AnyType;
				}

				case Token.FALSE:
				case Token.TRUE:
				case Token.EQ:
				case Token.NE:
				case Token.LT:
				case Token.LE:
				case Token.GT:
				case Token.GE:
				case Token.SHEQ:
				case Token.SHNE:
				case Token.NOT:
				case Token.INSTANCEOF:
				case Token.IN:
				case Token.DEL_REF:
				case Token.DELPROP:
				{
					// NYI: boolean type
					return Rhino.Optimizer.Optimizer.AnyType;
				}

				case Token.STRING:
				case Token.TYPEOF:
				case Token.TYPEOFNAME:
				{
					// NYI: string type
					return Rhino.Optimizer.Optimizer.AnyType;
				}

				case Token.NULL:
				case Token.REGEXP:
				case Token.ARRAYCOMP:
				case Token.ARRAYLIT:
				case Token.OBJECTLIT:
				{
					return Rhino.Optimizer.Optimizer.AnyType;
				}

				case Token.ADD:
				{
					// XXX: actually, we know it's not
					// number, but no type yet for that
					// if the lhs & rhs are known to be numbers, we can be sure that's
					// the result, otherwise it could be a string.
					Node child = n.GetFirstChild();
					int lType = FindExpressionType(fn, child, varTypes);
					int rType = FindExpressionType(fn, child.GetNext(), varTypes);
					return lType | rType;
				}

				case Token.HOOK:
				{
					// we're not distinguishing strings yet
					Node ifTrue = n.GetFirstChild().GetNext();
					Node ifFalse = ifTrue.GetNext();
					int ifTrueType = FindExpressionType(fn, ifTrue, varTypes);
					int ifFalseType = FindExpressionType(fn, ifFalse, varTypes);
					return ifTrueType | ifFalseType;
				}

				case Token.COMMA:
				case Token.SETVAR:
				case Token.SETNAME:
				case Token.SETPROP:
				case Token.SETELEM:
				{
					return FindExpressionType(fn, n.GetLastChild(), varTypes);
				}

				case Token.AND:
				case Token.OR:
				{
					Node child = n.GetFirstChild();
					int lType = FindExpressionType(fn, child, varTypes);
					int rType = FindExpressionType(fn, child.GetNext(), varTypes);
					return lType | rType;
				}
			}
			return Rhino.Optimizer.Optimizer.AnyType;
		}

		private static bool FindDefPoints(OptFunctionNode fn, Node n, int[] varTypes)
		{
			bool result = false;
			Node first = n.GetFirstChild();
			for (Node next = first; next != null; next = next.GetNext())
			{
				result |= FindDefPoints(fn, next, varTypes);
			}
			switch (n.GetType())
			{
				case Token.DEC:
				case Token.INC:
				{
					if (first.GetType() == Token.GETVAR)
					{
						// theVar is a Number now
						int i = fn.GetVarIndex(first);
						result |= AssignType(varTypes, i, Rhino.Optimizer.Optimizer.NumberType);
					}
					break;
				}

				case Token.SETVAR:
				{
					Node rValue = first.GetNext();
					int theType = FindExpressionType(fn, rValue, varTypes);
					int i = fn.GetVarIndex(n);
					result |= AssignType(varTypes, i, theType);
					break;
				}
			}
			return result;
		}

		private bool DoTypeFlow(OptFunctionNode fn, Node[] statementNodes, int[] varTypes)
		{
			bool changed = false;
			for (int i = itsStartNodeIndex; i <= itsEndNodeIndex; i++)
			{
				Node n = statementNodes[i];
				if (n != null)
				{
					changed |= FindDefPoints(fn, n, varTypes);
				}
			}
			return changed;
		}

		private void PrintLiveOnEntrySet(OptFunctionNode fn)
		{
		}

		private Block[] itsSuccessors;

		private Block[] itsPredecessors;

		private int itsStartNodeIndex;

		private int itsEndNodeIndex;

		private int itsBlockID;

		private BitArray itsLiveOnEntrySet;

		private BitArray itsLiveOnExitSet;

		private BitArray itsUseBeforeDefSet;

		private BitArray itsNotDefSet;

		internal const bool DEBUG = false;

		private static int debug_blockCount;
		// all the Blocks that come immediately after this
		// all the Blocks that come immediately before this
		// the Node at the start of the block
		// the Node at the end of the block
		// a unique index for each block
		// reaching def bit sets -
	}
}
