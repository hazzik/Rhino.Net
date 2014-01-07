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
		public static ClassFileWriter CreateClassFileWriter(TypeBuilder type, Type baseType, string source)
		{
			return new ClassFileWriter(type, baseType, source);
		}

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

		/// <summary>Construct a ClassFileWriter for a class.</summary>
		/// <remarks>Construct a ClassFileWriter for a class.</remarks>
		/// <param name="className">
		/// the name of the class to write, including
		/// full package qualification.
		/// </param>
		/// <param name="superClassName">
		/// the name of the superclass of the class
		/// to write, including full package qualification.
		/// </param>
		/// <param name="sourceFileName">
		/// the name of the source file to use for
		/// producing debug information, or null if debug information
		/// is not desired
		/// </param>
		private ClassFileWriter(TypeBuilder className, Type superClassName, string sourceFileName)
		{
			tb = className;
			if (sourceFileName != null)
			{
			}
			// All "new" implementations are supposed to output ACC_SUPER as a
			// class flag. This is specified in the first JVM spec, so it should
			// be old enough that it's okay to always set it.
		}

		public readonly TypeBuilder tb;

		public void Add(OpCode theOpCode, Type type, string fieldName, Type fieldType)
		{
			//TODO: declare FIELD?
			il.Emit(theOpCode, type.GetField(fieldName));
		}

		public void EmitLoadConstant(bool k)
		{
			il.EmitLoadConstant(k);
		}

		/// <summary>Store integer from stack top into the given local.</summary>
		/// <remarks>Store integer from stack top into the given local.</remarks>
		/// <param name="local">number of local register</param>
		public void EmitStloc(int local)
		{
			il.EmitStoreLocal(local);
		}
	
		public void EmitStloc(LocalBuilder local)
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

		public void MarkTableSwitchCase(int switchStart, int caseIndex, int stackTop)
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

		public void AdjustStackTop(int delta)
		{
			var newStack = itsStackTop + delta;
			if (newStack < 0 || short.MaxValue < newStack)
			{
				BadStack(newStack);
			}
			itsStackTop = (short)newStack;
			if (newStack > itsMaxStack)
			{
				itsMaxStack = (short)newStack;
			}
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

		private ObjArray itsVarDescriptors;

		private char[] tmpCharBuffer = new char[64];
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