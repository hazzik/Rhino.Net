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
using Rhino.Utils;
using Sharpen;

namespace Rhino.Optimizer
{
	/// <summary>This class performs node transforms to prepare for optimization.</summary>
	/// <remarks>This class performs node transforms to prepare for optimization.</remarks>
	/// <seealso cref="Rhino.NodeTransformer">Rhino.NodeTransformer</seealso>
	/// <author>Norris Boyd</author>
	internal class OptTransformer : NodeTransformer
	{
		internal OptTransformer(IDictionary<string, OptFunctionNode> possibleDirectCalls, List<OptFunctionNode> directCallTargets)
		{
			this.possibleDirectCalls = possibleDirectCalls;
			this.directCallTargets = directCallTargets;
		}

		protected internal override void VisitNew(Node node, ScriptNode tree)
		{
			DetectDirectCall(node, tree);
			base.VisitNew(node, tree);
		}

		protected internal override void VisitCall(Node node, ScriptNode tree)
		{
			DetectDirectCall(node, tree);
			base.VisitCall(node, tree);
		}

		private void DetectDirectCall(Node node, ScriptNode tree)
		{
			if (tree.GetType() == Token.FUNCTION)
			{
				Node left = node.GetFirstChild();
				// count the arguments
				int argCount = 0;
				Node arg = left.GetNext();
				while (arg != null)
				{
					arg = arg.GetNext();
					argCount++;
				}
				if (argCount == 0)
				{
					OptFunctionNode.Get(tree).itsContainsCalls0 = true;
				}
				if (possibleDirectCalls != null)
				{
					string targetName = null;
					if (left.GetType() == Token.NAME)
					{
						targetName = left.GetString();
					}
					else
					{
						if (left.GetType() == Token.GETPROP)
						{
							targetName = left.GetFirstChild().GetNext().GetString();
						}
						else
						{
							if (left.GetType() == Token.GETPROPNOWARN)
							{
								throw Kit.CodeBug();
							}
						}
					}
					if (targetName != null)
					{
						OptFunctionNode ofn;
						ofn = possibleDirectCalls.GetValueOrDefault(targetName);
						if (ofn != null && argCount == ofn.fnode.GetParamCount() && !ofn.fnode.RequiresActivation())
						{
							// Refuse to directCall any function with more
							// than 32 parameters - prevent code explosion
							// for wacky test cases
							if (argCount <= 32)
							{
								node.PutProp(Node.DIRECTCALL_PROP, ofn);
								if (!ofn.IsTargetOfDirectCall())
								{
									int index = directCallTargets.Count;
									directCallTargets.Add(ofn);
									ofn.SetDirectTargetIndex(index);
								}
							}
						}
					}
				}
			}
		}

		private IDictionary<string, OptFunctionNode> possibleDirectCalls;

        private List<OptFunctionNode> directCallTargets;
	}
}
