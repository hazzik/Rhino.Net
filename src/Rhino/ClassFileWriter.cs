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

	    public int GetLabelPC(Label label)
		{
			throw new NotImplementedException();
		}

	    public void AddExceptionHandler(Label startLabel, Label? endLabel, Label handlerLabel, Type exceptionType)
		{
			il.BeginCatchBlock(exceptionType);
		}
	}
}
#endif