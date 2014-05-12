/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

#if COMPILATION
using System;
using System.Reflection.Emit;
using Rhino;

namespace Org.Mozilla.Classfile
{
	/// <summary>
	/// ClassFileWriter
	/// A ClassFileWriter is used to write a Java class file.
	/// </summary>
	/// <remarks>
	/// ClassFileWriter
	/// A ClassFileWriter is used to write a Java class file. Methods are
	/// provided to create fields and methods, and within methods to write
	/// Java bytecodes.
	/// </remarks>
	/// <author>Roger Lawrence</author>
	public sealed class ClassFileWriter
	{
		public ILGenerator il;

		/// <summary>
		/// Thrown for cases where the error in generating the class file is
		/// due to a program size constraints rather than a likely bug in the
		/// compiler.
		/// </summary>
		/// <remarks>
		/// Thrown for cases where the error in generating the class file is
		/// due to a program size constraints rather than a likely bug in the
		/// compiler.
		/// </remarks>
		[Serializable]
		public class ClassFileFormatException : Exception
		{
			internal ClassFileFormatException(string message) : base(message)
			{
			}
		}

		/// <summary>Store integer from stack top into the given local.</summary>
		/// <remarks>Store integer from stack top into the given local.</remarks>
		/// <param name="local">number of local register</param>
		public void EmitStloc(int local)
		{
			il.EmitStoreLocal(local);
		}

		/// <summary>Load object from the given local into stack.</summary>
		/// <remarks>Load object from the given local into stack.</remarks>
		/// <param name="local">number of local register</param>
		public void EmitLdloc(int local)
		{
			il.EmitLoadLocal(local);
		}

		/// <summary>Load object from the given local into stack.</summary>
		/// <remarks>Load object from the given local into stack.</remarks>
		/// <param name="local">number of local register</param>
		public void EmitLdloc(LocalBuilder local)
		{
			il.EmitLoadLocal(local);
		}

		public int AddTableSwitch(int low, int high)
		{
			throw new NotImplementedException();
		}

		public void MarkTableSwitchDefault(int switchStart)
		{
			throw new NotImplementedException();
		}

		public void MarkTableSwitchCase(int switchStart, int caseIndex)
		{
			throw new NotImplementedException();
		}

		public void MarkHandler(Label theLabel)
		{
			itsStackTop = 1;
			il.MarkLabel(theLabel);
		}

		public int GetLabelPC(Label label)
		{
			throw new NotImplementedException();
		}

		public short GetStackTop()
		{
			return itsStackTop;
		}

		public void SetStackTop(short n)
		{
			itsStackTop = n;
		}

		public void AddExceptionHandler(Label startLabel, Label? endLabel, Label handlerLabel, Type exceptionType)
		{
			il.BeginCatchBlock(exceptionType);
		}

		private static void BadStack(int value)
		{
			string s;
			if (value < 0)
			{
				s = "Stack underflow: " + value;
			}
			else
			{
				s = "Too big stack: " + value;
			}
			throw new InvalidOperationException(s);
		}

		private int[] itsSuperBlockStarts;

		private int itsSuperBlockStartsTop;

		private UintMap itsJumpFroms;

		private static readonly bool GenerateStackMap;

		private ExceptionTableEntry[] itsExceptionTable;

		private int itsExceptionTableTop;

		private int[] itsLineNumberTable;

		private int itsLineNumberTableTop;

		private int itsCodeBufferTop;

		private short itsStackTop;

		private short itsMaxStack;

		private short itsMaxLocals;

		private int[] itsLabelTable;

		private int itsLabelTableTop;

		private long[] itsFixupTable;

		private int itsFixupTableTop;
	}

	internal sealed class ExceptionTableEntry
	{
		internal ExceptionTableEntry(int startLabel, int endLabel, int handlerLabel, short catchType)
		{
			itsStartLabel = startLabel;
			itsEndLabel = endLabel;
			itsHandlerLabel = handlerLabel;
			itsCatchType = catchType;
		}

		internal int itsStartLabel;

		internal int itsEndLabel;

		internal int itsHandlerLabel;

		internal short itsCatchType;
	}
}
#endif