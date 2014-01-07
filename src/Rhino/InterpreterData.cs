/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Debug;
using Sharpen;

namespace Rhino
{
	[System.Serializable]
	internal sealed class InterpreterData : DebuggableScript
	{
		internal const long serialVersionUID = 5067677351589230234L;

		internal const int INITIAL_MAX_ICODE_LENGTH = 1024;

		internal const int INITIAL_STRINGTABLE_SIZE = 64;

		internal const int INITIAL_NUMBERTABLE_SIZE = 64;

		internal InterpreterData(LanguageVersion languageVersion, string sourceFile, string encodedSource, bool isStrict)
		{
			this.languageVersion = languageVersion;
			this.itsSourceFile = sourceFile;
			this.encodedSource = encodedSource;
			this.isStrict = isStrict;
			Init();
		}

		internal InterpreterData(Rhino.InterpreterData parent)
		{
			this.parentData = parent;
			this.languageVersion = parent.languageVersion;
			this.itsSourceFile = parent.itsSourceFile;
			this.encodedSource = parent.encodedSource;
			Init();
		}

		private void Init()
		{
			itsICode = new sbyte[INITIAL_MAX_ICODE_LENGTH];
			itsStringTable = new string[INITIAL_STRINGTABLE_SIZE];
		}

		internal string itsName;

		internal string itsSourceFile;

		internal bool itsNeedsActivation;

		internal int itsFunctionType;

		internal string[] itsStringTable;

		internal double[] itsDoubleTable;

		internal Rhino.InterpreterData[] itsNestedFunctions;

		internal object[] itsRegExpLiterals;

		internal sbyte[] itsICode;

		internal int[] itsExceptionTable;

		internal int itsMaxVars;

		internal int itsMaxLocals;

		internal int itsMaxStack;

		internal int itsMaxFrameArray;

		internal string[] argNames;

		internal bool[] argIsConst;

		internal int argCount;

		internal int itsMaxCalleeArgs;

		internal string encodedSource;

		internal int encodedSourceStart;

		internal int encodedSourceEnd;

		internal LanguageVersion languageVersion;

		internal bool isStrict;

		internal bool topLevel;

		internal object[] literalIds;

		internal UintMap longJumps;

		internal int firstLinePC = -1;

		internal Rhino.InterpreterData parentData;

		internal bool evalScriptFlag;

		// see comments in NativeFuncion for definition of argNames and argCount
		// PC for the first LINE icode
		// true if script corresponds to eval() code
		public bool IsTopLevel()
		{
			return topLevel;
		}

		public bool IsFunction()
		{
			return itsFunctionType != 0;
		}

		public string GetFunctionName()
		{
			return itsName;
		}

		public int GetParamCount()
		{
			return argCount;
		}

		public int GetParamAndVarCount()
		{
			return argNames.Length;
		}

		public string GetParamOrVarName(int index)
		{
			return argNames[index];
		}

		public bool GetParamOrVarConst(int index)
		{
			return argIsConst[index];
		}

		public string GetSourceName()
		{
			return itsSourceFile;
		}

		public bool IsGeneratedScript()
		{
			return ScriptRuntime.IsGeneratedScript(itsSourceFile);
		}

		public int[] GetLineNumbers()
		{
			return Interpreter.GetLineNumbers(this);
		}

		public int GetFunctionCount()
		{
			return (itsNestedFunctions == null) ? 0 : itsNestedFunctions.Length;
		}

		public DebuggableScript GetFunction(int index)
		{
			return itsNestedFunctions[index];
		}

		public DebuggableScript GetParent()
		{
			return parentData;
		}
	}
}
