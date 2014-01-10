/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Text;
using Sharpen;

namespace Rhino
{
	/// <summary>This class represents an element on the script execution stack.</summary>
	/// <remarks>This class represents an element on the script execution stack.</remarks>
	/// <seealso cref="RhinoException.GetScriptStack()">RhinoException.GetScriptStack()</seealso>
	/// <author>Hannes Wallnoefer</author>
	/// <since>1.7R3</since>
	[System.Serializable]
	public sealed class ScriptStackElement
	{
		public readonly string fileName;

		public readonly string functionName;

		public readonly int lineNumber;

		public ScriptStackElement(string fileName, string functionName, int lineNumber)
		{
			this.fileName = fileName;
			this.functionName = functionName;
			this.lineNumber = lineNumber;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			RenderMozillaStyle(sb);
			return sb.ToString();
		}

		/// <summary>
		/// Render stack element in Java-inspired style:
		/// <code>    at fileName:lineNumber (functionName)</code>
		/// </summary>
		/// <param name="sb">the StringBuilder to append to</param>
		public void RenderJavaStyle(StringBuilder sb)
		{
			sb.Append("\tat ").Append(fileName);
			if (lineNumber > -1)
			{
				sb.Append(':').Append(lineNumber);
			}
			if (functionName != null)
			{
				sb.Append(" (").Append(functionName).Append(')');
			}
		}

		/// <summary>
		/// Render stack element in Mozilla/Firefox style:
		/// <code>functionName()@fileName:lineNumber</code>
		/// </summary>
		/// <param name="sb">the StringBuilder to append to</param>
		public void RenderMozillaStyle(StringBuilder sb)
		{
			if (functionName != null)
			{
				sb.Append(functionName).Append("()");
			}
			sb.Append('@').Append(fileName);
			if (lineNumber > -1)
			{
				sb.Append(':').Append(lineNumber);
			}
		}
	}
}
