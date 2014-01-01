/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using System.Text;
using Org.Mozilla.Classfile;
using Rhino;
using Sharpen;

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
	public class ClassFileWriter
	{
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
		[System.Serializable]
		public class ClassFileFormatException : Exception
		{
			private const long serialVersionUID = 1263998431033790599L;

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
		public ClassFileWriter(string className, string superClassName, string sourceFileName)
		{
			generatedClassName = className;
			itsConstantPool = new ConstantPool(this);
			itsThisClassIndex = itsConstantPool.AddClass(className);
			itsSuperClassIndex = itsConstantPool.AddClass(superClassName);
			if (sourceFileName != null)
			{
				itsSourceFileNameIndex = itsConstantPool.AddUtf8(sourceFileName);
			}
			// All "new" implementations are supposed to output ACC_SUPER as a
			// class flag. This is specified in the first JVM spec, so it should
			// be old enough that it's okay to always set it.
			itsFlags = ACC_PUBLIC | ACC_SUPER;
		}

		public string GetClassName()
		{
			return generatedClassName;
		}

		/// <summary>Add an interface implemented by this class.</summary>
		/// <remarks>
		/// Add an interface implemented by this class.
		/// This method may be called multiple times for classes that
		/// implement multiple interfaces.
		/// </remarks>
		/// <param name="interfaceName">
		/// a name of an interface implemented
		/// by the class being written, including full package
		/// qualification.
		/// </param>
		public virtual void AddInterface(string interfaceName)
		{
			short interfaceIndex = itsConstantPool.AddClass(interfaceName);
			itsInterfaces.Add(interfaceIndex);
		}

		public const short ACC_PUBLIC = unchecked((int)(0x0001));

		public const short ACC_PRIVATE = unchecked((int)(0x0002));

		public const short ACC_PROTECTED = unchecked((int)(0x0004));

		public const short ACC_STATIC = unchecked((int)(0x0008));

		public const short ACC_FINAL = unchecked((int)(0x0010));

		public const short ACC_SUPER = unchecked((int)(0x0020));

		public const short ACC_SYNCHRONIZED = unchecked((int)(0x0020));

		public const short ACC_VOLATILE = unchecked((int)(0x0040));

		public const short ACC_TRANSIENT = unchecked((int)(0x0080));

		public const short ACC_NATIVE = unchecked((int)(0x0100));

		public const short ACC_ABSTRACT = unchecked((int)(0x0400));

		/// <summary>Set the class's flags.</summary>
		/// <remarks>
		/// Set the class's flags.
		/// Flags must be a set of the following flags, bitwise or'd
		/// together:
		/// ACC_PUBLIC
		/// ACC_PRIVATE
		/// ACC_PROTECTED
		/// ACC_FINAL
		/// ACC_ABSTRACT
		/// TODO: check that this is the appropriate set
		/// </remarks>
		/// <param name="flags">the set of class flags to set</param>
		public virtual void SetFlags(short flags)
		{
			itsFlags = flags;
		}

		internal static string GetSlashedForm(string name)
		{
			return name.Replace('.', '/');
		}

		/// <summary>
		/// Convert Java class name in dot notation into
		/// "Lname-with-dots-replaced-by-slashes;" form suitable for use as
		/// JVM type signatures.
		/// </summary>
		/// <remarks>
		/// Convert Java class name in dot notation into
		/// "Lname-with-dots-replaced-by-slashes;" form suitable for use as
		/// JVM type signatures.
		/// </remarks>
		public static string ClassNameToSignature(string name)
		{
			int nameLength = name.Length;
			int colonPos = 1 + nameLength;
			char[] buf = new char[colonPos + 1];
			buf[0] = 'L';
			buf[colonPos] = ';';
			Sharpen.Runtime.GetCharsForString(name, 0, nameLength, buf, 1);
			for (int i = 1; i != colonPos; ++i)
			{
				if (buf[i] == '.')
				{
					buf[i] = '/';
				}
			}
			return new string(buf, 0, colonPos + 1);
		}

		/// <summary>Add a field to the class.</summary>
		/// <remarks>Add a field to the class.</remarks>
		/// <param name="fieldName">the name of the field</param>
		/// <param name="type">the type of the field using ...</param>
		/// <param name="flags">
		/// the attributes of the field, such as ACC_PUBLIC, etc.
		/// bitwise or'd together
		/// </param>
		public virtual void AddField(string fieldName, string type, short flags)
		{
			short fieldNameIndex = itsConstantPool.AddUtf8(fieldName);
			short typeIndex = itsConstantPool.AddUtf8(type);
			itsFields.Add(new ClassFileField(fieldNameIndex, typeIndex, flags));
		}

		/// <summary>Add a field to the class.</summary>
		/// <remarks>Add a field to the class.</remarks>
		/// <param name="fieldName">the name of the field</param>
		/// <param name="type">the type of the field using ...</param>
		/// <param name="flags">
		/// the attributes of the field, such as ACC_PUBLIC, etc.
		/// bitwise or'd together
		/// </param>
		/// <param name="value">an initial integral value</param>
		public virtual void AddField(string fieldName, string type, short flags, int value)
		{
			short fieldNameIndex = itsConstantPool.AddUtf8(fieldName);
			short typeIndex = itsConstantPool.AddUtf8(type);
			ClassFileField field = new ClassFileField(fieldNameIndex, typeIndex, flags);
			field.SetAttributes(itsConstantPool.AddUtf8("ConstantValue"), (short)0, (short)0, itsConstantPool.AddConstant(value));
			itsFields.Add(field);
		}

		/// <summary>Add a field to the class.</summary>
		/// <remarks>Add a field to the class.</remarks>
		/// <param name="fieldName">the name of the field</param>
		/// <param name="type">the type of the field using ...</param>
		/// <param name="flags">
		/// the attributes of the field, such as ACC_PUBLIC, etc.
		/// bitwise or'd together
		/// </param>
		/// <param name="value">an initial long value</param>
		public virtual void AddField(string fieldName, string type, short flags, long value)
		{
			short fieldNameIndex = itsConstantPool.AddUtf8(fieldName);
			short typeIndex = itsConstantPool.AddUtf8(type);
			ClassFileField field = new ClassFileField(fieldNameIndex, typeIndex, flags);
			field.SetAttributes(itsConstantPool.AddUtf8("ConstantValue"), (short)0, (short)2, itsConstantPool.AddConstant(value));
			itsFields.Add(field);
		}

		/// <summary>Add a field to the class.</summary>
		/// <remarks>Add a field to the class.</remarks>
		/// <param name="fieldName">the name of the field</param>
		/// <param name="type">the type of the field using ...</param>
		/// <param name="flags">
		/// the attributes of the field, such as ACC_PUBLIC, etc.
		/// bitwise or'd together
		/// </param>
		/// <param name="value">an initial double value</param>
		public virtual void AddField(string fieldName, string type, short flags, double value)
		{
			short fieldNameIndex = itsConstantPool.AddUtf8(fieldName);
			short typeIndex = itsConstantPool.AddUtf8(type);
			ClassFileField field = new ClassFileField(fieldNameIndex, typeIndex, flags);
			field.SetAttributes(itsConstantPool.AddUtf8("ConstantValue"), (short)0, (short)2, itsConstantPool.AddConstant(value));
			itsFields.Add(field);
		}

		/// <summary>
		/// Add Information about java variable to use when generating the local
		/// variable table.
		/// </summary>
		/// <remarks>
		/// Add Information about java variable to use when generating the local
		/// variable table.
		/// </remarks>
		/// <param name="name">variable name.</param>
		/// <param name="type">variable type as bytecode descriptor string.</param>
		/// <param name="startPC">
		/// the starting bytecode PC where this variable is live,
		/// or -1 if it does not have a Java register.
		/// </param>
		/// <param name="register">
		/// the Java register number of variable
		/// or -1 if it does not have a Java register.
		/// </param>
		public virtual void AddVariableDescriptor(string name, string type, int startPC, int register)
		{
			int nameIndex = itsConstantPool.AddUtf8(name);
			int descriptorIndex = itsConstantPool.AddUtf8(type);
			int[] chunk = new int[] { nameIndex, descriptorIndex, startPC, register };
			if (itsVarDescriptors == null)
			{
				itsVarDescriptors = new ObjArray();
			}
			itsVarDescriptors.Add(chunk);
		}

		/// <summary>Add a method and begin adding code.</summary>
		/// <remarks>
		/// Add a method and begin adding code.
		/// This method must be called before other methods for adding code,
		/// exception tables, etc. can be invoked.
		/// </remarks>
		/// <param name="methodName">the name of the method</param>
		/// <param name="type">a string representing the type</param>
		/// <param name="flags">
		/// the attributes of the field, such as ACC_PUBLIC, etc.
		/// bitwise or'd together
		/// </param>
		public virtual void StartMethod(string methodName, string type, short flags)
		{
			short methodNameIndex = itsConstantPool.AddUtf8(methodName);
			short typeIndex = itsConstantPool.AddUtf8(type);
			itsCurrentMethod = new ClassFileMethod(methodName, methodNameIndex, type, typeIndex, flags);
			itsJumpFroms = new UintMap();
			itsMethods.Add(itsCurrentMethod);
			AddSuperBlockStart(0);
		}

		/// <summary>Complete generation of the method.</summary>
		/// <remarks>
		/// Complete generation of the method.
		/// After this method is called, no more code can be added to the
		/// method begun with <code>startMethod</code>.
		/// </remarks>
		/// <param name="maxLocals">
		/// the maximum number of local variable slots
		/// (a.k.a. Java registers) used by the method
		/// </param>
		public virtual void StopMethod(short maxLocals)
		{
			if (itsCurrentMethod == null)
			{
				throw new InvalidOperationException("No method to stop");
			}
			FixLabelGotos();
			itsMaxLocals = maxLocals;
			ClassFileWriter.StackMapTable stackMap = null;
			if (GenerateStackMap)
			{
				FinalizeSuperBlockStarts();
				stackMap = new ClassFileWriter.StackMapTable(this);
				stackMap.Generate();
			}
			int lineNumberTableLength = 0;
			if (itsLineNumberTable != null)
			{
				// 6 bytes for the attribute header
				// 2 bytes for the line number count
				// 4 bytes for each entry
				lineNumberTableLength = 6 + 2 + (itsLineNumberTableTop * 4);
			}
			int variableTableLength = 0;
			if (itsVarDescriptors != null)
			{
				// 6 bytes for the attribute header
				// 2 bytes for the variable count
				// 10 bytes for each entry
				variableTableLength = 6 + 2 + (itsVarDescriptors.Size() * 10);
			}
			int stackMapTableLength = 0;
			if (stackMap != null)
			{
				int stackMapWriteSize = stackMap.ComputeWriteSize();
				if (stackMapWriteSize > 0)
				{
					stackMapTableLength = 6 + stackMapWriteSize;
				}
			}
			int attrLength = 2 + 4 + 2 + 2 + 4 + itsCodeBufferTop + 2 + (itsExceptionTableTop * 8) + 2 + lineNumberTableLength + variableTableLength + stackMapTableLength;
			// attribute_name_index
			// attribute_length
			// max_stack
			// max_locals
			// code_length
			// exception_table_length
			// attributes_count
			if (attrLength > 65536)
			{
				// See http://java.sun.com/docs/books/jvms/second_edition/html/ClassFile.doc.html,
				// section 4.10, "The amount of code per non-native, non-abstract
				// method is limited to 65536 bytes...
				throw new ClassFileWriter.ClassFileFormatException("generated bytecode for method exceeds 64K limit.");
			}
			byte[] codeAttribute = new byte[attrLength];
			int index = 0;
			int codeAttrIndex = itsConstantPool.AddUtf8("Code");
			index = PutInt16(codeAttrIndex, codeAttribute, index);
			attrLength -= 6;
			// discount the attribute header
			index = PutInt32(attrLength, codeAttribute, index);
			index = PutInt16(itsMaxStack, codeAttribute, index);
			index = PutInt16(itsMaxLocals, codeAttribute, index);
			index = PutInt32(itsCodeBufferTop, codeAttribute, index);
			System.Array.Copy(itsCodeBuffer, 0, codeAttribute, index, itsCodeBufferTop);
			index += itsCodeBufferTop;
			if (itsExceptionTableTop > 0)
			{
				index = PutInt16(itsExceptionTableTop, codeAttribute, index);
				for (int i = 0; i < itsExceptionTableTop; i++)
				{
					ExceptionTableEntry ete = itsExceptionTable[i];
					short startPC = (short)GetLabelPC(ete.itsStartLabel);
					short endPC = (short)GetLabelPC(ete.itsEndLabel);
					short handlerPC = (short)GetLabelPC(ete.itsHandlerLabel);
					short catchType = ete.itsCatchType;
					if (startPC == -1)
					{
						throw new InvalidOperationException("start label not defined");
					}
					if (endPC == -1)
					{
						throw new InvalidOperationException("end label not defined");
					}
					if (handlerPC == -1)
					{
						throw new InvalidOperationException("handler label not defined");
					}
					index = PutInt16(startPC, codeAttribute, index);
					index = PutInt16(endPC, codeAttribute, index);
					index = PutInt16(handlerPC, codeAttribute, index);
					index = PutInt16(catchType, codeAttribute, index);
				}
			}
			else
			{
				// write 0 as exception table length
				index = PutInt16(0, codeAttribute, index);
			}
			int attributeCount = 0;
			if (itsLineNumberTable != null)
			{
				attributeCount++;
			}
			if (itsVarDescriptors != null)
			{
				attributeCount++;
			}
			if (stackMapTableLength > 0)
			{
				attributeCount++;
			}
			index = PutInt16(attributeCount, codeAttribute, index);
			if (itsLineNumberTable != null)
			{
				int lineNumberTableAttrIndex = itsConstantPool.AddUtf8("LineNumberTable");
				index = PutInt16(lineNumberTableAttrIndex, codeAttribute, index);
				int tableAttrLength = 2 + (itsLineNumberTableTop * 4);
				index = PutInt32(tableAttrLength, codeAttribute, index);
				index = PutInt16(itsLineNumberTableTop, codeAttribute, index);
				for (int i = 0; i < itsLineNumberTableTop; i++)
				{
					index = PutInt32(itsLineNumberTable[i], codeAttribute, index);
				}
			}
			if (itsVarDescriptors != null)
			{
				int variableTableAttrIndex = itsConstantPool.AddUtf8("LocalVariableTable");
				index = PutInt16(variableTableAttrIndex, codeAttribute, index);
				int varCount = itsVarDescriptors.Size();
				int tableAttrLength = 2 + (varCount * 10);
				index = PutInt32(tableAttrLength, codeAttribute, index);
				index = PutInt16(varCount, codeAttribute, index);
				for (int i = 0; i < varCount; i++)
				{
					int[] chunk = (int[])itsVarDescriptors.Get(i);
					int nameIndex = chunk[0];
					int descriptorIndex = chunk[1];
					int startPC = chunk[2];
					int register = chunk[3];
					int length = itsCodeBufferTop - startPC;
					index = PutInt16(startPC, codeAttribute, index);
					index = PutInt16(length, codeAttribute, index);
					index = PutInt16(nameIndex, codeAttribute, index);
					index = PutInt16(descriptorIndex, codeAttribute, index);
					index = PutInt16(register, codeAttribute, index);
				}
			}
			if (stackMapTableLength > 0)
			{
				int stackMapTableAttrIndex = itsConstantPool.AddUtf8("StackMapTable");
				int start = index;
				index = PutInt16(stackMapTableAttrIndex, codeAttribute, index);
				index = stackMap.Write(codeAttribute, index);
			}
			itsCurrentMethod.SetCodeAttribute(codeAttribute);
			itsExceptionTable = null;
			itsExceptionTableTop = 0;
			itsLineNumberTableTop = 0;
			itsCodeBufferTop = 0;
			itsCurrentMethod = null;
			itsMaxStack = 0;
			itsStackTop = 0;
			itsLabelTableTop = 0;
			itsFixupTableTop = 0;
			itsVarDescriptors = null;
			itsSuperBlockStarts = null;
			itsSuperBlockStartsTop = 0;
			itsJumpFroms = null;
		}

		/// <summary>Add the single-byte opcode to the current method.</summary>
		/// <remarks>Add the single-byte opcode to the current method.</remarks>
		/// <param name="theOpCode">the opcode of the bytecode</param>
		public virtual void Add(int theOpCode)
		{
			if (OpcodeCount(theOpCode) != 0)
			{
				throw new ArgumentException("Unexpected operands");
			}
			int newStack = itsStackTop + StackChange(theOpCode);
			if (newStack < 0 || short.MaxValue < newStack)
			{
				BadStack(newStack);
			}
			AddToCodeBuffer(theOpCode);
			itsStackTop = (short)newStack;
			if (newStack > itsMaxStack)
			{
				itsMaxStack = (short)newStack;
			}
			if (theOpCode == ByteCode.ATHROW)
			{
				AddSuperBlockStart(itsCodeBufferTop);
			}
		}

		/// <summary>Add a single-operand opcode to the current method.</summary>
		/// <remarks>Add a single-operand opcode to the current method.</remarks>
		/// <param name="theOpCode">the opcode of the bytecode</param>
		/// <param name="theOperand">the operand of the bytecode</param>
		public virtual void Add(int theOpCode, int theOperand)
		{
			int newStack = itsStackTop + StackChange(theOpCode);
			if (newStack < 0 || short.MaxValue < newStack)
			{
				BadStack(newStack);
			}
			switch (theOpCode)
			{
				case ByteCode.GOTO:
				{
					// This is necessary because dead code is seemingly being
					// generated and Sun's verifier is expecting type state to be
					// placed even at dead blocks of code.
					AddSuperBlockStart(itsCodeBufferTop + 3);
					goto case ByteCode.IFEQ;
				}

				case ByteCode.IFEQ:
				case ByteCode.IFNE:
				case ByteCode.IFLT:
				case ByteCode.IFGE:
				case ByteCode.IFGT:
				case ByteCode.IFLE:
				case ByteCode.IF_ICMPEQ:
				case ByteCode.IF_ICMPNE:
				case ByteCode.IF_ICMPLT:
				case ByteCode.IF_ICMPGE:
				case ByteCode.IF_ICMPGT:
				case ByteCode.IF_ICMPLE:
				case ByteCode.IF_ACMPEQ:
				case ByteCode.IF_ACMPNE:
				case ByteCode.JSR:
				case ByteCode.IFNULL:
				case ByteCode.IFNONNULL:
				{
					// fallthru...
					if ((theOperand & unchecked((int)(0x80000000))) != unchecked((int)(0x80000000)))
					{
						if ((theOperand < 0) || (theOperand > 65535))
						{
							throw new ArgumentException("Bad label for branch");
						}
					}
					int branchPC = itsCodeBufferTop;
					AddToCodeBuffer(theOpCode);
					if ((theOperand & unchecked((int)(0x80000000))) != unchecked((int)(0x80000000)))
					{
						// hard displacement
						AddToCodeInt16(theOperand);
						int target = theOperand + branchPC;
						AddSuperBlockStart(target);
						itsJumpFroms.Put(target, branchPC);
					}
					else
					{
						// a label
						int targetPC = GetLabelPC(theOperand);
						if (targetPC != -1)
						{
							int offset = targetPC - branchPC;
							AddToCodeInt16(offset);
							AddSuperBlockStart(targetPC);
							itsJumpFroms.Put(targetPC, branchPC);
						}
						else
						{
							AddLabelFixup(theOperand, branchPC + 1);
							AddToCodeInt16(0);
						}
					}
					break;
				}

				case ByteCode.BIPUSH:
				{
					if (unchecked((byte)theOperand) != theOperand)
					{
						throw new ArgumentException("out of range byte");
					}
					AddToCodeBuffer(theOpCode);
					AddToCodeBuffer(unchecked((byte)theOperand));
					break;
				}

				case ByteCode.SIPUSH:
				{
					if ((short)theOperand != theOperand)
					{
						throw new ArgumentException("out of range short");
					}
					AddToCodeBuffer(theOpCode);
					AddToCodeInt16(theOperand);
					break;
				}

				case ByteCode.NEWARRAY:
				{
					if (!(0 <= theOperand && theOperand < 256))
					{
						throw new ArgumentException("out of range index");
					}
					AddToCodeBuffer(theOpCode);
					AddToCodeBuffer(theOperand);
					break;
				}

				case ByteCode.GETFIELD:
				case ByteCode.PUTFIELD:
				{
					if (!(0 <= theOperand && theOperand < 65536))
					{
						throw new ArgumentException("out of range field");
					}
					AddToCodeBuffer(theOpCode);
					AddToCodeInt16(theOperand);
					break;
				}

				case ByteCode.LDC:
				case ByteCode.LDC_W:
				case ByteCode.LDC2_W:
				{
					if (!(0 <= theOperand && theOperand < 65536))
					{
						throw new ArgumentException("out of range index");
					}
					if (theOperand >= 256 || theOpCode == ByteCode.LDC_W || theOpCode == ByteCode.LDC2_W)
					{
						if (theOpCode == ByteCode.LDC)
						{
							AddToCodeBuffer(ByteCode.LDC_W);
						}
						else
						{
							AddToCodeBuffer(theOpCode);
						}
						AddToCodeInt16(theOperand);
					}
					else
					{
						AddToCodeBuffer(theOpCode);
						AddToCodeBuffer(theOperand);
					}
					break;
				}

				case ByteCode.RET:
				case ByteCode.ILOAD:
				case ByteCode.LLOAD:
				case ByteCode.FLOAD:
				case ByteCode.DLOAD:
				case ByteCode.ALOAD:
				case ByteCode.ISTORE:
				case ByteCode.LSTORE:
				case ByteCode.FSTORE:
				case ByteCode.DSTORE:
				case ByteCode.ASTORE:
				{
					if (!(0 <= theOperand && theOperand < 65536))
					{
						throw new ClassFileWriter.ClassFileFormatException("out of range variable");
					}
					if (theOperand >= 256)
					{
						AddToCodeBuffer(ByteCode.WIDE);
						AddToCodeBuffer(theOpCode);
						AddToCodeInt16(theOperand);
					}
					else
					{
						AddToCodeBuffer(theOpCode);
						AddToCodeBuffer(theOperand);
					}
					break;
				}

				default:
				{
					throw new ArgumentException("Unexpected opcode for 1 operand");
				}
			}
			itsStackTop = (short)newStack;
			if (newStack > itsMaxStack)
			{
				itsMaxStack = (short)newStack;
			}
		}

		/// <summary>Generate the load constant bytecode for the given integer.</summary>
		/// <remarks>Generate the load constant bytecode for the given integer.</remarks>
		/// <param name="k">the constant</param>
		public virtual void AddLoadConstant(int k)
		{
			switch (k)
			{
				case 0:
				{
					Add(ByteCode.ICONST_0);
					break;
				}

				case 1:
				{
					Add(ByteCode.ICONST_1);
					break;
				}

				case 2:
				{
					Add(ByteCode.ICONST_2);
					break;
				}

				case 3:
				{
					Add(ByteCode.ICONST_3);
					break;
				}

				case 4:
				{
					Add(ByteCode.ICONST_4);
					break;
				}

				case 5:
				{
					Add(ByteCode.ICONST_5);
					break;
				}

				default:
				{
					Add(ByteCode.LDC, itsConstantPool.AddConstant(k));
					break;
				}
			}
		}

		/// <summary>Generate the load constant bytecode for the given long.</summary>
		/// <remarks>Generate the load constant bytecode for the given long.</remarks>
		/// <param name="k">the constant</param>
		public virtual void AddLoadConstant(long k)
		{
			Add(ByteCode.LDC2_W, itsConstantPool.AddConstant(k));
		}

		/// <summary>Generate the load constant bytecode for the given float.</summary>
		/// <remarks>Generate the load constant bytecode for the given float.</remarks>
		/// <param name="k">the constant</param>
		public virtual void AddLoadConstant(float k)
		{
			Add(ByteCode.LDC, itsConstantPool.AddConstant(k));
		}

		/// <summary>Generate the load constant bytecode for the given double.</summary>
		/// <remarks>Generate the load constant bytecode for the given double.</remarks>
		/// <param name="k">the constant</param>
		public virtual void AddLoadConstant(double k)
		{
			Add(ByteCode.LDC2_W, itsConstantPool.AddConstant(k));
		}

		/// <summary>Generate the load constant bytecode for the given string.</summary>
		/// <remarks>Generate the load constant bytecode for the given string.</remarks>
		/// <param name="k">the constant</param>
		public virtual void AddLoadConstant(string k)
		{
			Add(ByteCode.LDC, itsConstantPool.AddConstant(k));
		}

		/// <summary>Add the given two-operand bytecode to the current method.</summary>
		/// <remarks>Add the given two-operand bytecode to the current method.</remarks>
		/// <param name="theOpCode">the opcode of the bytecode</param>
		/// <param name="theOperand1">the first operand of the bytecode</param>
		/// <param name="theOperand2">the second operand of the bytecode</param>
		public virtual void Add(int theOpCode, int theOperand1, int theOperand2)
		{
			int newStack = itsStackTop + StackChange(theOpCode);
			if (newStack < 0 || short.MaxValue < newStack)
			{
				BadStack(newStack);
			}
			if (theOpCode == ByteCode.IINC)
			{
				if (!(0 <= theOperand1 && theOperand1 < 65536))
				{
					throw new ClassFileWriter.ClassFileFormatException("out of range variable");
				}
				if (!(0 <= theOperand2 && theOperand2 < 65536))
				{
					throw new ClassFileWriter.ClassFileFormatException("out of range increment");
				}
				if (theOperand1 > 255 || theOperand2 < -128 || theOperand2 > 127)
				{
					AddToCodeBuffer(ByteCode.WIDE);
					AddToCodeBuffer(ByteCode.IINC);
					AddToCodeInt16(theOperand1);
					AddToCodeInt16(theOperand2);
				}
				else
				{
					AddToCodeBuffer(ByteCode.IINC);
					AddToCodeBuffer(theOperand1);
					AddToCodeBuffer(theOperand2);
				}
			}
			else
			{
				if (theOpCode == ByteCode.MULTIANEWARRAY)
				{
					if (!(0 <= theOperand1 && theOperand1 < 65536))
					{
						throw new ArgumentException("out of range index");
					}
					if (!(0 <= theOperand2 && theOperand2 < 256))
					{
						throw new ArgumentException("out of range dimensions");
					}
					AddToCodeBuffer(ByteCode.MULTIANEWARRAY);
					AddToCodeInt16(theOperand1);
					AddToCodeBuffer(theOperand2);
				}
				else
				{
					throw new ArgumentException("Unexpected opcode for 2 operands");
				}
			}
			itsStackTop = (short)newStack;
			if (newStack > itsMaxStack)
			{
				itsMaxStack = (short)newStack;
			}
		}

		public virtual void Add(int theOpCode, string className)
		{
			int newStack = itsStackTop + StackChange(theOpCode);
			if (newStack < 0 || short.MaxValue < newStack)
			{
				BadStack(newStack);
			}
			switch (theOpCode)
			{
				case ByteCode.NEW:
				case ByteCode.ANEWARRAY:
				case ByteCode.CHECKCAST:
				case ByteCode.INSTANCEOF:
				{
					short classIndex = itsConstantPool.AddClass(className);
					AddToCodeBuffer(theOpCode);
					AddToCodeInt16(classIndex);
					break;
				}

				default:
				{
					throw new ArgumentException("bad opcode for class reference");
				}
			}
			itsStackTop = (short)newStack;
			if (newStack > itsMaxStack)
			{
				itsMaxStack = (short)newStack;
			}
		}

		public virtual void Add(int theOpCode, string className, string fieldName, string fieldType)
		{
			int newStack = itsStackTop + StackChange(theOpCode);
			char fieldTypeChar = fieldType[0];
			int fieldSize = (fieldTypeChar == 'J' || fieldTypeChar == 'D') ? 2 : 1;
			switch (theOpCode)
			{
				case ByteCode.GETFIELD:
				case ByteCode.GETSTATIC:
				{
					newStack += fieldSize;
					break;
				}

				case ByteCode.PUTSTATIC:
				case ByteCode.PUTFIELD:
				{
					newStack -= fieldSize;
					break;
				}

				default:
				{
					throw new ArgumentException("bad opcode for field reference");
				}
			}
			if (newStack < 0 || short.MaxValue < newStack)
			{
				BadStack(newStack);
			}
			short fieldRefIndex = itsConstantPool.AddFieldRef(className, fieldName, fieldType);
			AddToCodeBuffer(theOpCode);
			AddToCodeInt16(fieldRefIndex);
			itsStackTop = (short)newStack;
			if (newStack > itsMaxStack)
			{
				itsMaxStack = (short)newStack;
			}
		}

		public virtual void AddInvoke(int theOpCode, string className, string methodName, string methodType)
		{
			int parameterInfo = SizeOfParameters(methodType);
			int parameterCount = (int)(((uint)parameterInfo) >> 16);
			int stackDiff = (short)parameterInfo;
			int newStack = itsStackTop + stackDiff;
			newStack += StackChange(theOpCode);
			// adjusts for 'this'
			if (newStack < 0 || short.MaxValue < newStack)
			{
				BadStack(newStack);
			}
			switch (theOpCode)
			{
				case ByteCode.INVOKEVIRTUAL:
				case ByteCode.INVOKESPECIAL:
				case ByteCode.INVOKESTATIC:
				case ByteCode.INVOKEINTERFACE:
				{
					AddToCodeBuffer(theOpCode);
					if (theOpCode == ByteCode.INVOKEINTERFACE)
					{
						short ifMethodRefIndex = itsConstantPool.AddInterfaceMethodRef(className, methodName, methodType);
						AddToCodeInt16(ifMethodRefIndex);
						AddToCodeBuffer(parameterCount + 1);
						AddToCodeBuffer(0);
					}
					else
					{
						short methodRefIndex = itsConstantPool.AddMethodRef(className, methodName, methodType);
						AddToCodeInt16(methodRefIndex);
					}
					break;
				}

				default:
				{
					throw new ArgumentException("bad opcode for method reference");
				}
			}
			itsStackTop = (short)newStack;
			if (newStack > itsMaxStack)
			{
				itsMaxStack = (short)newStack;
			}
		}

		/// <summary>Generate code to load the given integer on stack.</summary>
		/// <remarks>Generate code to load the given integer on stack.</remarks>
		/// <param name="k">the constant</param>
		public virtual void AddPush(int k)
		{
			if (unchecked((byte)k) == k)
			{
				if (k == -1)
				{
					Add(ByteCode.ICONST_M1);
				}
				else
				{
					if (0 <= k && k <= 5)
					{
						Add(unchecked((byte)(ByteCode.ICONST_0 + k)));
					}
					else
					{
						Add(ByteCode.BIPUSH, unchecked((byte)k));
					}
				}
			}
			else
			{
				if ((short)k == k)
				{
					Add(ByteCode.SIPUSH, (short)k);
				}
				else
				{
					AddLoadConstant(k);
				}
			}
		}

		public virtual void AddPush(bool k)
		{
			Add(k ? ByteCode.ICONST_1 : ByteCode.ICONST_0);
		}

		/// <summary>Generate code to load the given long on stack.</summary>
		/// <remarks>Generate code to load the given long on stack.</remarks>
		/// <param name="k">the constant</param>
		public virtual void AddPush(long k)
		{
			int ik = (int)k;
			if (ik == k)
			{
				AddPush(ik);
				Add(ByteCode.I2L);
			}
			else
			{
				AddLoadConstant(k);
			}
		}

		/// <summary>Generate code to load the given double on stack.</summary>
		/// <remarks>Generate code to load the given double on stack.</remarks>
		/// <param name="k">the constant</param>
		public virtual void AddPush(double k)
		{
			if (k == 0.0)
			{
				// zero
				Add(ByteCode.DCONST_0);
				if (1.0 / k < 0)
				{
					// Negative zero
					Add(ByteCode.DNEG);
				}
			}
			else
			{
				if (k == 1.0 || k == -1.0)
				{
					Add(ByteCode.DCONST_1);
					if (k < 0)
					{
						Add(ByteCode.DNEG);
					}
				}
				else
				{
					AddLoadConstant(k);
				}
			}
		}

		/// <summary>
		/// Generate the code to leave on stack the given string even if the
		/// string encoding exeeds the class file limit for single string constant
		/// </summary>
		/// <param name="k">the constant</param>
		public virtual void AddPush(string k)
		{
			int length = k.Length;
			int limit = itsConstantPool.GetUtfEncodingLimit(k, 0, length);
			if (limit == length)
			{
				AddLoadConstant(k);
				return;
			}
			// Split string into picies fitting the UTF limit and generate code for
			// StringBuffer sb = new StringBuffer(length);
			// sb.append(loadConstant(piece_1));
			// ...
			// sb.append(loadConstant(piece_N));
			// sb.toString();
			string SB = "java/lang/StringBuffer";
			Add(ByteCode.NEW, SB);
			Add(ByteCode.DUP);
			AddPush(length);
			AddInvoke(ByteCode.INVOKESPECIAL, SB, "<init>", "(I)V");
			int cursor = 0;
			for (; ; )
			{
				Add(ByteCode.DUP);
				string s = Sharpen.Runtime.Substring(k, cursor, limit);
				AddLoadConstant(s);
				AddInvoke(ByteCode.INVOKEVIRTUAL, SB, "append", "(Ljava/lang/String;)Ljava/lang/StringBuffer;");
				Add(ByteCode.POP);
				if (limit == length)
				{
					break;
				}
				cursor = limit;
				limit = itsConstantPool.GetUtfEncodingLimit(k, limit, length);
			}
			AddInvoke(ByteCode.INVOKEVIRTUAL, SB, "toString", "()Ljava/lang/String;");
		}

		/// <summary>
		/// Check if k fits limit on string constant size imposed by class file
		/// format.
		/// </summary>
		/// <remarks>
		/// Check if k fits limit on string constant size imposed by class file
		/// format.
		/// </remarks>
		/// <param name="k">the string constant</param>
		public virtual bool IsUnderStringSizeLimit(string k)
		{
			return itsConstantPool.IsUnderUtfEncodingLimit(k);
		}

		/// <summary>Store integer from stack top into the given local.</summary>
		/// <remarks>Store integer from stack top into the given local.</remarks>
		/// <param name="local">number of local register</param>
		public virtual void AddIStore(int local)
		{
			Xop(ByteCode.ISTORE_0, ByteCode.ISTORE, local);
		}

		/// <summary>Store long from stack top into the given local.</summary>
		/// <remarks>Store long from stack top into the given local.</remarks>
		/// <param name="local">number of local register</param>
		public virtual void AddLStore(int local)
		{
			Xop(ByteCode.LSTORE_0, ByteCode.LSTORE, local);
		}

		/// <summary>Store float from stack top into the given local.</summary>
		/// <remarks>Store float from stack top into the given local.</remarks>
		/// <param name="local">number of local register</param>
		public virtual void AddFStore(int local)
		{
			Xop(ByteCode.FSTORE_0, ByteCode.FSTORE, local);
		}

		/// <summary>Store double from stack top into the given local.</summary>
		/// <remarks>Store double from stack top into the given local.</remarks>
		/// <param name="local">number of local register</param>
		public virtual void AddDStore(int local)
		{
			Xop(ByteCode.DSTORE_0, ByteCode.DSTORE, local);
		}

		/// <summary>Store object from stack top into the given local.</summary>
		/// <remarks>Store object from stack top into the given local.</remarks>
		/// <param name="local">number of local register</param>
		public virtual void AddAStore(int local)
		{
			Xop(ByteCode.ASTORE_0, ByteCode.ASTORE, local);
		}

		/// <summary>Load integer from the given local into stack.</summary>
		/// <remarks>Load integer from the given local into stack.</remarks>
		/// <param name="local">number of local register</param>
		public virtual void AddILoad(int local)
		{
			Xop(ByteCode.ILOAD_0, ByteCode.ILOAD, local);
		}

		/// <summary>Load long from the given local into stack.</summary>
		/// <remarks>Load long from the given local into stack.</remarks>
		/// <param name="local">number of local register</param>
		public virtual void AddLLoad(int local)
		{
			Xop(ByteCode.LLOAD_0, ByteCode.LLOAD, local);
		}

		/// <summary>Load float from the given local into stack.</summary>
		/// <remarks>Load float from the given local into stack.</remarks>
		/// <param name="local">number of local register</param>
		public virtual void AddFLoad(int local)
		{
			Xop(ByteCode.FLOAD_0, ByteCode.FLOAD, local);
		}

		/// <summary>Load double from the given local into stack.</summary>
		/// <remarks>Load double from the given local into stack.</remarks>
		/// <param name="local">number of local register</param>
		public virtual void AddDLoad(int local)
		{
			Xop(ByteCode.DLOAD_0, ByteCode.DLOAD, local);
		}

		/// <summary>Load object from the given local into stack.</summary>
		/// <remarks>Load object from the given local into stack.</remarks>
		/// <param name="local">number of local register</param>
		public virtual void AddALoad(int local)
		{
			Xop(ByteCode.ALOAD_0, ByteCode.ALOAD, local);
		}

		/// <summary>Load "this" into stack.</summary>
		/// <remarks>Load "this" into stack.</remarks>
		public virtual void AddLoadThis()
		{
			Add(ByteCode.ALOAD_0);
		}

		private void Xop(int shortOp, int op, int local)
		{
			switch (local)
			{
				case 0:
				{
					Add(shortOp);
					break;
				}

				case 1:
				{
					Add(shortOp + 1);
					break;
				}

				case 2:
				{
					Add(shortOp + 2);
					break;
				}

				case 3:
				{
					Add(shortOp + 3);
					break;
				}

				default:
				{
					Add(op, local);
					break;
				}
			}
		}

		public virtual int AddTableSwitch(int low, int high)
		{
			if (low > high)
			{
				throw new ClassFileWriter.ClassFileFormatException("Bad bounds: " + low + ' ' + high);
			}
			int newStack = itsStackTop + StackChange(ByteCode.TABLESWITCH);
			if (newStack < 0 || short.MaxValue < newStack)
			{
				BadStack(newStack);
			}
			int entryCount = high - low + 1;
			int padSize = 3 & ~itsCodeBufferTop;
			// == 3 - itsCodeBufferTop % 4
			int N = AddReservedCodeSpace(1 + padSize + 4 * (1 + 2 + entryCount));
			int switchStart = N;
			itsCodeBuffer[N++] = unchecked((byte)ByteCode.TABLESWITCH);
			while (padSize != 0)
			{
				itsCodeBuffer[N++] = 0;
				--padSize;
			}
			N += 4;
			// skip default offset
			N = PutInt32(low, itsCodeBuffer, N);
			PutInt32(high, itsCodeBuffer, N);
			itsStackTop = (short)newStack;
			if (newStack > itsMaxStack)
			{
				itsMaxStack = (short)newStack;
			}
			return switchStart;
		}

		public void MarkTableSwitchDefault(int switchStart)
		{
			AddSuperBlockStart(itsCodeBufferTop);
			itsJumpFroms.Put(itsCodeBufferTop, switchStart);
			SetTableSwitchJump(switchStart, -1, itsCodeBufferTop);
		}

		public void MarkTableSwitchCase(int switchStart, int caseIndex)
		{
			AddSuperBlockStart(itsCodeBufferTop);
			itsJumpFroms.Put(itsCodeBufferTop, switchStart);
			SetTableSwitchJump(switchStart, caseIndex, itsCodeBufferTop);
		}

		public void MarkTableSwitchCase(int switchStart, int caseIndex, int stackTop)
		{
			if (!(0 <= stackTop && stackTop <= itsMaxStack))
			{
				throw new ArgumentException("Bad stack index: " + stackTop);
			}
			itsStackTop = (short)stackTop;
			AddSuperBlockStart(itsCodeBufferTop);
			itsJumpFroms.Put(itsCodeBufferTop, switchStart);
			SetTableSwitchJump(switchStart, caseIndex, itsCodeBufferTop);
		}

		/// <summary>Set a jump case for a tableswitch instruction.</summary>
		/// <remarks>
		/// Set a jump case for a tableswitch instruction. The jump target should
		/// be marked as a super block start for stack map generation.
		/// </remarks>
		public virtual void SetTableSwitchJump(int switchStart, int caseIndex, int jumpTarget)
		{
			if (!(0 <= jumpTarget && jumpTarget <= itsCodeBufferTop))
			{
				throw new ArgumentException("Bad jump target: " + jumpTarget);
			}
			if (!(caseIndex >= -1))
			{
				throw new ArgumentException("Bad case index: " + caseIndex);
			}
			int padSize = 3 & ~switchStart;
			// == 3 - switchStart % 4
			int caseOffset;
			if (caseIndex < 0)
			{
				// default label
				caseOffset = switchStart + 1 + padSize;
			}
			else
			{
				caseOffset = switchStart + 1 + padSize + 4 * (3 + caseIndex);
			}
			if (!(0 <= switchStart && switchStart <= itsCodeBufferTop - 4 * 4 - padSize - 1))
			{
				throw new ArgumentException(switchStart + " is outside a possible range of tableswitch" + " in already generated code");
			}
			if ((unchecked((int)(0xFF)) & itsCodeBuffer[switchStart]) != ByteCode.TABLESWITCH)
			{
				throw new ArgumentException(switchStart + " is not offset of tableswitch statement");
			}
			if (!(0 <= caseOffset && caseOffset + 4 <= itsCodeBufferTop))
			{
				// caseIndex >= -1 does not guarantee that caseOffset >= 0 due
				// to a possible overflow.
				throw new ClassFileWriter.ClassFileFormatException("Too big case index: " + caseIndex);
			}
			// ALERT: perhaps check against case bounds?
			PutInt32(jumpTarget - switchStart, itsCodeBuffer, caseOffset);
		}

		public virtual int AcquireLabel()
		{
			int top = itsLabelTableTop;
			if (itsLabelTable == null || top == itsLabelTable.Length)
			{
				if (itsLabelTable == null)
				{
					itsLabelTable = new int[MIN_LABEL_TABLE_SIZE];
				}
				else
				{
					int[] tmp = new int[itsLabelTable.Length * 2];
					System.Array.Copy(itsLabelTable, 0, tmp, 0, top);
					itsLabelTable = tmp;
				}
			}
			itsLabelTableTop = top + 1;
			itsLabelTable[top] = -1;
			return top | unchecked((int)(0x80000000));
		}

		public virtual void MarkLabel(int label)
		{
			if (!(label < 0))
			{
				throw new ArgumentException("Bad label, no biscuit");
			}
			label &= unchecked((int)(0x7FFFFFFF));
			if (label > itsLabelTableTop)
			{
				throw new ArgumentException("Bad label");
			}
			if (itsLabelTable[label] != -1)
			{
				throw new InvalidOperationException("Can only mark label once");
			}
			itsLabelTable[label] = itsCodeBufferTop;
		}

		public virtual void MarkLabel(int label, short stackTop)
		{
			MarkLabel(label);
			itsStackTop = stackTop;
		}

		public virtual void MarkHandler(int theLabel)
		{
			itsStackTop = 1;
			MarkLabel(theLabel);
		}

		public virtual int GetLabelPC(int label)
		{
			if (!(label < 0))
			{
				throw new ArgumentException("Bad label, no biscuit");
			}
			label &= unchecked((int)(0x7FFFFFFF));
			if (!(label < itsLabelTableTop))
			{
				throw new ArgumentException("Bad label");
			}
			return itsLabelTable[label];
		}

		private void AddLabelFixup(int label, int fixupSite)
		{
			if (!(label < 0))
			{
				throw new ArgumentException("Bad label, no biscuit");
			}
			label &= unchecked((int)(0x7FFFFFFF));
			if (!(label < itsLabelTableTop))
			{
				throw new ArgumentException("Bad label");
			}
			int top = itsFixupTableTop;
			if (itsFixupTable == null || top == itsFixupTable.Length)
			{
				if (itsFixupTable == null)
				{
					itsFixupTable = new long[MIN_FIXUP_TABLE_SIZE];
				}
				else
				{
					long[] tmp = new long[itsFixupTable.Length * 2];
					System.Array.Copy(itsFixupTable, 0, tmp, 0, top);
					itsFixupTable = tmp;
				}
			}
			itsFixupTableTop = top + 1;
			itsFixupTable[top] = ((long)label << 32) | fixupSite;
		}

		private void FixLabelGotos()
		{
			byte[] codeBuffer = itsCodeBuffer;
			for (int i = 0; i < itsFixupTableTop; i++)
			{
				long fixup = itsFixupTable[i];
				int label = (int)(fixup >> 32);
				int fixupSite = (int)fixup;
				int pc = itsLabelTable[label];
				if (pc == -1)
				{
					// Unlocated label
					throw new Exception();
				}
				// -1 to get delta from instruction start
				AddSuperBlockStart(pc);
				itsJumpFroms.Put(pc, fixupSite - 1);
				int offset = pc - (fixupSite - 1);
				if ((short)offset != offset)
				{
					throw new ClassFileWriter.ClassFileFormatException("Program too complex: too big jump offset");
				}
				codeBuffer[fixupSite] = unchecked((byte)(offset >> 8));
				codeBuffer[fixupSite + 1] = unchecked((byte)offset);
			}
			itsFixupTableTop = 0;
		}

		/// <summary>Get the current offset into the code of the current method.</summary>
		/// <remarks>Get the current offset into the code of the current method.</remarks>
		/// <returns>an integer representing the offset</returns>
		public virtual int GetCurrentCodeOffset()
		{
			return itsCodeBufferTop;
		}

		public virtual short GetStackTop()
		{
			return itsStackTop;
		}

		public virtual void SetStackTop(short n)
		{
			itsStackTop = n;
		}

		public virtual void AdjustStackTop(int delta)
		{
			int newStack = itsStackTop + delta;
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

		private void AddToCodeBuffer(int b)
		{
			int N = AddReservedCodeSpace(1);
			itsCodeBuffer[N] = unchecked((byte)b);
		}

		private void AddToCodeInt16(int value)
		{
			int N = AddReservedCodeSpace(2);
			PutInt16(value, itsCodeBuffer, N);
		}

		private int AddReservedCodeSpace(int size)
		{
			if (itsCurrentMethod == null)
			{
				throw new ArgumentException("No method to add to");
			}
			int oldTop = itsCodeBufferTop;
			int newTop = oldTop + size;
			if (newTop > itsCodeBuffer.Length)
			{
				int newSize = itsCodeBuffer.Length * 2;
				if (newTop > newSize)
				{
					newSize = newTop;
				}
				byte[] tmp = new byte[newSize];
				System.Array.Copy(itsCodeBuffer, 0, tmp, 0, oldTop);
				itsCodeBuffer = tmp;
			}
			itsCodeBufferTop = newTop;
			return oldTop;
		}

		public virtual void AddExceptionHandler(int startLabel, int endLabel, int handlerLabel, string catchClassName)
		{
			if ((startLabel & unchecked((int)(0x80000000))) != unchecked((int)(0x80000000)))
			{
				throw new ArgumentException("Bad startLabel");
			}
			if ((endLabel & unchecked((int)(0x80000000))) != unchecked((int)(0x80000000)))
			{
				throw new ArgumentException("Bad endLabel");
			}
			if ((handlerLabel & unchecked((int)(0x80000000))) != unchecked((int)(0x80000000)))
			{
				throw new ArgumentException("Bad handlerLabel");
			}
			short catch_type_index = (catchClassName == null) ? 0 : itsConstantPool.AddClass(catchClassName);
			ExceptionTableEntry newEntry = new ExceptionTableEntry(startLabel, endLabel, handlerLabel, catch_type_index);
			int N = itsExceptionTableTop;
			if (N == 0)
			{
				itsExceptionTable = new ExceptionTableEntry[ExceptionTableSize];
			}
			else
			{
				if (N == itsExceptionTable.Length)
				{
					ExceptionTableEntry[] tmp = new ExceptionTableEntry[N * 2];
					System.Array.Copy(itsExceptionTable, 0, tmp, 0, N);
					itsExceptionTable = tmp;
				}
			}
			itsExceptionTable[N] = newEntry;
			itsExceptionTableTop = N + 1;
		}

		public virtual void AddLineNumberEntry(short lineNumber)
		{
			if (itsCurrentMethod == null)
			{
				throw new ArgumentException("No method to stop");
			}
			int N = itsLineNumberTableTop;
			if (N == 0)
			{
				itsLineNumberTable = new int[LineNumberTableSize];
			}
			else
			{
				if (N == itsLineNumberTable.Length)
				{
					int[] tmp = new int[N * 2];
					System.Array.Copy(itsLineNumberTable, 0, tmp, 0, N);
					itsLineNumberTable = tmp;
				}
			}
			itsLineNumberTable[N] = (itsCodeBufferTop << 16) + lineNumber;
			itsLineNumberTableTop = N + 1;
		}

		/// <summary>
		/// A stack map table is a code attribute introduced in Java 6 that
		/// gives type information at key points in the method body (namely, at
		/// the beginning of each super block after the first).
		/// </summary>
		/// <remarks>
		/// A stack map table is a code attribute introduced in Java 6 that
		/// gives type information at key points in the method body (namely, at
		/// the beginning of each super block after the first). Each frame of a
		/// stack map table contains the state of local variable and operand stack
		/// for a given super block.
		/// </remarks>
		internal sealed class StackMapTable
		{
			internal StackMapTable(ClassFileWriter _enclosing)
			{
				this._enclosing = _enclosing;
				this.superBlocks = null;
				this.locals = this.stack = null;
				this.workList = null;
				this.rawStackMap = null;
				this.localsTop = 0;
				this.stackTop = 0;
				this.workListTop = 0;
				this.rawStackMapTop = 0;
				this.wide = false;
			}

			internal void Generate()
			{
				this.superBlocks = new SuperBlock[this._enclosing.itsSuperBlockStartsTop];
				int[] initialLocals = this._enclosing.CreateInitialLocals();
				for (int i = 0; i < this._enclosing.itsSuperBlockStartsTop; i++)
				{
					int start = this._enclosing.itsSuperBlockStarts[i];
					int end;
					if (i == this._enclosing.itsSuperBlockStartsTop - 1)
					{
						end = this._enclosing.itsCodeBufferTop;
					}
					else
					{
						end = this._enclosing.itsSuperBlockStarts[i + 1];
					}
					this.superBlocks[i] = new SuperBlock(i, start, end, initialLocals);
				}
				this.superBlockDeps = this.GetSuperBlockDependencies();
				this.Verify();
			}

			private SuperBlock GetSuperBlockFromOffset(int offset)
			{
				for (int i = 0; i < this.superBlocks.Length; i++)
				{
					SuperBlock sb = this.superBlocks[i];
					if (sb == null)
					{
						break;
					}
					else
					{
						if (offset >= sb.GetStart() && offset < sb.GetEnd())
						{
							return sb;
						}
					}
				}
				throw new ArgumentException("bad offset: " + offset);
			}

			/// <summary>
			/// Determine whether or not an opcode is an actual end to a super
			/// block.
			/// </summary>
			/// <remarks>
			/// Determine whether or not an opcode is an actual end to a super
			/// block. This includes any returns or unconditional jumps.
			/// </remarks>
			private bool IsSuperBlockEnd(int opcode)
			{
				switch (opcode)
				{
					case ByteCode.ARETURN:
					case ByteCode.FRETURN:
					case ByteCode.IRETURN:
					case ByteCode.LRETURN:
					case ByteCode.RETURN:
					case ByteCode.ATHROW:
					case ByteCode.GOTO:
					case ByteCode.GOTO_W:
					case ByteCode.TABLESWITCH:
					case ByteCode.LOOKUPSWITCH:
					{
						return true;
					}

					default:
					{
						return false;
					}
				}
			}

			/// <summary>Calculate partial dependencies for super blocks.</summary>
			/// <remarks>
			/// Calculate partial dependencies for super blocks.
			/// This is used as a workaround for dead code that is generated. Only
			/// one dependency per super block is given.
			/// </remarks>
			private SuperBlock[] GetSuperBlockDependencies()
			{
				SuperBlock[] deps = new SuperBlock[this.superBlocks.Length];
				for (int i = 0; i < this._enclosing.itsExceptionTableTop; i++)
				{
					ExceptionTableEntry ete = this._enclosing.itsExceptionTable[i];
					short startPC = (short)this._enclosing.GetLabelPC(ete.itsStartLabel);
					short handlerPC = (short)this._enclosing.GetLabelPC(ete.itsHandlerLabel);
					SuperBlock handlerSB = this.GetSuperBlockFromOffset(handlerPC);
					SuperBlock dep = this.GetSuperBlockFromOffset(startPC);
					deps[handlerSB.GetIndex()] = dep;
				}
				int[] targetPCs = this._enclosing.itsJumpFroms.GetKeys();
				for (int i_1 = 0; i_1 < targetPCs.Length; i_1++)
				{
					int targetPC = targetPCs[i_1];
					int branchPC = this._enclosing.itsJumpFroms.GetInt(targetPC, -1);
					SuperBlock branchSB = this.GetSuperBlockFromOffset(branchPC);
					SuperBlock targetSB = this.GetSuperBlockFromOffset(targetPC);
					deps[targetSB.GetIndex()] = branchSB;
				}
				return deps;
			}

			/// <summary>Get the target super block of a branch instruction.</summary>
			/// <remarks>Get the target super block of a branch instruction.</remarks>
			/// <param name="bci">the index of the branch instruction in the code buffer</param>
			private SuperBlock GetBranchTarget(int bci)
			{
				int target;
				if ((this._enclosing.itsCodeBuffer[bci] & unchecked((int)(0xFF))) == ByteCode.GOTO_W)
				{
					target = bci + this.GetOperand(bci + 1, 4);
				}
				else
				{
					target = bci + (short)this.GetOperand(bci + 1, 2);
				}
				return this.GetSuperBlockFromOffset(target);
			}

			/// <summary>
			/// Determine whether or not an opcode is a conditional or unconditional
			/// jump.
			/// </summary>
			/// <remarks>
			/// Determine whether or not an opcode is a conditional or unconditional
			/// jump.
			/// </remarks>
			private bool IsBranch(int opcode)
			{
				switch (opcode)
				{
					case ByteCode.GOTO:
					case ByteCode.GOTO_W:
					case ByteCode.IFEQ:
					case ByteCode.IFGE:
					case ByteCode.IFGT:
					case ByteCode.IFLE:
					case ByteCode.IFLT:
					case ByteCode.IFNE:
					case ByteCode.IFNONNULL:
					case ByteCode.IFNULL:
					case ByteCode.IF_ACMPEQ:
					case ByteCode.IF_ACMPNE:
					case ByteCode.IF_ICMPEQ:
					case ByteCode.IF_ICMPGE:
					case ByteCode.IF_ICMPGT:
					case ByteCode.IF_ICMPLE:
					case ByteCode.IF_ICMPLT:
					case ByteCode.IF_ICMPNE:
					{
						return true;
					}

					default:
					{
						return false;
					}
				}
			}

			private int GetOperand(int offset)
			{
				return this.GetOperand(offset, 1);
			}

			/// <summary>Extract a logical operand from the byte code.</summary>
			/// <remarks>
			/// Extract a logical operand from the byte code.
			/// This is used, for example, to get branch offsets.
			/// </remarks>
			private int GetOperand(int start, int size)
			{
				int result = 0;
				if (size > 4)
				{
					throw new ArgumentException("bad operand size");
				}
				for (int i = 0; i < size; i++)
				{
					result = (result << 8) | (this._enclosing.itsCodeBuffer[start + i] & unchecked((int)(0xFF)));
				}
				return result;
			}

			/// <summary>
			/// Calculate initial local variable and op stack types for each super
			/// block in the method.
			/// </summary>
			/// <remarks>
			/// Calculate initial local variable and op stack types for each super
			/// block in the method.
			/// </remarks>
			private void Verify()
			{
				int[] initialLocals = this._enclosing.CreateInitialLocals();
				this.superBlocks[0].Merge(initialLocals, initialLocals.Length, new int[0], 0, this._enclosing.itsConstantPool);
				// Start from the top of the method and queue up block dependencies
				// as they come along.
				this.workList = new SuperBlock[] { this.superBlocks[0] };
				this.workListTop = 1;
				this.ExecuteWorkList();
				// Replace dead code with no-ops.
				for (int i = 0; i < this.superBlocks.Length; i++)
				{
					SuperBlock sb = this.superBlocks[i];
					if (!sb.IsInitialized())
					{
						this.KillSuperBlock(sb);
					}
				}
				this.ExecuteWorkList();
			}

			/// <summary>Replace the contents of a super block with no-ops.</summary>
			/// <remarks>
			/// Replace the contents of a super block with no-ops.
			/// The above description is not strictly true; the last instruction is
			/// an athrow instruction. This technique is borrowed from ASM's
			/// developer guide: http://asm.ow2.org/doc/developer-guide.html#deadcode
			/// The proposed algorithm fills a block with nop, ending it with an
			/// athrow. The stack map generated would be empty locals with an
			/// exception on the stack. In theory, it shouldn't matter what the
			/// locals are, as long as the stack has an exception for the athrow bit.
			/// However, it turns out that if the code being modified falls into an
			/// exception handler, it causes problems. Therefore, if it does, then
			/// we steal the locals from the exception block.
			/// If the block itself is an exception handler, we remove it from the
			/// exception table to simplify block dependencies.
			/// </remarks>
			private void KillSuperBlock(SuperBlock sb)
			{
				int[] locals = new int[0];
				int[] stack = new int[] { TypeInfo.OBJECT("java/lang/Throwable", this._enclosing.itsConstantPool) };
				// If the super block is handled by any exception handler, use its
				// locals as the killed block's locals. Ignore uninitialized
				// handlers, because they will also be killed and removed from the
				// exception table.
				for (int i = 0; i < this._enclosing.itsExceptionTableTop; i++)
				{
					ExceptionTableEntry ete = this._enclosing.itsExceptionTable[i];
					int eteStart = this._enclosing.GetLabelPC(ete.itsStartLabel);
					int eteEnd = this._enclosing.GetLabelPC(ete.itsEndLabel);
					int handlerPC = this._enclosing.GetLabelPC(ete.itsHandlerLabel);
					SuperBlock handlerSB = this.GetSuperBlockFromOffset(handlerPC);
					if ((sb.GetStart() > eteStart && sb.GetStart() < eteEnd) || (eteStart > sb.GetStart() && eteStart < sb.GetEnd()) && handlerSB.IsInitialized())
					{
						locals = handlerSB.GetLocals();
						break;
					}
				}
				// Remove any exception table entry whose handler is the killed
				// block. This removes block dependencies to make stack maps for
				// dead blocks easier to create.
				for (int i_1 = 0; i_1 < this._enclosing.itsExceptionTableTop; i_1++)
				{
					ExceptionTableEntry ete = this._enclosing.itsExceptionTable[i_1];
					int eteStart = this._enclosing.GetLabelPC(ete.itsStartLabel);
					if (eteStart == sb.GetStart())
					{
						for (int j = i_1 + 1; j < this._enclosing.itsExceptionTableTop; j++)
						{
							this._enclosing.itsExceptionTable[j - 1] = this._enclosing.itsExceptionTable[j];
						}
						this._enclosing.itsExceptionTableTop--;
						i_1--;
					}
				}
				sb.Merge(locals, locals.Length, stack, stack.Length, this._enclosing.itsConstantPool);
				int end = sb.GetEnd() - 1;
				this._enclosing.itsCodeBuffer[end] = unchecked((byte)ByteCode.ATHROW);
				for (int bci = sb.GetStart(); bci < end; bci++)
				{
					this._enclosing.itsCodeBuffer[bci] = unchecked((byte)ByteCode.NOP);
				}
			}

			private void ExecuteWorkList()
			{
				while (this.workListTop > 0)
				{
					SuperBlock work = this.workList[--this.workListTop];
					work.SetInQueue(false);
					this.locals = work.GetLocals();
					this.stack = work.GetStack();
					this.localsTop = this.locals.Length;
					this.stackTop = this.stack.Length;
					this.ExecuteBlock(work);
				}
			}

			/// <summary>Simulate the local variable and op stack for a super block.</summary>
			/// <remarks>Simulate the local variable and op stack for a super block.</remarks>
			private void ExecuteBlock(SuperBlock work)
			{
				int bc = 0;
				int next = 0;
				for (int bci = work.GetStart(); bci < work.GetEnd(); bci += next)
				{
					bc = this._enclosing.itsCodeBuffer[bci] & unchecked((int)(0xFF));
					next = this.Execute(bci);
					// If we have a branch to some super block, we need to merge
					// the current state of the local table and op stack with what's
					// currently stored as the initial state of the super block. If
					// something actually changed, we need to add it to the work
					// list.
					if (this.IsBranch(bc))
					{
						SuperBlock targetSB = this.GetBranchTarget(bci);
						this.FlowInto(targetSB);
					}
					else
					{
						if (bc == ByteCode.TABLESWITCH)
						{
							int switchStart = bci + 1 + (3 & ~bci);
							// 3 - bci % 4
							int defaultOffset = this.GetOperand(switchStart, 4);
							SuperBlock targetSB = this.GetSuperBlockFromOffset(bci + defaultOffset);
							this.FlowInto(targetSB);
							int low = this.GetOperand(switchStart + 4, 4);
							int high = this.GetOperand(switchStart + 8, 4);
							int numCases = high - low + 1;
							int caseBase = switchStart + 12;
							for (int i = 0; i < numCases; i++)
							{
								int label = bci + this.GetOperand(caseBase + 4 * i, 4);
								targetSB = this.GetSuperBlockFromOffset(label);
								this.FlowInto(targetSB);
							}
						}
					}
					for (int i_1 = 0; i_1 < this._enclosing.itsExceptionTableTop; i_1++)
					{
						ExceptionTableEntry ete = this._enclosing.itsExceptionTable[i_1];
						short startPC = (short)this._enclosing.GetLabelPC(ete.itsStartLabel);
						short endPC = (short)this._enclosing.GetLabelPC(ete.itsEndLabel);
						if (bci < startPC || bci >= endPC)
						{
							continue;
						}
						short handlerPC = (short)this._enclosing.GetLabelPC(ete.itsHandlerLabel);
						SuperBlock sb = this.GetSuperBlockFromOffset(handlerPC);
						int exceptionType;
						if (ete.itsCatchType == 0)
						{
							exceptionType = TypeInfo.OBJECT(this._enclosing.itsConstantPool.AddClass("java/lang/Throwable"));
						}
						else
						{
							exceptionType = TypeInfo.OBJECT(ete.itsCatchType);
						}
						sb.Merge(this.locals, this.localsTop, new int[] { exceptionType }, 1, this._enclosing.itsConstantPool);
						this.AddToWorkList(sb);
					}
				}
				// Check the last instruction to see if it is a true end of a
				// super block (ie., if the instruction is a return). If it
				// isn't, we need to continue processing the next chunk.
				if (!this.IsSuperBlockEnd(bc))
				{
					int nextIndex = work.GetIndex() + 1;
					if (nextIndex < this.superBlocks.Length)
					{
						this.FlowInto(this.superBlocks[nextIndex]);
					}
				}
			}

			/// <summary>
			/// Perform a merge of type state and add the super block to the work
			/// list if the merge changed anything.
			/// </summary>
			/// <remarks>
			/// Perform a merge of type state and add the super block to the work
			/// list if the merge changed anything.
			/// </remarks>
			private void FlowInto(SuperBlock sb)
			{
				if (sb.Merge(this.locals, this.localsTop, this.stack, this.stackTop, this._enclosing.itsConstantPool))
				{
					this.AddToWorkList(sb);
				}
			}

			private void AddToWorkList(SuperBlock sb)
			{
				if (!sb.IsInQueue())
				{
					sb.SetInQueue(true);
					sb.SetInitialized(true);
					if (this.workListTop == this.workList.Length)
					{
						SuperBlock[] tmp = new SuperBlock[this.workListTop * 2];
						System.Array.Copy(this.workList, 0, tmp, 0, this.workListTop);
						this.workList = tmp;
					}
					this.workList[this.workListTop++] = sb;
				}
			}

			/// <summary>Execute a single byte code instruction.</summary>
			/// <remarks>Execute a single byte code instruction.</remarks>
			/// <param name="bci">the index of the byte code instruction to execute</param>
			/// <returns>the length of the byte code instruction</returns>
			private int Execute(int bci)
			{
				int bc = this._enclosing.itsCodeBuffer[bci] & unchecked((int)(0xFF));
				int type;
				int type2;
				int index;
				int length = 0;
				long lType;
				long lType2;
				string className;
				switch (bc)
				{
					case ByteCode.NOP:
					case ByteCode.IINC:
					case ByteCode.GOTO:
					case ByteCode.GOTO_W:
					{
						// No change
						break;
					}

					case ByteCode.CHECKCAST:
					{
						this.Pop();
						this.Push(TypeInfo.OBJECT(this.GetOperand(bci + 1, 2)));
						break;
					}

					case ByteCode.IASTORE:
					case ByteCode.LASTORE:
					case ByteCode.FASTORE:
					case ByteCode.DASTORE:
					case ByteCode.AASTORE:
					case ByteCode.BASTORE:
					case ByteCode.CASTORE:
					case ByteCode.SASTORE:
					{
						// pop; pop; pop
						this.Pop();
						goto case ByteCode.PUTFIELD;
					}

					case ByteCode.PUTFIELD:
					case ByteCode.IF_ICMPEQ:
					case ByteCode.IF_ICMPNE:
					case ByteCode.IF_ICMPLT:
					case ByteCode.IF_ICMPGE:
					case ByteCode.IF_ICMPGT:
					case ByteCode.IF_ICMPLE:
					case ByteCode.IF_ACMPEQ:
					case ByteCode.IF_ACMPNE:
					{
						// pop; pop
						this.Pop();
						goto case ByteCode.IFEQ;
					}

					case ByteCode.IFEQ:
					case ByteCode.IFNE:
					case ByteCode.IFLT:
					case ByteCode.IFGE:
					case ByteCode.IFGT:
					case ByteCode.IFLE:
					case ByteCode.IFNULL:
					case ByteCode.IFNONNULL:
					case ByteCode.POP:
					case ByteCode.MONITORENTER:
					case ByteCode.MONITOREXIT:
					case ByteCode.PUTSTATIC:
					{
						// pop
						this.Pop();
						break;
					}

					case ByteCode.POP2:
					{
						this.Pop2();
						break;
					}

					case ByteCode.ACONST_NULL:
					{
						this.Push(TypeInfo.NULL);
						break;
					}

					case ByteCode.IALOAD:
					case ByteCode.BALOAD:
					case ByteCode.CALOAD:
					case ByteCode.SALOAD:
					case ByteCode.IADD:
					case ByteCode.ISUB:
					case ByteCode.IMUL:
					case ByteCode.IDIV:
					case ByteCode.IREM:
					case ByteCode.ISHL:
					case ByteCode.ISHR:
					case ByteCode.IUSHR:
					case ByteCode.IAND:
					case ByteCode.IOR:
					case ByteCode.IXOR:
					case ByteCode.LCMP:
					case ByteCode.FCMPL:
					case ByteCode.FCMPG:
					case ByteCode.DCMPL:
					case ByteCode.DCMPG:
					{
						// pop; pop; push(INTEGER)
						this.Pop();
						goto case ByteCode.INEG;
					}

					case ByteCode.INEG:
					case ByteCode.L2I:
					case ByteCode.F2I:
					case ByteCode.D2I:
					case ByteCode.I2B:
					case ByteCode.I2C:
					case ByteCode.I2S:
					case ByteCode.ARRAYLENGTH:
					case ByteCode.INSTANCEOF:
					{
						// pop; push(INTEGER)
						this.Pop();
						goto case ByteCode.ICONST_M1;
					}

					case ByteCode.ICONST_M1:
					case ByteCode.ICONST_0:
					case ByteCode.ICONST_1:
					case ByteCode.ICONST_2:
					case ByteCode.ICONST_3:
					case ByteCode.ICONST_4:
					case ByteCode.ICONST_5:
					case ByteCode.ILOAD:
					case ByteCode.ILOAD_0:
					case ByteCode.ILOAD_1:
					case ByteCode.ILOAD_2:
					case ByteCode.ILOAD_3:
					case ByteCode.BIPUSH:
					case ByteCode.SIPUSH:
					{
						// push(INTEGER)
						this.Push(TypeInfo.INTEGER);
						break;
					}

					case ByteCode.LALOAD:
					case ByteCode.LADD:
					case ByteCode.LSUB:
					case ByteCode.LMUL:
					case ByteCode.LDIV:
					case ByteCode.LREM:
					case ByteCode.LSHL:
					case ByteCode.LSHR:
					case ByteCode.LUSHR:
					case ByteCode.LAND:
					case ByteCode.LOR:
					case ByteCode.LXOR:
					{
						// pop; pop; push(LONG)
						this.Pop();
						goto case ByteCode.LNEG;
					}

					case ByteCode.LNEG:
					case ByteCode.I2L:
					case ByteCode.F2L:
					case ByteCode.D2L:
					{
						// pop; push(LONG)
						this.Pop();
						goto case ByteCode.LCONST_0;
					}

					case ByteCode.LCONST_0:
					case ByteCode.LCONST_1:
					case ByteCode.LLOAD:
					case ByteCode.LLOAD_0:
					case ByteCode.LLOAD_1:
					case ByteCode.LLOAD_2:
					case ByteCode.LLOAD_3:
					{
						// push(LONG)
						this.Push(TypeInfo.LONG);
						break;
					}

					case ByteCode.FALOAD:
					case ByteCode.FADD:
					case ByteCode.FSUB:
					case ByteCode.FMUL:
					case ByteCode.FDIV:
					case ByteCode.FREM:
					{
						// pop; pop; push(FLOAT)
						this.Pop();
						goto case ByteCode.FNEG;
					}

					case ByteCode.FNEG:
					case ByteCode.I2F:
					case ByteCode.L2F:
					case ByteCode.D2F:
					{
						// pop; push(FLOAT)
						this.Pop();
						goto case ByteCode.FCONST_0;
					}

					case ByteCode.FCONST_0:
					case ByteCode.FCONST_1:
					case ByteCode.FCONST_2:
					case ByteCode.FLOAD:
					case ByteCode.FLOAD_0:
					case ByteCode.FLOAD_1:
					case ByteCode.FLOAD_2:
					case ByteCode.FLOAD_3:
					{
						// push(FLOAT)
						this.Push(TypeInfo.FLOAT);
						break;
					}

					case ByteCode.DALOAD:
					case ByteCode.DADD:
					case ByteCode.DSUB:
					case ByteCode.DMUL:
					case ByteCode.DDIV:
					case ByteCode.DREM:
					{
						// pop; pop; push(DOUBLE)
						this.Pop();
						goto case ByteCode.DNEG;
					}

					case ByteCode.DNEG:
					case ByteCode.I2D:
					case ByteCode.L2D:
					case ByteCode.F2D:
					{
						// pop; push(DOUBLE)
						this.Pop();
						goto case ByteCode.DCONST_0;
					}

					case ByteCode.DCONST_0:
					case ByteCode.DCONST_1:
					case ByteCode.DLOAD:
					case ByteCode.DLOAD_0:
					case ByteCode.DLOAD_1:
					case ByteCode.DLOAD_2:
					case ByteCode.DLOAD_3:
					{
						// push(DOUBLE)
						this.Push(TypeInfo.DOUBLE);
						break;
					}

					case ByteCode.ISTORE:
					{
						this.ExecuteStore(this.GetOperand(bci + 1, this.wide ? 2 : 1), TypeInfo.INTEGER);
						break;
					}

					case ByteCode.ISTORE_0:
					case ByteCode.ISTORE_1:
					case ByteCode.ISTORE_2:
					case ByteCode.ISTORE_3:
					{
						this.ExecuteStore(bc - ByteCode.ISTORE_0, TypeInfo.INTEGER);
						break;
					}

					case ByteCode.LSTORE:
					{
						this.ExecuteStore(this.GetOperand(bci + 1, this.wide ? 2 : 1), TypeInfo.LONG);
						break;
					}

					case ByteCode.LSTORE_0:
					case ByteCode.LSTORE_1:
					case ByteCode.LSTORE_2:
					case ByteCode.LSTORE_3:
					{
						this.ExecuteStore(bc - ByteCode.LSTORE_0, TypeInfo.LONG);
						break;
					}

					case ByteCode.FSTORE:
					{
						this.ExecuteStore(this.GetOperand(bci + 1, this.wide ? 2 : 1), TypeInfo.FLOAT);
						break;
					}

					case ByteCode.FSTORE_0:
					case ByteCode.FSTORE_1:
					case ByteCode.FSTORE_2:
					case ByteCode.FSTORE_3:
					{
						this.ExecuteStore(bc - ByteCode.FSTORE_0, TypeInfo.FLOAT);
						break;
					}

					case ByteCode.DSTORE:
					{
						this.ExecuteStore(this.GetOperand(bci + 1, this.wide ? 2 : 1), TypeInfo.DOUBLE);
						break;
					}

					case ByteCode.DSTORE_0:
					case ByteCode.DSTORE_1:
					case ByteCode.DSTORE_2:
					case ByteCode.DSTORE_3:
					{
						this.ExecuteStore(bc - ByteCode.DSTORE_0, TypeInfo.DOUBLE);
						break;
					}

					case ByteCode.ALOAD:
					{
						this.ExecuteALoad(this.GetOperand(bci + 1, this.wide ? 2 : 1));
						break;
					}

					case ByteCode.ALOAD_0:
					case ByteCode.ALOAD_1:
					case ByteCode.ALOAD_2:
					case ByteCode.ALOAD_3:
					{
						this.ExecuteALoad(bc - ByteCode.ALOAD_0);
						break;
					}

					case ByteCode.ASTORE:
					{
						this.ExecuteAStore(this.GetOperand(bci + 1, this.wide ? 2 : 1));
						break;
					}

					case ByteCode.ASTORE_0:
					case ByteCode.ASTORE_1:
					case ByteCode.ASTORE_2:
					case ByteCode.ASTORE_3:
					{
						this.ExecuteAStore(bc - ByteCode.ASTORE_0);
						break;
					}

					case ByteCode.IRETURN:
					case ByteCode.LRETURN:
					case ByteCode.FRETURN:
					case ByteCode.DRETURN:
					case ByteCode.ARETURN:
					case ByteCode.RETURN:
					{
						this.ClearStack();
						break;
					}

					case ByteCode.ATHROW:
					{
						type = this.Pop();
						this.ClearStack();
						this.Push(type);
						break;
					}

					case ByteCode.SWAP:
					{
						type = this.Pop();
						type2 = this.Pop();
						this.Push(type);
						this.Push(type2);
						break;
					}

					case ByteCode.LDC:
					case ByteCode.LDC_W:
					case ByteCode.LDC2_W:
					{
						if (bc == ByteCode.LDC)
						{
							index = this.GetOperand(bci + 1);
						}
						else
						{
							index = this.GetOperand(bci + 1, 2);
						}
						byte constType = this._enclosing.itsConstantPool.GetConstantType(index);
						switch (constType)
						{
							case ConstantPool.CONSTANT_Double:
							{
								this.Push(TypeInfo.DOUBLE);
								break;
							}

							case ConstantPool.CONSTANT_Float:
							{
								this.Push(TypeInfo.FLOAT);
								break;
							}

							case ConstantPool.CONSTANT_Long:
							{
								this.Push(TypeInfo.LONG);
								break;
							}

							case ConstantPool.CONSTANT_Integer:
							{
								this.Push(TypeInfo.INTEGER);
								break;
							}

							case ConstantPool.CONSTANT_String:
							{
								this.Push(TypeInfo.OBJECT("java/lang/String", this._enclosing.itsConstantPool));
								break;
							}

							default:
							{
								throw new ArgumentException("bad const type " + constType);
							}
						}
						break;
					}

					case ByteCode.NEW:
					{
						this.Push(TypeInfo.UNINITIALIZED_VARIABLE(bci));
						break;
					}

					case ByteCode.NEWARRAY:
					{
						this.Pop();
						char componentType = ClassFileWriter.ArrayTypeToName(this._enclosing.itsCodeBuffer[bci + 1]);
						index = this._enclosing.itsConstantPool.AddClass("[" + componentType);
						this.Push(TypeInfo.OBJECT((short)index));
						break;
					}

					case ByteCode.ANEWARRAY:
					{
						index = this.GetOperand(bci + 1, 2);
						className = (string)this._enclosing.itsConstantPool.GetConstantData(index);
						this.Pop();
						this.Push(TypeInfo.OBJECT("[L" + className + ';', this._enclosing.itsConstantPool));
						break;
					}

					case ByteCode.INVOKEVIRTUAL:
					case ByteCode.INVOKESPECIAL:
					case ByteCode.INVOKESTATIC:
					case ByteCode.INVOKEINTERFACE:
					{
						index = this.GetOperand(bci + 1, 2);
						FieldOrMethodRef m = (FieldOrMethodRef)this._enclosing.itsConstantPool.GetConstantData(index);
						string methodType = m.GetType();
						string methodName = m.GetName();
						int parameterCount = (int)(((uint)ClassFileWriter.SizeOfParameters(methodType)) >> 16);
						for (int i = 0; i < parameterCount; i++)
						{
							this.Pop();
						}
						if (bc != ByteCode.INVOKESTATIC)
						{
							int instType = this.Pop();
							int tag = TypeInfo.GetTag(instType);
							if (tag == TypeInfo.UNINITIALIZED_VARIABLE(0) || tag == TypeInfo.UNINITIALIZED_THIS)
							{
								if ("<init>".Equals(methodName))
								{
									int newType = TypeInfo.OBJECT(this._enclosing.itsThisClassIndex);
									this.InitializeTypeInfo(instType, newType);
								}
								else
								{
									throw new InvalidOperationException("bad instance");
								}
							}
						}
						int rParen = methodType.IndexOf(')');
						string returnType = Sharpen.Runtime.Substring(methodType, rParen + 1);
						returnType = ClassFileWriter.DescriptorToInternalName(returnType);
						if (!returnType.Equals("V"))
						{
							this.Push(TypeInfo.FromType(returnType, this._enclosing.itsConstantPool));
						}
						break;
					}

					case ByteCode.GETFIELD:
					{
						this.Pop();
						goto case ByteCode.GETSTATIC;
					}

					case ByteCode.GETSTATIC:
					{
						index = this.GetOperand(bci + 1, 2);
						FieldOrMethodRef f = (FieldOrMethodRef)this._enclosing.itsConstantPool.GetConstantData(index);
						string fieldType = ClassFileWriter.DescriptorToInternalName(f.GetType());
						this.Push(TypeInfo.FromType(fieldType, this._enclosing.itsConstantPool));
						break;
					}

					case ByteCode.DUP:
					{
						type = this.Pop();
						this.Push(type);
						this.Push(type);
						break;
					}

					case ByteCode.DUP_X1:
					{
						type = this.Pop();
						type2 = this.Pop();
						this.Push(type);
						this.Push(type2);
						this.Push(type);
						break;
					}

					case ByteCode.DUP_X2:
					{
						type = this.Pop();
						lType = this.Pop2();
						this.Push(type);
						this.Push2(lType);
						this.Push(type);
						break;
					}

					case ByteCode.DUP2:
					{
						lType = this.Pop2();
						this.Push2(lType);
						this.Push2(lType);
						break;
					}

					case ByteCode.DUP2_X1:
					{
						lType = this.Pop2();
						type = this.Pop();
						this.Push2(lType);
						this.Push(type);
						this.Push2(lType);
						break;
					}

					case ByteCode.DUP2_X2:
					{
						lType = this.Pop2();
						lType2 = this.Pop2();
						this.Push2(lType);
						this.Push2(lType2);
						this.Push2(lType);
						break;
					}

					case ByteCode.TABLESWITCH:
					{
						int switchStart = bci + 1 + (3 & ~bci);
						int low = this.GetOperand(switchStart + 4, 4);
						int high = this.GetOperand(switchStart + 8, 4);
						length = 4 * (high - low + 4) + switchStart - bci;
						this.Pop();
						break;
					}

					case ByteCode.AALOAD:
					{
						this.Pop();
						int typeIndex = (int)(((uint)this.Pop()) >> 8);
						className = (string)this._enclosing.itsConstantPool.GetConstantData(typeIndex);
						string arrayType = className;
						if (arrayType[0] != '[')
						{
							throw new InvalidOperationException("bad array type");
						}
						string elementDesc = Sharpen.Runtime.Substring(arrayType, 1);
						string elementType = ClassFileWriter.DescriptorToInternalName(elementDesc);
						typeIndex = this._enclosing.itsConstantPool.AddClass(elementType);
						this.Push(TypeInfo.OBJECT(typeIndex));
						break;
					}

					case ByteCode.WIDE:
					{
						// Alters behaviour of next instruction
						this.wide = true;
						break;
					}

					case ByteCode.MULTIANEWARRAY:
					case ByteCode.LOOKUPSWITCH:
					case ByteCode.JSR:
					case ByteCode.RET:
					case ByteCode.JSR_W:
					default:
					{
						// Currently not used in any part of Rhino, so ignore it
						// TODO: JSR is deprecated
						throw new ArgumentException("bad opcode: " + bc);
					}
				}
				if (length == 0)
				{
					length = ClassFileWriter.OpcodeLength(bc, this.wide);
				}
				if (this.wide && bc != ByteCode.WIDE)
				{
					this.wide = false;
				}
				return length;
			}

			private void ExecuteALoad(int localIndex)
			{
				int type = this.GetLocal(localIndex);
				int tag = TypeInfo.GetTag(type);
				if (tag == TypeInfo.OBJECT_TAG || tag == TypeInfo.UNINITIALIZED_THIS || tag == TypeInfo.UNINITIALIZED_VAR_TAG || tag == TypeInfo.NULL)
				{
					this.Push(type);
				}
				else
				{
					throw new InvalidOperationException("bad local variable type: " + type + " at index: " + localIndex);
				}
			}

			private void ExecuteAStore(int localIndex)
			{
				this.SetLocal(localIndex, this.Pop());
			}

			private void ExecuteStore(int localIndex, int typeInfo)
			{
				this.Pop();
				this.SetLocal(localIndex, typeInfo);
			}

			/// <summary>
			/// Change an UNINITIALIZED_OBJECT or UNINITIALIZED_THIS to the proper
			/// type of the object.
			/// </summary>
			/// <remarks>
			/// Change an UNINITIALIZED_OBJECT or UNINITIALIZED_THIS to the proper
			/// type of the object. This occurs when the proper constructor is
			/// invoked.
			/// </remarks>
			private void InitializeTypeInfo(int prevType, int newType)
			{
				this.InitializeTypeInfo(prevType, newType, this.locals, this.localsTop);
				this.InitializeTypeInfo(prevType, newType, this.stack, this.stackTop);
			}

			private void InitializeTypeInfo(int prevType, int newType, int[] data, int dataTop)
			{
				for (int i = 0; i < dataTop; i++)
				{
					if (data[i] == prevType)
					{
						data[i] = newType;
					}
				}
			}

			private int GetLocal(int localIndex)
			{
				if (localIndex < this.localsTop)
				{
					return this.locals[localIndex];
				}
				else
				{
					return TypeInfo.TOP;
				}
			}

			private void SetLocal(int localIndex, int typeInfo)
			{
				if (localIndex >= this.localsTop)
				{
					int[] tmp = new int[localIndex + 1];
					System.Array.Copy(this.locals, 0, tmp, 0, this.localsTop);
					this.locals = tmp;
					this.localsTop = localIndex + 1;
				}
				this.locals[localIndex] = typeInfo;
			}

			private void Push(int typeInfo)
			{
				if (this.stackTop == this.stack.Length)
				{
					int[] tmp = new int[Math.Max(this.stackTop * 2, 4)];
					System.Array.Copy(this.stack, 0, tmp, 0, this.stackTop);
					this.stack = tmp;
				}
				this.stack[this.stackTop++] = typeInfo;
			}

			private int Pop()
			{
				return this.stack[--this.stackTop];
			}

			/// <summary>Push two words onto the op stack.</summary>
			/// <remarks>
			/// Push two words onto the op stack.
			/// This is only meant to be used as a complement to pop2(), and both
			/// methods are helpers for the more complex DUP operations.
			/// </remarks>
			private void Push2(long typeInfo)
			{
				this.Push((int)(typeInfo & unchecked((int)(0xFFFFFF))));
				typeInfo = (long)(((ulong)typeInfo) >> 32);
				if (typeInfo != 0)
				{
					this.Push((int)(typeInfo & unchecked((int)(0xFFFFFF))));
				}
			}

			/// <summary>Pop two words from the op stack.</summary>
			/// <remarks>
			/// Pop two words from the op stack.
			/// If the top of the stack is a DOUBLE or LONG, then the bottom 32 bits
			/// reflects the appropriate type and the top 32 bits are 0. Otherwise,
			/// the top 32 bits are the first word on the stack and the lower 32
			/// bits are the second word on the stack.
			/// </remarks>
			private long Pop2()
			{
				long type = this.Pop();
				if (TypeInfo.IsTwoWords((int)type))
				{
					return type;
				}
				else
				{
					return type << 32 | (this.Pop() & unchecked((int)(0xFFFFFF)));
				}
			}

			private void ClearStack()
			{
				this.stackTop = 0;
			}

			/// <summary>Compute the output size of the stack map table.</summary>
			/// <remarks>
			/// Compute the output size of the stack map table.
			/// Because this would share much in common with actual writing of the
			/// stack map table, we instead just write the stack map table to a
			/// buffer and return the size from it. The buffer is later used in
			/// the actual writing of bytecode.
			/// </remarks>
			internal int ComputeWriteSize()
			{
				// Allocate a buffer that can handle the worst case size of the
				// stack map to prevent lots of reallocations.
				int writeSize = this.GetWorstCaseWriteSize();
				this.rawStackMap = new byte[writeSize];
				this.ComputeRawStackMap();
				return this.rawStackMapTop + 2;
			}

			internal int Write(byte[] data, int offset)
			{
				offset = ClassFileWriter.PutInt32(this.rawStackMapTop + 2, data, offset);
				offset = ClassFileWriter.PutInt16(this.superBlocks.Length - 1, data, offset);
				System.Array.Copy(this.rawStackMap, 0, data, offset, this.rawStackMapTop);
				return offset + this.rawStackMapTop;
			}

			/// <summary>Compute a space-optimal stack map table.</summary>
			/// <remarks>Compute a space-optimal stack map table.</remarks>
			private void ComputeRawStackMap()
			{
				SuperBlock prev = this.superBlocks[0];
				int[] prevLocals = prev.GetTrimmedLocals();
				int prevOffset = -1;
				for (int i = 1; i < this.superBlocks.Length; i++)
				{
					SuperBlock current = this.superBlocks[i];
					int[] currentLocals = current.GetTrimmedLocals();
					int[] currentStack = current.GetStack();
					int offsetDelta = current.GetStart() - prevOffset - 1;
					if (currentStack.Length == 0)
					{
						int last = prevLocals.Length > currentLocals.Length ? currentLocals.Length : prevLocals.Length;
						int delta = Math.Abs(prevLocals.Length - currentLocals.Length);
						int j;
						// Compare locals until one is different or the end of a
						// local variable array is reached
						for (j = 0; j < last; j++)
						{
							if (prevLocals[j] != currentLocals[j])
							{
								break;
							}
						}
						if (j == currentLocals.Length && delta == 0)
						{
							// All of the compared locals are equal and the local
							// arrays are of equal size
							this.WriteSameFrame(currentLocals, offsetDelta);
						}
						else
						{
							if (j == currentLocals.Length && delta <= 3)
							{
								// All of the compared locals are equal and the current
								// frame has less locals than the previous frame
								this.WriteChopFrame(delta, offsetDelta);
							}
							else
							{
								if (j == prevLocals.Length && delta <= 3)
								{
									// All of the compared locals are equal and the current
									// frame has more locals than the previous frame
									this.WriteAppendFrame(currentLocals, delta, offsetDelta);
								}
								else
								{
									// Not all locals were compared were equal, so a full
									// frame is necessary
									this.WriteFullFrame(currentLocals, currentStack, offsetDelta);
								}
							}
						}
					}
					else
					{
						if (currentStack.Length == 1)
						{
							if (Arrays.Equals(prevLocals, currentLocals))
							{
								this.WriteSameLocalsOneStackItemFrame(currentLocals, currentStack, offsetDelta);
							}
							else
							{
								// Output a full frame, since no other frame types have
								// one operand stack item.
								this.WriteFullFrame(currentLocals, currentStack, offsetDelta);
							}
						}
						else
						{
							// Any stack map frame that has more than one operand stack
							// item has to be a full frame. All other frame types have
							// at most one item on the stack.
							this.WriteFullFrame(currentLocals, currentStack, offsetDelta);
						}
					}
					prev = current;
					prevLocals = currentLocals;
					prevOffset = current.GetStart();
				}
			}

			/// <summary>Get the worst case write size of the stack map table.</summary>
			/// <remarks>
			/// Get the worst case write size of the stack map table.
			/// This computes how much full frames would take, if each full frame
			/// contained the maximum number of locals and stack operands, and each
			/// verification type was 3 bytes.
			/// </remarks>
			private int GetWorstCaseWriteSize()
			{
				return (this.superBlocks.Length - 1) * (7 + this._enclosing.itsMaxLocals * 3 + this._enclosing.itsMaxStack * 3);
			}

			private void WriteSameFrame(int[] locals, int offsetDelta)
			{
				if (offsetDelta <= 63)
				{
					// Output a same_frame frame. Despite the name,
					// the operand stack may differ, but the current
					// operand stack must be empty.
					this.rawStackMap[this.rawStackMapTop++] = unchecked((byte)offsetDelta);
				}
				else
				{
					// Output a same_frame_extended frame. Similar to
					// the above, except with a larger offset delta.
					this.rawStackMap[this.rawStackMapTop++] = unchecked((byte)251);
					this.rawStackMapTop = ClassFileWriter.PutInt16(offsetDelta, this.rawStackMap, this.rawStackMapTop);
				}
			}

			private void WriteSameLocalsOneStackItemFrame(int[] locals, int[] stack, int offsetDelta)
			{
				if (offsetDelta <= 63)
				{
					// Output a same_locals_1_stack_item frame. Similar
					// to same_frame, only with one item on the operand
					// stack instead of zero.
					this.rawStackMap[this.rawStackMapTop++] = unchecked((byte)(64 + offsetDelta));
				}
				else
				{
					// Output a same_locals_1_stack_item_extended frame.
					// Similar to same_frame_extended, only with one
					// item on the operand stack instead of zero.
					this.rawStackMap[this.rawStackMapTop++] = unchecked((byte)247);
					this.rawStackMapTop = ClassFileWriter.PutInt16(offsetDelta, this.rawStackMap, this.rawStackMapTop);
				}
				this.WriteType(stack[0]);
			}

			private void WriteFullFrame(int[] locals, int[] stack, int offsetDelta)
			{
				this.rawStackMap[this.rawStackMapTop++] = unchecked((byte)255);
				this.rawStackMapTop = ClassFileWriter.PutInt16(offsetDelta, this.rawStackMap, this.rawStackMapTop);
				this.rawStackMapTop = ClassFileWriter.PutInt16(locals.Length, this.rawStackMap, this.rawStackMapTop);
				this.rawStackMapTop = this.WriteTypes(locals);
				this.rawStackMapTop = ClassFileWriter.PutInt16(stack.Length, this.rawStackMap, this.rawStackMapTop);
				this.rawStackMapTop = this.WriteTypes(stack);
			}

			private void WriteAppendFrame(int[] locals, int localsDelta, int offsetDelta)
			{
				int start = locals.Length - localsDelta;
				this.rawStackMap[this.rawStackMapTop++] = unchecked((byte)(251 + localsDelta));
				this.rawStackMapTop = ClassFileWriter.PutInt16(offsetDelta, this.rawStackMap, this.rawStackMapTop);
				this.rawStackMapTop = this.WriteTypes(locals, start);
			}

			private void WriteChopFrame(int localsDelta, int offsetDelta)
			{
				this.rawStackMap[this.rawStackMapTop++] = unchecked((byte)(251 - localsDelta));
				this.rawStackMapTop = ClassFileWriter.PutInt16(offsetDelta, this.rawStackMap, this.rawStackMapTop);
			}

			private int WriteTypes(int[] types)
			{
				return this.WriteTypes(types, 0);
			}

			private int WriteTypes(int[] types, int start)
			{
				int startOffset = this.rawStackMapTop;
				for (int i = start; i < types.Length; i++)
				{
					this.rawStackMapTop = this.WriteType(types[i]);
				}
				return this.rawStackMapTop;
			}

			private int WriteType(int type)
			{
				int tag = type & unchecked((int)(0xFF));
				this.rawStackMap[this.rawStackMapTop++] = unchecked((byte)tag);
				if (tag == TypeInfo.OBJECT_TAG || tag == TypeInfo.UNINITIALIZED_VAR_TAG)
				{
					this.rawStackMapTop = ClassFileWriter.PutInt16((int)(((uint)type) >> 8), this.rawStackMap, this.rawStackMapTop);
				}
				return this.rawStackMapTop;
			}

			private int[] locals;

			private int localsTop;

			private int[] stack;

			private int stackTop;

			private SuperBlock[] workList;

			private int workListTop;

			private SuperBlock[] superBlocks;

			private SuperBlock[] superBlockDeps;

			private byte[] rawStackMap;

			private int rawStackMapTop;

			private bool wide;

			internal const bool DEBUGSTACKMAP = false;

			private readonly ClassFileWriter _enclosing;
			// Intermediate operand stack and local variable state. During
			// execution of a block, these are initialized to copies of the initial
			// block type state and are modified by the actual stack/local
			// emulation.
		}

		/// <summary>Convert a newarray operand into an internal type.</summary>
		/// <remarks>Convert a newarray operand into an internal type.</remarks>
		private static char ArrayTypeToName(int type)
		{
			switch (type)
			{
				case ByteCode.T_BOOLEAN:
				{
					return 'Z';
				}

				case ByteCode.T_CHAR:
				{
					return 'C';
				}

				case ByteCode.T_FLOAT:
				{
					return 'F';
				}

				case ByteCode.T_DOUBLE:
				{
					return 'D';
				}

				case ByteCode.T_BYTE:
				{
					return 'B';
				}

				case ByteCode.T_SHORT:
				{
					return 'S';
				}

				case ByteCode.T_INT:
				{
					return 'I';
				}

				case ByteCode.T_LONG:
				{
					return 'J';
				}

				default:
				{
					throw new ArgumentException("bad operand");
				}
			}
		}

		/// <summary>Convert a class descriptor into an internal name.</summary>
		/// <remarks>
		/// Convert a class descriptor into an internal name.
		/// For example, descriptor Ljava/lang/Object; becomes java/lang/Object.
		/// </remarks>
		private static string ClassDescriptorToInternalName(string descriptor)
		{
			return Sharpen.Runtime.Substring(descriptor, 1, descriptor.Length - 1);
		}

		/// <summary>Convert a non-method type descriptor into an internal type.</summary>
		/// <remarks>Convert a non-method type descriptor into an internal type.</remarks>
		/// <param name="descriptor">the simple type descriptor to convert</param>
		private static string DescriptorToInternalName(string descriptor)
		{
			switch (descriptor[0])
			{
				case 'B':
				case 'C':
				case 'D':
				case 'F':
				case 'I':
				case 'J':
				case 'S':
				case 'Z':
				case 'V':
				case '[':
				{
					return descriptor;
				}

				case 'L':
				{
					return ClassDescriptorToInternalName(descriptor);
				}

				default:
				{
					throw new ArgumentException("bad descriptor:" + descriptor);
				}
			}
		}

		/// <summary>Compute the initial local variable array for the current method.</summary>
		/// <remarks>
		/// Compute the initial local variable array for the current method.
		/// Creates an array of the size of the method's max locals, regardless of
		/// the number of parameters in the method.
		/// </remarks>
		private int[] CreateInitialLocals()
		{
			int[] initialLocals = new int[itsMaxLocals];
			int localsTop = 0;
			// Instance methods require the first local variable in the array
			// to be "this". However, if the method being created is a
			// constructor, aka the method is <init>, then the type of "this"
			// should be StackMapTable.UNINITIALIZED_THIS
			if ((itsCurrentMethod.GetFlags() & ACC_STATIC) == 0)
			{
				if ("<init>".Equals(itsCurrentMethod.GetName()))
				{
					initialLocals[localsTop++] = TypeInfo.UNINITIALIZED_THIS;
				}
				else
				{
					initialLocals[localsTop++] = TypeInfo.OBJECT(itsThisClassIndex);
				}
			}
			// No error checking should be necessary, sizeOfParameters does this
			string type = itsCurrentMethod.GetType();
			int lParenIndex = type.IndexOf('(');
			int rParenIndex = type.IndexOf(')');
			if (lParenIndex != 0 || rParenIndex < 0)
			{
				throw new ArgumentException("bad method type");
			}
			int start = lParenIndex + 1;
			StringBuilder paramType = new StringBuilder();
			while (start < rParenIndex)
			{
				switch (type[start])
				{
					case 'B':
					case 'C':
					case 'D':
					case 'F':
					case 'I':
					case 'J':
					case 'S':
					case 'Z':
					{
						paramType.Append(type[start]);
						++start;
						break;
					}

					case 'L':
					{
						int end = type.IndexOf(';', start) + 1;
						string name = Sharpen.Runtime.Substring(type, start, end);
						paramType.Append(name);
						start = end;
						break;
					}

					case '[':
					{
						paramType.Append('[');
						++start;
						continue;
					}
				}
				string internalType = DescriptorToInternalName(paramType.ToString());
				int typeInfo = TypeInfo.FromType(internalType, itsConstantPool);
				initialLocals[localsTop++] = typeInfo;
				if (TypeInfo.IsTwoWords(typeInfo))
				{
					localsTop++;
				}
				paramType.Length = 0;
			}
			return initialLocals;
		}

		/// <summary>Write the class file to the OutputStream.</summary>
		/// <remarks>Write the class file to the OutputStream.</remarks>
		/// <param name="oStream">the stream to write to</param>
		/// <exception cref="System.IO.IOException">if writing to the stream produces an exception</exception>
		public virtual void Write(Stream oStream)
		{
			byte[] array = ToByteArray();
			oStream.Write(array);
		}

		private int GetWriteSize()
		{
			int size = 0;
			if (itsSourceFileNameIndex != 0)
			{
				itsConstantPool.AddUtf8("SourceFile");
			}
			size += 8;
			//writeLong(FileHeaderConstant);
			size += itsConstantPool.GetWriteSize();
			size += 2;
			//writeShort(itsFlags);
			size += 2;
			//writeShort(itsThisClassIndex);
			size += 2;
			//writeShort(itsSuperClassIndex);
			size += 2;
			//writeShort(itsInterfaces.size());
			size += 2 * itsInterfaces.Size();
			size += 2;
			//writeShort(itsFields.size());
			for (int i = 0; i < itsFields.Size(); i++)
			{
				size += ((ClassFileField)(itsFields.Get(i))).GetWriteSize();
			}
			size += 2;
			//writeShort(itsMethods.size());
			for (int i_1 = 0; i_1 < itsMethods.Size(); i_1++)
			{
				size += ((ClassFileMethod)(itsMethods.Get(i_1))).GetWriteSize();
			}
			if (itsSourceFileNameIndex != 0)
			{
				size += 2;
				//writeShort(1);  attributes count
				size += 2;
				//writeShort(sourceFileAttributeNameIndex);
				size += 4;
				//writeInt(2);
				size += 2;
			}
			else
			{
				//writeShort(itsSourceFileNameIndex);
				size += 2;
			}
			//out.writeShort(0);  no attributes
			return size;
		}

		/// <summary>Get the class file as array of bytesto the OutputStream.</summary>
		/// <remarks>Get the class file as array of bytesto the OutputStream.</remarks>
		public virtual byte[] ToByteArray()
		{
			int dataSize = GetWriteSize();
			byte[] data = new byte[dataSize];
			int offset = 0;
			short sourceFileAttributeNameIndex = 0;
			if (itsSourceFileNameIndex != 0)
			{
				sourceFileAttributeNameIndex = itsConstantPool.AddUtf8("SourceFile");
			}
			offset = PutInt32(FileHeaderConstant, data, offset);
			offset = PutInt16(MinorVersion, data, offset);
			offset = PutInt16(MajorVersion, data, offset);
			offset = itsConstantPool.Write(data, offset);
			offset = PutInt16(itsFlags, data, offset);
			offset = PutInt16(itsThisClassIndex, data, offset);
			offset = PutInt16(itsSuperClassIndex, data, offset);
			offset = PutInt16(itsInterfaces.Size(), data, offset);
			for (int i = 0; i < itsInterfaces.Size(); i++)
			{
				int interfaceIndex = System.Convert.ToInt16(((short)(itsInterfaces.Get(i))));
				offset = PutInt16(interfaceIndex, data, offset);
			}
			offset = PutInt16(itsFields.Size(), data, offset);
			for (int i_1 = 0; i_1 < itsFields.Size(); i_1++)
			{
				ClassFileField field = (ClassFileField)itsFields.Get(i_1);
				offset = field.Write(data, offset);
			}
			offset = PutInt16(itsMethods.Size(), data, offset);
			for (int i_2 = 0; i_2 < itsMethods.Size(); i_2++)
			{
				ClassFileMethod method = (ClassFileMethod)itsMethods.Get(i_2);
				offset = method.Write(data, offset);
			}
			if (itsSourceFileNameIndex != 0)
			{
				offset = PutInt16(1, data, offset);
				// attributes count
				offset = PutInt16(sourceFileAttributeNameIndex, data, offset);
				offset = PutInt32(2, data, offset);
				offset = PutInt16(itsSourceFileNameIndex, data, offset);
			}
			else
			{
				offset = PutInt16(0, data, offset);
			}
			// no attributes
			if (offset != dataSize)
			{
				// Check getWriteSize is consistent with write!
				throw new Exception();
			}
			return data;
		}

		internal static int PutInt64(long value, byte[] array, int offset)
		{
			offset = PutInt32((int)((long)(((ulong)value) >> 32)), array, offset);
			return PutInt32((int)value, array, offset);
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

		private static int SizeOfParameters(string pString)
		{
			int length = pString.Length;
			int rightParenthesis = pString.LastIndexOf(')');
			if (3 <= length && pString[0] == '(' && 1 <= rightParenthesis && rightParenthesis + 1 < length)
			{
				bool ok = true;
				int index = 1;
				int stackDiff = 0;
				int count = 0;
				while (index != rightParenthesis)
				{
					switch (pString[index])
					{
						default:
						{
							ok = false;
							goto stringLoop_break;
						}

						case 'J':
						case 'D':
						{
							--stackDiff;
							goto case 'B';
						}

						case 'B':
						case 'S':
						case 'C':
						case 'I':
						case 'Z':
						case 'F':
						{
							// fall thru
							--stackDiff;
							++count;
							++index;
							continue;
						}

						case '[':
						{
							++index;
							int c = pString[index];
							while (c == '[')
							{
								++index;
								c = pString[index];
							}
							switch (c)
							{
								default:
								{
									ok = false;
									goto stringLoop_break;
								}

								case 'J':
								case 'D':
								case 'B':
								case 'S':
								case 'C':
								case 'I':
								case 'Z':
								case 'F':
								{
									--stackDiff;
									++count;
									++index;
									continue;
								}

								case 'L':
								{
									break;
								}
							}
							goto case 'L';
						}

						case 'L':
						{
							// fall thru
							// fall thru
							--stackDiff;
							++count;
							++index;
							int semicolon = pString.IndexOf(';', index);
							if (!(index + 1 <= semicolon && semicolon < rightParenthesis))
							{
								ok = false;
								goto stringLoop_break;
							}
							index = semicolon + 1;
							continue;
						}
					}
stringLoop_continue: ;
				}
stringLoop_break: ;
				if (ok)
				{
					switch (pString[rightParenthesis + 1])
					{
						default:
						{
							ok = false;
							break;
						}

						case 'J':
						case 'D':
						{
							++stackDiff;
							goto case 'B';
						}

						case 'B':
						case 'S':
						case 'C':
						case 'I':
						case 'Z':
						case 'F':
						case 'L':
						case '[':
						{
							// fall thru
							++stackDiff;
							goto case 'V';
						}

						case 'V':
						{
							// fall thru
							break;
						}
					}
					if (ok)
					{
						return ((count << 16) | (unchecked((int)(0xFFFF)) & stackDiff));
					}
				}
			}
			throw new ArgumentException("Bad parameter signature: " + pString);
		}

		internal static int PutInt16(int value, byte[] array, int offset)
		{
			array[offset + 0] = unchecked((byte)((int)(((uint)value) >> 8)));
			array[offset + 1] = unchecked((byte)value);
			return offset + 2;
		}

		internal static int PutInt32(int value, byte[] array, int offset)
		{
			array[offset + 0] = unchecked((byte)((int)(((uint)value) >> 24)));
			array[offset + 1] = unchecked((byte)((int)(((uint)value) >> 16)));
			array[offset + 2] = unchecked((byte)((int)(((uint)value) >> 8)));
			array[offset + 3] = unchecked((byte)value);
			return offset + 4;
		}

		/// <summary>Size of a bytecode instruction, counting the opcode and its operands.</summary>
		/// <remarks>
		/// Size of a bytecode instruction, counting the opcode and its operands.
		/// This is different from opcodeCount, since opcodeCount counts logical
		/// operands.
		/// </remarks>
		internal static int OpcodeLength(int opcode, bool wide)
		{
			switch (opcode)
			{
				case ByteCode.AALOAD:
				case ByteCode.AASTORE:
				case ByteCode.ACONST_NULL:
				case ByteCode.ALOAD_0:
				case ByteCode.ALOAD_1:
				case ByteCode.ALOAD_2:
				case ByteCode.ALOAD_3:
				case ByteCode.ARETURN:
				case ByteCode.ARRAYLENGTH:
				case ByteCode.ASTORE_0:
				case ByteCode.ASTORE_1:
				case ByteCode.ASTORE_2:
				case ByteCode.ASTORE_3:
				case ByteCode.ATHROW:
				case ByteCode.BALOAD:
				case ByteCode.BASTORE:
				case ByteCode.BREAKPOINT:
				case ByteCode.CALOAD:
				case ByteCode.CASTORE:
				case ByteCode.D2F:
				case ByteCode.D2I:
				case ByteCode.D2L:
				case ByteCode.DADD:
				case ByteCode.DALOAD:
				case ByteCode.DASTORE:
				case ByteCode.DCMPG:
				case ByteCode.DCMPL:
				case ByteCode.DCONST_0:
				case ByteCode.DCONST_1:
				case ByteCode.DDIV:
				case ByteCode.DLOAD_0:
				case ByteCode.DLOAD_1:
				case ByteCode.DLOAD_2:
				case ByteCode.DLOAD_3:
				case ByteCode.DMUL:
				case ByteCode.DNEG:
				case ByteCode.DREM:
				case ByteCode.DRETURN:
				case ByteCode.DSTORE_0:
				case ByteCode.DSTORE_1:
				case ByteCode.DSTORE_2:
				case ByteCode.DSTORE_3:
				case ByteCode.DSUB:
				case ByteCode.DUP:
				case ByteCode.DUP2:
				case ByteCode.DUP2_X1:
				case ByteCode.DUP2_X2:
				case ByteCode.DUP_X1:
				case ByteCode.DUP_X2:
				case ByteCode.F2D:
				case ByteCode.F2I:
				case ByteCode.F2L:
				case ByteCode.FADD:
				case ByteCode.FALOAD:
				case ByteCode.FASTORE:
				case ByteCode.FCMPG:
				case ByteCode.FCMPL:
				case ByteCode.FCONST_0:
				case ByteCode.FCONST_1:
				case ByteCode.FCONST_2:
				case ByteCode.FDIV:
				case ByteCode.FLOAD_0:
				case ByteCode.FLOAD_1:
				case ByteCode.FLOAD_2:
				case ByteCode.FLOAD_3:
				case ByteCode.FMUL:
				case ByteCode.FNEG:
				case ByteCode.FREM:
				case ByteCode.FRETURN:
				case ByteCode.FSTORE_0:
				case ByteCode.FSTORE_1:
				case ByteCode.FSTORE_2:
				case ByteCode.FSTORE_3:
				case ByteCode.FSUB:
				case ByteCode.I2B:
				case ByteCode.I2C:
				case ByteCode.I2D:
				case ByteCode.I2F:
				case ByteCode.I2L:
				case ByteCode.I2S:
				case ByteCode.IADD:
				case ByteCode.IALOAD:
				case ByteCode.IAND:
				case ByteCode.IASTORE:
				case ByteCode.ICONST_0:
				case ByteCode.ICONST_1:
				case ByteCode.ICONST_2:
				case ByteCode.ICONST_3:
				case ByteCode.ICONST_4:
				case ByteCode.ICONST_5:
				case ByteCode.ICONST_M1:
				case ByteCode.IDIV:
				case ByteCode.ILOAD_0:
				case ByteCode.ILOAD_1:
				case ByteCode.ILOAD_2:
				case ByteCode.ILOAD_3:
				case ByteCode.IMPDEP1:
				case ByteCode.IMPDEP2:
				case ByteCode.IMUL:
				case ByteCode.INEG:
				case ByteCode.IOR:
				case ByteCode.IREM:
				case ByteCode.IRETURN:
				case ByteCode.ISHL:
				case ByteCode.ISHR:
				case ByteCode.ISTORE_0:
				case ByteCode.ISTORE_1:
				case ByteCode.ISTORE_2:
				case ByteCode.ISTORE_3:
				case ByteCode.ISUB:
				case ByteCode.IUSHR:
				case ByteCode.IXOR:
				case ByteCode.L2D:
				case ByteCode.L2F:
				case ByteCode.L2I:
				case ByteCode.LADD:
				case ByteCode.LALOAD:
				case ByteCode.LAND:
				case ByteCode.LASTORE:
				case ByteCode.LCMP:
				case ByteCode.LCONST_0:
				case ByteCode.LCONST_1:
				case ByteCode.LDIV:
				case ByteCode.LLOAD_0:
				case ByteCode.LLOAD_1:
				case ByteCode.LLOAD_2:
				case ByteCode.LLOAD_3:
				case ByteCode.LMUL:
				case ByteCode.LNEG:
				case ByteCode.LOR:
				case ByteCode.LREM:
				case ByteCode.LRETURN:
				case ByteCode.LSHL:
				case ByteCode.LSHR:
				case ByteCode.LSTORE_0:
				case ByteCode.LSTORE_1:
				case ByteCode.LSTORE_2:
				case ByteCode.LSTORE_3:
				case ByteCode.LSUB:
				case ByteCode.LUSHR:
				case ByteCode.LXOR:
				case ByteCode.MONITORENTER:
				case ByteCode.MONITOREXIT:
				case ByteCode.NOP:
				case ByteCode.POP:
				case ByteCode.POP2:
				case ByteCode.RETURN:
				case ByteCode.SALOAD:
				case ByteCode.SASTORE:
				case ByteCode.SWAP:
				case ByteCode.WIDE:
				{
					return 1;
				}

				case ByteCode.BIPUSH:
				case ByteCode.LDC:
				case ByteCode.NEWARRAY:
				{
					return 2;
				}

				case ByteCode.ALOAD:
				case ByteCode.ASTORE:
				case ByteCode.DLOAD:
				case ByteCode.DSTORE:
				case ByteCode.FLOAD:
				case ByteCode.FSTORE:
				case ByteCode.ILOAD:
				case ByteCode.ISTORE:
				case ByteCode.LLOAD:
				case ByteCode.LSTORE:
				case ByteCode.RET:
				{
					return wide ? 3 : 2;
				}

				case ByteCode.ANEWARRAY:
				case ByteCode.CHECKCAST:
				case ByteCode.GETFIELD:
				case ByteCode.GETSTATIC:
				case ByteCode.GOTO:
				case ByteCode.IFEQ:
				case ByteCode.IFGE:
				case ByteCode.IFGT:
				case ByteCode.IFLE:
				case ByteCode.IFLT:
				case ByteCode.IFNE:
				case ByteCode.IFNONNULL:
				case ByteCode.IFNULL:
				case ByteCode.IF_ACMPEQ:
				case ByteCode.IF_ACMPNE:
				case ByteCode.IF_ICMPEQ:
				case ByteCode.IF_ICMPGE:
				case ByteCode.IF_ICMPGT:
				case ByteCode.IF_ICMPLE:
				case ByteCode.IF_ICMPLT:
				case ByteCode.IF_ICMPNE:
				case ByteCode.INSTANCEOF:
				case ByteCode.INVOKESPECIAL:
				case ByteCode.INVOKESTATIC:
				case ByteCode.INVOKEVIRTUAL:
				case ByteCode.JSR:
				case ByteCode.LDC_W:
				case ByteCode.LDC2_W:
				case ByteCode.NEW:
				case ByteCode.PUTFIELD:
				case ByteCode.PUTSTATIC:
				case ByteCode.SIPUSH:
				{
					return 3;
				}

				case ByteCode.IINC:
				{
					return wide ? 5 : 3;
				}

				case ByteCode.MULTIANEWARRAY:
				{
					return 4;
				}

				case ByteCode.GOTO_W:
				case ByteCode.INVOKEINTERFACE:
				case ByteCode.JSR_W:
				{
					return 5;
				}
			}
			throw new ArgumentException("Bad opcode: " + opcode);
		}

		/// <summary>Number of operands accompanying the opcode.</summary>
		/// <remarks>Number of operands accompanying the opcode.</remarks>
		internal static int OpcodeCount(int opcode)
		{
			switch (opcode)
			{
				case ByteCode.AALOAD:
				case ByteCode.AASTORE:
				case ByteCode.ACONST_NULL:
				case ByteCode.ALOAD_0:
				case ByteCode.ALOAD_1:
				case ByteCode.ALOAD_2:
				case ByteCode.ALOAD_3:
				case ByteCode.ARETURN:
				case ByteCode.ARRAYLENGTH:
				case ByteCode.ASTORE_0:
				case ByteCode.ASTORE_1:
				case ByteCode.ASTORE_2:
				case ByteCode.ASTORE_3:
				case ByteCode.ATHROW:
				case ByteCode.BALOAD:
				case ByteCode.BASTORE:
				case ByteCode.BREAKPOINT:
				case ByteCode.CALOAD:
				case ByteCode.CASTORE:
				case ByteCode.D2F:
				case ByteCode.D2I:
				case ByteCode.D2L:
				case ByteCode.DADD:
				case ByteCode.DALOAD:
				case ByteCode.DASTORE:
				case ByteCode.DCMPG:
				case ByteCode.DCMPL:
				case ByteCode.DCONST_0:
				case ByteCode.DCONST_1:
				case ByteCode.DDIV:
				case ByteCode.DLOAD_0:
				case ByteCode.DLOAD_1:
				case ByteCode.DLOAD_2:
				case ByteCode.DLOAD_3:
				case ByteCode.DMUL:
				case ByteCode.DNEG:
				case ByteCode.DREM:
				case ByteCode.DRETURN:
				case ByteCode.DSTORE_0:
				case ByteCode.DSTORE_1:
				case ByteCode.DSTORE_2:
				case ByteCode.DSTORE_3:
				case ByteCode.DSUB:
				case ByteCode.DUP:
				case ByteCode.DUP2:
				case ByteCode.DUP2_X1:
				case ByteCode.DUP2_X2:
				case ByteCode.DUP_X1:
				case ByteCode.DUP_X2:
				case ByteCode.F2D:
				case ByteCode.F2I:
				case ByteCode.F2L:
				case ByteCode.FADD:
				case ByteCode.FALOAD:
				case ByteCode.FASTORE:
				case ByteCode.FCMPG:
				case ByteCode.FCMPL:
				case ByteCode.FCONST_0:
				case ByteCode.FCONST_1:
				case ByteCode.FCONST_2:
				case ByteCode.FDIV:
				case ByteCode.FLOAD_0:
				case ByteCode.FLOAD_1:
				case ByteCode.FLOAD_2:
				case ByteCode.FLOAD_3:
				case ByteCode.FMUL:
				case ByteCode.FNEG:
				case ByteCode.FREM:
				case ByteCode.FRETURN:
				case ByteCode.FSTORE_0:
				case ByteCode.FSTORE_1:
				case ByteCode.FSTORE_2:
				case ByteCode.FSTORE_3:
				case ByteCode.FSUB:
				case ByteCode.I2B:
				case ByteCode.I2C:
				case ByteCode.I2D:
				case ByteCode.I2F:
				case ByteCode.I2L:
				case ByteCode.I2S:
				case ByteCode.IADD:
				case ByteCode.IALOAD:
				case ByteCode.IAND:
				case ByteCode.IASTORE:
				case ByteCode.ICONST_0:
				case ByteCode.ICONST_1:
				case ByteCode.ICONST_2:
				case ByteCode.ICONST_3:
				case ByteCode.ICONST_4:
				case ByteCode.ICONST_5:
				case ByteCode.ICONST_M1:
				case ByteCode.IDIV:
				case ByteCode.ILOAD_0:
				case ByteCode.ILOAD_1:
				case ByteCode.ILOAD_2:
				case ByteCode.ILOAD_3:
				case ByteCode.IMPDEP1:
				case ByteCode.IMPDEP2:
				case ByteCode.IMUL:
				case ByteCode.INEG:
				case ByteCode.IOR:
				case ByteCode.IREM:
				case ByteCode.IRETURN:
				case ByteCode.ISHL:
				case ByteCode.ISHR:
				case ByteCode.ISTORE_0:
				case ByteCode.ISTORE_1:
				case ByteCode.ISTORE_2:
				case ByteCode.ISTORE_3:
				case ByteCode.ISUB:
				case ByteCode.IUSHR:
				case ByteCode.IXOR:
				case ByteCode.L2D:
				case ByteCode.L2F:
				case ByteCode.L2I:
				case ByteCode.LADD:
				case ByteCode.LALOAD:
				case ByteCode.LAND:
				case ByteCode.LASTORE:
				case ByteCode.LCMP:
				case ByteCode.LCONST_0:
				case ByteCode.LCONST_1:
				case ByteCode.LDIV:
				case ByteCode.LLOAD_0:
				case ByteCode.LLOAD_1:
				case ByteCode.LLOAD_2:
				case ByteCode.LLOAD_3:
				case ByteCode.LMUL:
				case ByteCode.LNEG:
				case ByteCode.LOR:
				case ByteCode.LREM:
				case ByteCode.LRETURN:
				case ByteCode.LSHL:
				case ByteCode.LSHR:
				case ByteCode.LSTORE_0:
				case ByteCode.LSTORE_1:
				case ByteCode.LSTORE_2:
				case ByteCode.LSTORE_3:
				case ByteCode.LSUB:
				case ByteCode.LUSHR:
				case ByteCode.LXOR:
				case ByteCode.MONITORENTER:
				case ByteCode.MONITOREXIT:
				case ByteCode.NOP:
				case ByteCode.POP:
				case ByteCode.POP2:
				case ByteCode.RETURN:
				case ByteCode.SALOAD:
				case ByteCode.SASTORE:
				case ByteCode.SWAP:
				case ByteCode.WIDE:
				{
					return 0;
				}

				case ByteCode.ALOAD:
				case ByteCode.ANEWARRAY:
				case ByteCode.ASTORE:
				case ByteCode.BIPUSH:
				case ByteCode.CHECKCAST:
				case ByteCode.DLOAD:
				case ByteCode.DSTORE:
				case ByteCode.FLOAD:
				case ByteCode.FSTORE:
				case ByteCode.GETFIELD:
				case ByteCode.GETSTATIC:
				case ByteCode.GOTO:
				case ByteCode.GOTO_W:
				case ByteCode.IFEQ:
				case ByteCode.IFGE:
				case ByteCode.IFGT:
				case ByteCode.IFLE:
				case ByteCode.IFLT:
				case ByteCode.IFNE:
				case ByteCode.IFNONNULL:
				case ByteCode.IFNULL:
				case ByteCode.IF_ACMPEQ:
				case ByteCode.IF_ACMPNE:
				case ByteCode.IF_ICMPEQ:
				case ByteCode.IF_ICMPGE:
				case ByteCode.IF_ICMPGT:
				case ByteCode.IF_ICMPLE:
				case ByteCode.IF_ICMPLT:
				case ByteCode.IF_ICMPNE:
				case ByteCode.ILOAD:
				case ByteCode.INSTANCEOF:
				case ByteCode.INVOKEINTERFACE:
				case ByteCode.INVOKESPECIAL:
				case ByteCode.INVOKESTATIC:
				case ByteCode.INVOKEVIRTUAL:
				case ByteCode.ISTORE:
				case ByteCode.JSR:
				case ByteCode.JSR_W:
				case ByteCode.LDC:
				case ByteCode.LDC2_W:
				case ByteCode.LDC_W:
				case ByteCode.LLOAD:
				case ByteCode.LSTORE:
				case ByteCode.NEW:
				case ByteCode.NEWARRAY:
				case ByteCode.PUTFIELD:
				case ByteCode.PUTSTATIC:
				case ByteCode.RET:
				case ByteCode.SIPUSH:
				{
					return 1;
				}

				case ByteCode.IINC:
				case ByteCode.MULTIANEWARRAY:
				{
					return 2;
				}

				case ByteCode.LOOKUPSWITCH:
				case ByteCode.TABLESWITCH:
				{
					return -1;
				}
			}
			throw new ArgumentException("Bad opcode: " + opcode);
		}

		/// <summary>The effect on the operand stack of a given opcode.</summary>
		/// <remarks>The effect on the operand stack of a given opcode.</remarks>
		internal static int StackChange(int opcode)
		{
			switch (opcode)
			{
				case ByteCode.DASTORE:
				case ByteCode.LASTORE:
				{
					// For INVOKE... accounts only for popping this (unless static),
					// ignoring parameters and return type
					return -4;
				}

				case ByteCode.AASTORE:
				case ByteCode.BASTORE:
				case ByteCode.CASTORE:
				case ByteCode.DCMPG:
				case ByteCode.DCMPL:
				case ByteCode.FASTORE:
				case ByteCode.IASTORE:
				case ByteCode.LCMP:
				case ByteCode.SASTORE:
				{
					return -3;
				}

				case ByteCode.DADD:
				case ByteCode.DDIV:
				case ByteCode.DMUL:
				case ByteCode.DREM:
				case ByteCode.DRETURN:
				case ByteCode.DSTORE:
				case ByteCode.DSTORE_0:
				case ByteCode.DSTORE_1:
				case ByteCode.DSTORE_2:
				case ByteCode.DSTORE_3:
				case ByteCode.DSUB:
				case ByteCode.IF_ACMPEQ:
				case ByteCode.IF_ACMPNE:
				case ByteCode.IF_ICMPEQ:
				case ByteCode.IF_ICMPGE:
				case ByteCode.IF_ICMPGT:
				case ByteCode.IF_ICMPLE:
				case ByteCode.IF_ICMPLT:
				case ByteCode.IF_ICMPNE:
				case ByteCode.LADD:
				case ByteCode.LAND:
				case ByteCode.LDIV:
				case ByteCode.LMUL:
				case ByteCode.LOR:
				case ByteCode.LREM:
				case ByteCode.LRETURN:
				case ByteCode.LSTORE:
				case ByteCode.LSTORE_0:
				case ByteCode.LSTORE_1:
				case ByteCode.LSTORE_2:
				case ByteCode.LSTORE_3:
				case ByteCode.LSUB:
				case ByteCode.LXOR:
				case ByteCode.POP2:
				{
					return -2;
				}

				case ByteCode.AALOAD:
				case ByteCode.ARETURN:
				case ByteCode.ASTORE:
				case ByteCode.ASTORE_0:
				case ByteCode.ASTORE_1:
				case ByteCode.ASTORE_2:
				case ByteCode.ASTORE_3:
				case ByteCode.ATHROW:
				case ByteCode.BALOAD:
				case ByteCode.CALOAD:
				case ByteCode.D2F:
				case ByteCode.D2I:
				case ByteCode.FADD:
				case ByteCode.FALOAD:
				case ByteCode.FCMPG:
				case ByteCode.FCMPL:
				case ByteCode.FDIV:
				case ByteCode.FMUL:
				case ByteCode.FREM:
				case ByteCode.FRETURN:
				case ByteCode.FSTORE:
				case ByteCode.FSTORE_0:
				case ByteCode.FSTORE_1:
				case ByteCode.FSTORE_2:
				case ByteCode.FSTORE_3:
				case ByteCode.FSUB:
				case ByteCode.GETFIELD:
				case ByteCode.IADD:
				case ByteCode.IALOAD:
				case ByteCode.IAND:
				case ByteCode.IDIV:
				case ByteCode.IFEQ:
				case ByteCode.IFGE:
				case ByteCode.IFGT:
				case ByteCode.IFLE:
				case ByteCode.IFLT:
				case ByteCode.IFNE:
				case ByteCode.IFNONNULL:
				case ByteCode.IFNULL:
				case ByteCode.IMUL:
				case ByteCode.INVOKEINTERFACE:
				case ByteCode.INVOKESPECIAL:
				case ByteCode.INVOKEVIRTUAL:
				case ByteCode.IOR:
				case ByteCode.IREM:
				case ByteCode.IRETURN:
				case ByteCode.ISHL:
				case ByteCode.ISHR:
				case ByteCode.ISTORE:
				case ByteCode.ISTORE_0:
				case ByteCode.ISTORE_1:
				case ByteCode.ISTORE_2:
				case ByteCode.ISTORE_3:
				case ByteCode.ISUB:
				case ByteCode.IUSHR:
				case ByteCode.IXOR:
				case ByteCode.L2F:
				case ByteCode.L2I:
				case ByteCode.LOOKUPSWITCH:
				case ByteCode.LSHL:
				case ByteCode.LSHR:
				case ByteCode.LUSHR:
				case ByteCode.MONITORENTER:
				case ByteCode.MONITOREXIT:
				case ByteCode.POP:
				case ByteCode.PUTFIELD:
				case ByteCode.SALOAD:
				case ByteCode.TABLESWITCH:
				{
					//
					// but needs to account for
					// pops 'this' (unless static)
					return -1;
				}

				case ByteCode.ANEWARRAY:
				case ByteCode.ARRAYLENGTH:
				case ByteCode.BREAKPOINT:
				case ByteCode.CHECKCAST:
				case ByteCode.D2L:
				case ByteCode.DALOAD:
				case ByteCode.DNEG:
				case ByteCode.F2I:
				case ByteCode.FNEG:
				case ByteCode.GETSTATIC:
				case ByteCode.GOTO:
				case ByteCode.GOTO_W:
				case ByteCode.I2B:
				case ByteCode.I2C:
				case ByteCode.I2F:
				case ByteCode.I2S:
				case ByteCode.IINC:
				case ByteCode.IMPDEP1:
				case ByteCode.IMPDEP2:
				case ByteCode.INEG:
				case ByteCode.INSTANCEOF:
				case ByteCode.INVOKESTATIC:
				case ByteCode.L2D:
				case ByteCode.LALOAD:
				case ByteCode.LNEG:
				case ByteCode.NEWARRAY:
				case ByteCode.NOP:
				case ByteCode.PUTSTATIC:
				case ByteCode.RET:
				case ByteCode.RETURN:
				case ByteCode.SWAP:
				case ByteCode.WIDE:
				{
					return 0;
				}

				case ByteCode.ACONST_NULL:
				case ByteCode.ALOAD:
				case ByteCode.ALOAD_0:
				case ByteCode.ALOAD_1:
				case ByteCode.ALOAD_2:
				case ByteCode.ALOAD_3:
				case ByteCode.BIPUSH:
				case ByteCode.DUP:
				case ByteCode.DUP_X1:
				case ByteCode.DUP_X2:
				case ByteCode.F2D:
				case ByteCode.F2L:
				case ByteCode.FCONST_0:
				case ByteCode.FCONST_1:
				case ByteCode.FCONST_2:
				case ByteCode.FLOAD:
				case ByteCode.FLOAD_0:
				case ByteCode.FLOAD_1:
				case ByteCode.FLOAD_2:
				case ByteCode.FLOAD_3:
				case ByteCode.I2D:
				case ByteCode.I2L:
				case ByteCode.ICONST_0:
				case ByteCode.ICONST_1:
				case ByteCode.ICONST_2:
				case ByteCode.ICONST_3:
				case ByteCode.ICONST_4:
				case ByteCode.ICONST_5:
				case ByteCode.ICONST_M1:
				case ByteCode.ILOAD:
				case ByteCode.ILOAD_0:
				case ByteCode.ILOAD_1:
				case ByteCode.ILOAD_2:
				case ByteCode.ILOAD_3:
				case ByteCode.JSR:
				case ByteCode.JSR_W:
				case ByteCode.LDC:
				case ByteCode.LDC_W:
				case ByteCode.MULTIANEWARRAY:
				case ByteCode.NEW:
				case ByteCode.SIPUSH:
				{
					return 1;
				}

				case ByteCode.DCONST_0:
				case ByteCode.DCONST_1:
				case ByteCode.DLOAD:
				case ByteCode.DLOAD_0:
				case ByteCode.DLOAD_1:
				case ByteCode.DLOAD_2:
				case ByteCode.DLOAD_3:
				case ByteCode.DUP2:
				case ByteCode.DUP2_X1:
				case ByteCode.DUP2_X2:
				case ByteCode.LCONST_0:
				case ByteCode.LCONST_1:
				case ByteCode.LDC2_W:
				case ByteCode.LLOAD:
				case ByteCode.LLOAD_0:
				case ByteCode.LLOAD_1:
				case ByteCode.LLOAD_2:
				case ByteCode.LLOAD_3:
				{
					return 2;
				}
			}
			throw new ArgumentException("Bad opcode: " + opcode);
		}

		private static string BytecodeStr(int code)
		{
			if (DEBUGSTACK || DEBUGCODE)
			{
				switch (code)
				{
					case ByteCode.NOP:
					{
						return "nop";
					}

					case ByteCode.ACONST_NULL:
					{
						return "aconst_null";
					}

					case ByteCode.ICONST_M1:
					{
						return "iconst_m1";
					}

					case ByteCode.ICONST_0:
					{
						return "iconst_0";
					}

					case ByteCode.ICONST_1:
					{
						return "iconst_1";
					}

					case ByteCode.ICONST_2:
					{
						return "iconst_2";
					}

					case ByteCode.ICONST_3:
					{
						return "iconst_3";
					}

					case ByteCode.ICONST_4:
					{
						return "iconst_4";
					}

					case ByteCode.ICONST_5:
					{
						return "iconst_5";
					}

					case ByteCode.LCONST_0:
					{
						return "lconst_0";
					}

					case ByteCode.LCONST_1:
					{
						return "lconst_1";
					}

					case ByteCode.FCONST_0:
					{
						return "fconst_0";
					}

					case ByteCode.FCONST_1:
					{
						return "fconst_1";
					}

					case ByteCode.FCONST_2:
					{
						return "fconst_2";
					}

					case ByteCode.DCONST_0:
					{
						return "dconst_0";
					}

					case ByteCode.DCONST_1:
					{
						return "dconst_1";
					}

					case ByteCode.BIPUSH:
					{
						return "bipush";
					}

					case ByteCode.SIPUSH:
					{
						return "sipush";
					}

					case ByteCode.LDC:
					{
						return "ldc";
					}

					case ByteCode.LDC_W:
					{
						return "ldc_w";
					}

					case ByteCode.LDC2_W:
					{
						return "ldc2_w";
					}

					case ByteCode.ILOAD:
					{
						return "iload";
					}

					case ByteCode.LLOAD:
					{
						return "lload";
					}

					case ByteCode.FLOAD:
					{
						return "fload";
					}

					case ByteCode.DLOAD:
					{
						return "dload";
					}

					case ByteCode.ALOAD:
					{
						return "aload";
					}

					case ByteCode.ILOAD_0:
					{
						return "iload_0";
					}

					case ByteCode.ILOAD_1:
					{
						return "iload_1";
					}

					case ByteCode.ILOAD_2:
					{
						return "iload_2";
					}

					case ByteCode.ILOAD_3:
					{
						return "iload_3";
					}

					case ByteCode.LLOAD_0:
					{
						return "lload_0";
					}

					case ByteCode.LLOAD_1:
					{
						return "lload_1";
					}

					case ByteCode.LLOAD_2:
					{
						return "lload_2";
					}

					case ByteCode.LLOAD_3:
					{
						return "lload_3";
					}

					case ByteCode.FLOAD_0:
					{
						return "fload_0";
					}

					case ByteCode.FLOAD_1:
					{
						return "fload_1";
					}

					case ByteCode.FLOAD_2:
					{
						return "fload_2";
					}

					case ByteCode.FLOAD_3:
					{
						return "fload_3";
					}

					case ByteCode.DLOAD_0:
					{
						return "dload_0";
					}

					case ByteCode.DLOAD_1:
					{
						return "dload_1";
					}

					case ByteCode.DLOAD_2:
					{
						return "dload_2";
					}

					case ByteCode.DLOAD_3:
					{
						return "dload_3";
					}

					case ByteCode.ALOAD_0:
					{
						return "aload_0";
					}

					case ByteCode.ALOAD_1:
					{
						return "aload_1";
					}

					case ByteCode.ALOAD_2:
					{
						return "aload_2";
					}

					case ByteCode.ALOAD_3:
					{
						return "aload_3";
					}

					case ByteCode.IALOAD:
					{
						return "iaload";
					}

					case ByteCode.LALOAD:
					{
						return "laload";
					}

					case ByteCode.FALOAD:
					{
						return "faload";
					}

					case ByteCode.DALOAD:
					{
						return "daload";
					}

					case ByteCode.AALOAD:
					{
						return "aaload";
					}

					case ByteCode.BALOAD:
					{
						return "baload";
					}

					case ByteCode.CALOAD:
					{
						return "caload";
					}

					case ByteCode.SALOAD:
					{
						return "saload";
					}

					case ByteCode.ISTORE:
					{
						return "istore";
					}

					case ByteCode.LSTORE:
					{
						return "lstore";
					}

					case ByteCode.FSTORE:
					{
						return "fstore";
					}

					case ByteCode.DSTORE:
					{
						return "dstore";
					}

					case ByteCode.ASTORE:
					{
						return "astore";
					}

					case ByteCode.ISTORE_0:
					{
						return "istore_0";
					}

					case ByteCode.ISTORE_1:
					{
						return "istore_1";
					}

					case ByteCode.ISTORE_2:
					{
						return "istore_2";
					}

					case ByteCode.ISTORE_3:
					{
						return "istore_3";
					}

					case ByteCode.LSTORE_0:
					{
						return "lstore_0";
					}

					case ByteCode.LSTORE_1:
					{
						return "lstore_1";
					}

					case ByteCode.LSTORE_2:
					{
						return "lstore_2";
					}

					case ByteCode.LSTORE_3:
					{
						return "lstore_3";
					}

					case ByteCode.FSTORE_0:
					{
						return "fstore_0";
					}

					case ByteCode.FSTORE_1:
					{
						return "fstore_1";
					}

					case ByteCode.FSTORE_2:
					{
						return "fstore_2";
					}

					case ByteCode.FSTORE_3:
					{
						return "fstore_3";
					}

					case ByteCode.DSTORE_0:
					{
						return "dstore_0";
					}

					case ByteCode.DSTORE_1:
					{
						return "dstore_1";
					}

					case ByteCode.DSTORE_2:
					{
						return "dstore_2";
					}

					case ByteCode.DSTORE_3:
					{
						return "dstore_3";
					}

					case ByteCode.ASTORE_0:
					{
						return "astore_0";
					}

					case ByteCode.ASTORE_1:
					{
						return "astore_1";
					}

					case ByteCode.ASTORE_2:
					{
						return "astore_2";
					}

					case ByteCode.ASTORE_3:
					{
						return "astore_3";
					}

					case ByteCode.IASTORE:
					{
						return "iastore";
					}

					case ByteCode.LASTORE:
					{
						return "lastore";
					}

					case ByteCode.FASTORE:
					{
						return "fastore";
					}

					case ByteCode.DASTORE:
					{
						return "dastore";
					}

					case ByteCode.AASTORE:
					{
						return "aastore";
					}

					case ByteCode.BASTORE:
					{
						return "bastore";
					}

					case ByteCode.CASTORE:
					{
						return "castore";
					}

					case ByteCode.SASTORE:
					{
						return "sastore";
					}

					case ByteCode.POP:
					{
						return "pop";
					}

					case ByteCode.POP2:
					{
						return "pop2";
					}

					case ByteCode.DUP:
					{
						return "dup";
					}

					case ByteCode.DUP_X1:
					{
						return "dup_x1";
					}

					case ByteCode.DUP_X2:
					{
						return "dup_x2";
					}

					case ByteCode.DUP2:
					{
						return "dup2";
					}

					case ByteCode.DUP2_X1:
					{
						return "dup2_x1";
					}

					case ByteCode.DUP2_X2:
					{
						return "dup2_x2";
					}

					case ByteCode.SWAP:
					{
						return "swap";
					}

					case ByteCode.IADD:
					{
						return "iadd";
					}

					case ByteCode.LADD:
					{
						return "ladd";
					}

					case ByteCode.FADD:
					{
						return "fadd";
					}

					case ByteCode.DADD:
					{
						return "dadd";
					}

					case ByteCode.ISUB:
					{
						return "isub";
					}

					case ByteCode.LSUB:
					{
						return "lsub";
					}

					case ByteCode.FSUB:
					{
						return "fsub";
					}

					case ByteCode.DSUB:
					{
						return "dsub";
					}

					case ByteCode.IMUL:
					{
						return "imul";
					}

					case ByteCode.LMUL:
					{
						return "lmul";
					}

					case ByteCode.FMUL:
					{
						return "fmul";
					}

					case ByteCode.DMUL:
					{
						return "dmul";
					}

					case ByteCode.IDIV:
					{
						return "idiv";
					}

					case ByteCode.LDIV:
					{
						return "ldiv";
					}

					case ByteCode.FDIV:
					{
						return "fdiv";
					}

					case ByteCode.DDIV:
					{
						return "ddiv";
					}

					case ByteCode.IREM:
					{
						return "irem";
					}

					case ByteCode.LREM:
					{
						return "lrem";
					}

					case ByteCode.FREM:
					{
						return "frem";
					}

					case ByteCode.DREM:
					{
						return "drem";
					}

					case ByteCode.INEG:
					{
						return "ineg";
					}

					case ByteCode.LNEG:
					{
						return "lneg";
					}

					case ByteCode.FNEG:
					{
						return "fneg";
					}

					case ByteCode.DNEG:
					{
						return "dneg";
					}

					case ByteCode.ISHL:
					{
						return "ishl";
					}

					case ByteCode.LSHL:
					{
						return "lshl";
					}

					case ByteCode.ISHR:
					{
						return "ishr";
					}

					case ByteCode.LSHR:
					{
						return "lshr";
					}

					case ByteCode.IUSHR:
					{
						return "iushr";
					}

					case ByteCode.LUSHR:
					{
						return "lushr";
					}

					case ByteCode.IAND:
					{
						return "iand";
					}

					case ByteCode.LAND:
					{
						return "land";
					}

					case ByteCode.IOR:
					{
						return "ior";
					}

					case ByteCode.LOR:
					{
						return "lor";
					}

					case ByteCode.IXOR:
					{
						return "ixor";
					}

					case ByteCode.LXOR:
					{
						return "lxor";
					}

					case ByteCode.IINC:
					{
						return "iinc";
					}

					case ByteCode.I2L:
					{
						return "i2l";
					}

					case ByteCode.I2F:
					{
						return "i2f";
					}

					case ByteCode.I2D:
					{
						return "i2d";
					}

					case ByteCode.L2I:
					{
						return "l2i";
					}

					case ByteCode.L2F:
					{
						return "l2f";
					}

					case ByteCode.L2D:
					{
						return "l2d";
					}

					case ByteCode.F2I:
					{
						return "f2i";
					}

					case ByteCode.F2L:
					{
						return "f2l";
					}

					case ByteCode.F2D:
					{
						return "f2d";
					}

					case ByteCode.D2I:
					{
						return "d2i";
					}

					case ByteCode.D2L:
					{
						return "d2l";
					}

					case ByteCode.D2F:
					{
						return "d2f";
					}

					case ByteCode.I2B:
					{
						return "i2b";
					}

					case ByteCode.I2C:
					{
						return "i2c";
					}

					case ByteCode.I2S:
					{
						return "i2s";
					}

					case ByteCode.LCMP:
					{
						return "lcmp";
					}

					case ByteCode.FCMPL:
					{
						return "fcmpl";
					}

					case ByteCode.FCMPG:
					{
						return "fcmpg";
					}

					case ByteCode.DCMPL:
					{
						return "dcmpl";
					}

					case ByteCode.DCMPG:
					{
						return "dcmpg";
					}

					case ByteCode.IFEQ:
					{
						return "ifeq";
					}

					case ByteCode.IFNE:
					{
						return "ifne";
					}

					case ByteCode.IFLT:
					{
						return "iflt";
					}

					case ByteCode.IFGE:
					{
						return "ifge";
					}

					case ByteCode.IFGT:
					{
						return "ifgt";
					}

					case ByteCode.IFLE:
					{
						return "ifle";
					}

					case ByteCode.IF_ICMPEQ:
					{
						return "if_icmpeq";
					}

					case ByteCode.IF_ICMPNE:
					{
						return "if_icmpne";
					}

					case ByteCode.IF_ICMPLT:
					{
						return "if_icmplt";
					}

					case ByteCode.IF_ICMPGE:
					{
						return "if_icmpge";
					}

					case ByteCode.IF_ICMPGT:
					{
						return "if_icmpgt";
					}

					case ByteCode.IF_ICMPLE:
					{
						return "if_icmple";
					}

					case ByteCode.IF_ACMPEQ:
					{
						return "if_acmpeq";
					}

					case ByteCode.IF_ACMPNE:
					{
						return "if_acmpne";
					}

					case ByteCode.GOTO:
					{
						return "goto";
					}

					case ByteCode.JSR:
					{
						return "jsr";
					}

					case ByteCode.RET:
					{
						return "ret";
					}

					case ByteCode.TABLESWITCH:
					{
						return "tableswitch";
					}

					case ByteCode.LOOKUPSWITCH:
					{
						return "lookupswitch";
					}

					case ByteCode.IRETURN:
					{
						return "ireturn";
					}

					case ByteCode.LRETURN:
					{
						return "lreturn";
					}

					case ByteCode.FRETURN:
					{
						return "freturn";
					}

					case ByteCode.DRETURN:
					{
						return "dreturn";
					}

					case ByteCode.ARETURN:
					{
						return "areturn";
					}

					case ByteCode.RETURN:
					{
						return "return";
					}

					case ByteCode.GETSTATIC:
					{
						return "getstatic";
					}

					case ByteCode.PUTSTATIC:
					{
						return "putstatic";
					}

					case ByteCode.GETFIELD:
					{
						return "getfield";
					}

					case ByteCode.PUTFIELD:
					{
						return "putfield";
					}

					case ByteCode.INVOKEVIRTUAL:
					{
						return "invokevirtual";
					}

					case ByteCode.INVOKESPECIAL:
					{
						return "invokespecial";
					}

					case ByteCode.INVOKESTATIC:
					{
						return "invokestatic";
					}

					case ByteCode.INVOKEINTERFACE:
					{
						return "invokeinterface";
					}

					case ByteCode.NEW:
					{
						return "new";
					}

					case ByteCode.NEWARRAY:
					{
						return "newarray";
					}

					case ByteCode.ANEWARRAY:
					{
						return "anewarray";
					}

					case ByteCode.ARRAYLENGTH:
					{
						return "arraylength";
					}

					case ByteCode.ATHROW:
					{
						return "athrow";
					}

					case ByteCode.CHECKCAST:
					{
						return "checkcast";
					}

					case ByteCode.INSTANCEOF:
					{
						return "instanceof";
					}

					case ByteCode.MONITORENTER:
					{
						return "monitorenter";
					}

					case ByteCode.MONITOREXIT:
					{
						return "monitorexit";
					}

					case ByteCode.WIDE:
					{
						return "wide";
					}

					case ByteCode.MULTIANEWARRAY:
					{
						return "multianewarray";
					}

					case ByteCode.IFNULL:
					{
						return "ifnull";
					}

					case ByteCode.IFNONNULL:
					{
						return "ifnonnull";
					}

					case ByteCode.GOTO_W:
					{
						return "goto_w";
					}

					case ByteCode.JSR_W:
					{
						return "jsr_w";
					}

					case ByteCode.BREAKPOINT:
					{
						return "breakpoint";
					}

					case ByteCode.IMPDEP1:
					{
						return "impdep1";
					}

					case ByteCode.IMPDEP2:
					{
						return "impdep2";
					}
				}
			}
			return string.Empty;
		}

		internal char[] GetCharBuffer(int minimalSize)
		{
			if (minimalSize > tmpCharBuffer.Length)
			{
				int newSize = tmpCharBuffer.Length * 2;
				if (minimalSize > newSize)
				{
					newSize = minimalSize;
				}
				tmpCharBuffer = new char[newSize];
			}
			return tmpCharBuffer;
		}

		/// <summary>Add a pc as the start of super block.</summary>
		/// <remarks>
		/// Add a pc as the start of super block.
		/// A pc is the beginning of a super block if:
		/// - pc == 0
		/// - it is the target of a branch instruction
		/// - it is the beginning of an exception handler
		/// - it is directly after an unconditional jump
		/// </remarks>
		private void AddSuperBlockStart(int pc)
		{
			if (GenerateStackMap)
			{
				if (itsSuperBlockStarts == null)
				{
					itsSuperBlockStarts = new int[SuperBlockStartsSize];
				}
				else
				{
					if (itsSuperBlockStarts.Length == itsSuperBlockStartsTop)
					{
						int[] tmp = new int[itsSuperBlockStartsTop * 2];
						System.Array.Copy(itsSuperBlockStarts, 0, tmp, 0, itsSuperBlockStartsTop);
						itsSuperBlockStarts = tmp;
					}
				}
				itsSuperBlockStarts[itsSuperBlockStartsTop++] = pc;
			}
		}

		/// <summary>Sort the list of recorded super block starts and remove duplicates.</summary>
		/// <remarks>
		/// Sort the list of recorded super block starts and remove duplicates.
		/// Also adds exception handling blocks as block starts, since there is no
		/// explicit control flow to these. Used for stack map table generation.
		/// </remarks>
		private void FinalizeSuperBlockStarts()
		{
			if (GenerateStackMap)
			{
				for (int i = 0; i < itsExceptionTableTop; i++)
				{
					ExceptionTableEntry ete = itsExceptionTable[i];
					short handlerPC = (short)GetLabelPC(ete.itsHandlerLabel);
					AddSuperBlockStart(handlerPC);
				}
				Arrays.Sort(itsSuperBlockStarts, 0, itsSuperBlockStartsTop);
				int prev = itsSuperBlockStarts[0];
				int copyTo = 1;
				for (int i_1 = 1; i_1 < itsSuperBlockStartsTop; i_1++)
				{
					int curr = itsSuperBlockStarts[i_1];
					if (prev != curr)
					{
						if (copyTo != i_1)
						{
							itsSuperBlockStarts[copyTo] = curr;
						}
						copyTo++;
						prev = curr;
					}
				}
				itsSuperBlockStartsTop = copyTo;
				if (itsSuperBlockStarts[copyTo - 1] == itsCodeBufferTop)
				{
					itsSuperBlockStartsTop--;
				}
			}
		}

		private int[] itsSuperBlockStarts = null;

		private int itsSuperBlockStartsTop = 0;

		private const int SuperBlockStartsSize = 4;

		private UintMap itsJumpFroms = null;

		private const int LineNumberTableSize = 16;

		private const int ExceptionTableSize = 4;

		private static readonly int MajorVersion;

		private static readonly int MinorVersion;

		private static readonly bool GenerateStackMap;

		static ClassFileWriter()
		{
			// Used to find blocks of code with no dependencies (aka dead code).
			// Necessary for generating type information for dead code, which is
			// expected by the Sun verifier. It is only necessary to store a single
			// jump source to determine if a block is reachable or not.
			// Figure out which classfile version should be generated. This assumes
			// that the runtime used to compile the JavaScript files is the same as
			// the one used to run them. This is important because there are cases
			// when bytecode is generated at runtime, where it is not easy to pass
			// along what version is necessary. Instead, we grab the version numbers
			// from the bytecode of this class and use that.
			//
			// Based on the version numbers we scrape, we can also determine what
			// bytecode features we need. For example, Java 6 bytecode (classfile
			// version 50) should have stack maps generated.
			Stream @is = null;
			int major = 48;
			int minor = 0;
			try
			{
				@is = typeof(ClassFileWriter).GetResourceAsStream("ClassFileWriter.class");
				if (@is == null)
				{
					@is = ClassLoader.GetSystemResourceAsStream("org/mozilla/classfile/ClassFileWriter.class");
				}
				byte[] header = new byte[8];
				// read loop is required since JDK7 will only provide 2 bytes
				// on the first read() - see bug #630111
				int read = 0;
				while (read < 8)
				{
					int c = @is.Read(header, read, 8 - read);
					if (c < 0)
					{
						throw new IOException();
					}
					read += c;
				}
				minor = (header[4] << 8) | (header[5] & unchecked((int)(0xff)));
				major = (header[6] << 8) | (header[7] & unchecked((int)(0xff)));
			}
			catch (Exception)
			{
			}
			finally
			{
				// Unable to get class file, use default bytecode version
				MinorVersion = minor;
				MajorVersion = major;
				GenerateStackMap = major >= 50;
				if (@is != null)
				{
					try
					{
						@is.Close();
					}
					catch (IOException)
					{
					}
				}
			}
		}

		private const int FileHeaderConstant = unchecked((int)(0xCAFEBABE));

		private const bool DEBUGSTACK = false;

		private const bool DEBUGLABELS = false;

		private const bool DEBUGCODE = false;

		private string generatedClassName;

		private ExceptionTableEntry[] itsExceptionTable;

		private int itsExceptionTableTop;

		private int[] itsLineNumberTable;

		private int itsLineNumberTableTop;

		private byte[] itsCodeBuffer = new byte[256];

		private int itsCodeBufferTop;

		private ConstantPool itsConstantPool;

		private ClassFileMethod itsCurrentMethod;

		private short itsStackTop;

		private short itsMaxStack;

		private short itsMaxLocals;

		private ObjArray itsMethods = new ObjArray();

		private ObjArray itsFields = new ObjArray();

		private ObjArray itsInterfaces = new ObjArray();

		private short itsFlags;

		private short itsThisClassIndex;

		private short itsSuperClassIndex;

		private short itsSourceFileNameIndex;

		private const int MIN_LABEL_TABLE_SIZE = 32;

		private int[] itsLabelTable;

		private int itsLabelTableTop;

		private const int MIN_FIXUP_TABLE_SIZE = 40;

		private long[] itsFixupTable;

		private int itsFixupTableTop;

		private ObjArray itsVarDescriptors;

		private char[] tmpCharBuffer = new char[64];
		// Set DEBUG flags to true to get better checking and progress info.
		// pack start_pc & line_number together
		// itsFixupTable[i] = (label_index << 32) | fixup_site
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

	internal sealed class ClassFileField
	{
		internal ClassFileField(short nameIndex, short typeIndex, short flags)
		{
			itsNameIndex = nameIndex;
			itsTypeIndex = typeIndex;
			itsFlags = flags;
			itsHasAttributes = false;
		}

		internal void SetAttributes(short attr1, short attr2, short attr3, int index)
		{
			itsHasAttributes = true;
			itsAttr1 = attr1;
			itsAttr2 = attr2;
			itsAttr3 = attr3;
			itsIndex = index;
		}

		internal int Write(byte[] data, int offset)
		{
			offset = ClassFileWriter.PutInt16(itsFlags, data, offset);
			offset = ClassFileWriter.PutInt16(itsNameIndex, data, offset);
			offset = ClassFileWriter.PutInt16(itsTypeIndex, data, offset);
			if (!itsHasAttributes)
			{
				// write 0 attributes
				offset = ClassFileWriter.PutInt16(0, data, offset);
			}
			else
			{
				offset = ClassFileWriter.PutInt16(1, data, offset);
				offset = ClassFileWriter.PutInt16(itsAttr1, data, offset);
				offset = ClassFileWriter.PutInt16(itsAttr2, data, offset);
				offset = ClassFileWriter.PutInt16(itsAttr3, data, offset);
				offset = ClassFileWriter.PutInt16(itsIndex, data, offset);
			}
			return offset;
		}

		internal int GetWriteSize()
		{
			int size = 2 * 3;
			if (!itsHasAttributes)
			{
				size += 2;
			}
			else
			{
				size += 2 + 2 * 4;
			}
			return size;
		}

		private short itsNameIndex;

		private short itsTypeIndex;

		private short itsFlags;

		private bool itsHasAttributes;

		private short itsAttr1;

		private short itsAttr2;

		private short itsAttr3;

		private int itsIndex;
	}

	internal sealed class ClassFileMethod
	{
		internal ClassFileMethod(string name, short nameIndex, string type, short typeIndex, short flags)
		{
			itsName = name;
			itsNameIndex = nameIndex;
			itsType = type;
			itsTypeIndex = typeIndex;
			itsFlags = flags;
		}

		internal void SetCodeAttribute(byte[] codeAttribute)
		{
			itsCodeAttribute = codeAttribute;
		}

		internal int Write(byte[] data, int offset)
		{
			offset = ClassFileWriter.PutInt16(itsFlags, data, offset);
			offset = ClassFileWriter.PutInt16(itsNameIndex, data, offset);
			offset = ClassFileWriter.PutInt16(itsTypeIndex, data, offset);
			// Code attribute only
			offset = ClassFileWriter.PutInt16(1, data, offset);
			System.Array.Copy(itsCodeAttribute, 0, data, offset, itsCodeAttribute.Length);
			offset += itsCodeAttribute.Length;
			return offset;
		}

		internal int GetWriteSize()
		{
			return 2 * 4 + itsCodeAttribute.Length;
		}

		internal string GetName()
		{
			return itsName;
		}

		internal string GetType()
		{
			return itsType;
		}

		internal short GetFlags()
		{
			return itsFlags;
		}

		private string itsName;

		private string itsType;

		private short itsNameIndex;

		private short itsTypeIndex;

		private short itsFlags;

		private byte[] itsCodeAttribute;
	}

	internal sealed class ConstantPool
	{
		internal ConstantPool(ClassFileWriter cfw)
		{
			this.cfw = cfw;
			itsTopIndex = 1;
			// the zero'th entry is reserved
			itsPool = new byte[ConstantPoolSize];
			itsTop = 0;
		}

		private const int ConstantPoolSize = 256;

		internal const byte CONSTANT_Class = 7;

		internal const byte CONSTANT_Fieldref = 9;

		internal const byte CONSTANT_Methodref = 10;

		internal const byte CONSTANT_InterfaceMethodref = 11;

		internal const byte CONSTANT_String = 8;

		internal const byte CONSTANT_Integer = 3;

		internal const byte CONSTANT_Float = 4;

		internal const byte CONSTANT_Long = 5;

		internal const byte CONSTANT_Double = 6;

		internal const byte CONSTANT_NameAndType = 12;

		internal const byte CONSTANT_Utf8 = 1;

		internal int Write(byte[] data, int offset)
		{
			offset = ClassFileWriter.PutInt16((short)itsTopIndex, data, offset);
			System.Array.Copy(itsPool, 0, data, offset, itsTop);
			offset += itsTop;
			return offset;
		}

		internal int GetWriteSize()
		{
			return 2 + itsTop;
		}

		internal int AddConstant(int k)
		{
			Ensure(5);
			itsPool[itsTop++] = CONSTANT_Integer;
			itsTop = ClassFileWriter.PutInt32(k, itsPool, itsTop);
			itsPoolTypes.Put(itsTopIndex, CONSTANT_Integer);
			return (short)(itsTopIndex++);
		}

		internal int AddConstant(long k)
		{
			Ensure(9);
			itsPool[itsTop++] = CONSTANT_Long;
			itsTop = ClassFileWriter.PutInt64(k, itsPool, itsTop);
			int index = itsTopIndex;
			itsTopIndex += 2;
			itsPoolTypes.Put(index, CONSTANT_Long);
			return index;
		}

		internal int AddConstant(float k)
		{
			Ensure(5);
			itsPool[itsTop++] = CONSTANT_Float;
			int bits = Sharpen.Runtime.FloatToIntBits(k);
			itsTop = ClassFileWriter.PutInt32(bits, itsPool, itsTop);
			itsPoolTypes.Put(itsTopIndex, CONSTANT_Float);
			return itsTopIndex++;
		}

		internal int AddConstant(double k)
		{
			Ensure(9);
			itsPool[itsTop++] = CONSTANT_Double;
			long bits = System.BitConverter.DoubleToInt64Bits(k);
			itsTop = ClassFileWriter.PutInt64(bits, itsPool, itsTop);
			int index = itsTopIndex;
			itsTopIndex += 2;
			itsPoolTypes.Put(index, CONSTANT_Double);
			return index;
		}

		internal int AddConstant(string k)
		{
			int utf8Index = unchecked((int)(0xFFFF)) & AddUtf8(k);
			int theIndex = itsStringConstHash.GetInt(utf8Index, -1);
			if (theIndex == -1)
			{
				theIndex = itsTopIndex++;
				Ensure(3);
				itsPool[itsTop++] = CONSTANT_String;
				itsTop = ClassFileWriter.PutInt16(utf8Index, itsPool, itsTop);
				itsStringConstHash.Put(utf8Index, theIndex);
			}
			itsPoolTypes.Put(theIndex, CONSTANT_String);
			return theIndex;
		}

		internal bool IsUnderUtfEncodingLimit(string s)
		{
			int strLen = s.Length;
			if (strLen * 3 <= MAX_UTF_ENCODING_SIZE)
			{
				return true;
			}
			else
			{
				if (strLen > MAX_UTF_ENCODING_SIZE)
				{
					return false;
				}
			}
			return strLen == GetUtfEncodingLimit(s, 0, strLen);
		}

		/// <summary>
		/// Get maximum i such that <tt>start <= i &lt;= end&lt;/tt> and
		/// <tt>s.substring(start, i)</tt> fits JVM UTF string encoding limit.
		/// </summary>
		/// <remarks>
		/// Get maximum i such that <tt>start <= i &lt;= end&lt;/tt> and
		/// <tt>s.substring(start, i)</tt> fits JVM UTF string encoding limit.
		/// </remarks>
		internal int GetUtfEncodingLimit(string s, int start, int end)
		{
			if ((end - start) * 3 <= MAX_UTF_ENCODING_SIZE)
			{
				return end;
			}
			int limit = MAX_UTF_ENCODING_SIZE;
			for (int i = start; i != end; i++)
			{
				int c = s[i];
				if (0 != c && c <= unchecked((int)(0x7F)))
				{
					--limit;
				}
				else
				{
					if (c < unchecked((int)(0x7FF)))
					{
						limit -= 2;
					}
					else
					{
						limit -= 3;
					}
				}
				if (limit < 0)
				{
					return i;
				}
			}
			return end;
		}

		internal short AddUtf8(string k)
		{
			int theIndex = itsUtf8Hash.Get(k, -1);
			if (theIndex == -1)
			{
				int strLen = k.Length;
				bool tooBigString;
				if (strLen > MAX_UTF_ENCODING_SIZE)
				{
					tooBigString = true;
				}
				else
				{
					tooBigString = false;
					// Ask for worst case scenario buffer when each char takes 3
					// bytes
					Ensure(1 + 2 + strLen * 3);
					int top = itsTop;
					itsPool[top++] = CONSTANT_Utf8;
					top += 2;
					// skip length
					char[] chars = cfw.GetCharBuffer(strLen);
					Sharpen.Runtime.GetCharsForString(k, 0, strLen, chars, 0);
					for (int i = 0; i != strLen; i++)
					{
						int c = chars[i];
						if (c != 0 && c <= unchecked((int)(0x7F)))
						{
							itsPool[top++] = unchecked((byte)c);
						}
						else
						{
							if (c > unchecked((int)(0x7FF)))
							{
								itsPool[top++] = unchecked((byte)(unchecked((int)(0xE0)) | (c >> 12)));
								itsPool[top++] = unchecked((byte)(unchecked((int)(0x80)) | ((c >> 6) & unchecked((int)(0x3F)))));
								itsPool[top++] = unchecked((byte)(unchecked((int)(0x80)) | (c & unchecked((int)(0x3F)))));
							}
							else
							{
								itsPool[top++] = unchecked((byte)(unchecked((int)(0xC0)) | (c >> 6)));
								itsPool[top++] = unchecked((byte)(unchecked((int)(0x80)) | (c & unchecked((int)(0x3F)))));
							}
						}
					}
					int utfLen = top - (itsTop + 1 + 2);
					if (utfLen > MAX_UTF_ENCODING_SIZE)
					{
						tooBigString = true;
					}
					else
					{
						// Write back length
						itsPool[itsTop + 1] = unchecked((byte)((int)(((uint)utfLen) >> 8)));
						itsPool[itsTop + 2] = unchecked((byte)utfLen);
						itsTop = top;
						theIndex = itsTopIndex++;
						itsUtf8Hash.Put(k, theIndex);
					}
				}
				if (tooBigString)
				{
					throw new ArgumentException("Too big string");
				}
			}
			SetConstantData(theIndex, k);
			itsPoolTypes.Put(theIndex, CONSTANT_Utf8);
			return (short)theIndex;
		}

		private short AddNameAndType(string name, string type)
		{
			short nameIndex = AddUtf8(name);
			short typeIndex = AddUtf8(type);
			Ensure(5);
			itsPool[itsTop++] = CONSTANT_NameAndType;
			itsTop = ClassFileWriter.PutInt16(nameIndex, itsPool, itsTop);
			itsTop = ClassFileWriter.PutInt16(typeIndex, itsPool, itsTop);
			itsPoolTypes.Put(itsTopIndex, CONSTANT_NameAndType);
			return (short)(itsTopIndex++);
		}

		internal short AddClass(string className)
		{
			int theIndex = itsClassHash.Get(className, -1);
			if (theIndex == -1)
			{
				string slashed = className;
				if (className.IndexOf('.') > 0)
				{
					slashed = ClassFileWriter.GetSlashedForm(className);
					theIndex = itsClassHash.Get(slashed, -1);
					if (theIndex != -1)
					{
						itsClassHash.Put(className, theIndex);
					}
				}
				if (theIndex == -1)
				{
					int utf8Index = AddUtf8(slashed);
					Ensure(3);
					itsPool[itsTop++] = CONSTANT_Class;
					itsTop = ClassFileWriter.PutInt16(utf8Index, itsPool, itsTop);
					theIndex = itsTopIndex++;
					itsClassHash.Put(slashed, theIndex);
					if (className != slashed)
					{
						itsClassHash.Put(className, theIndex);
					}
				}
			}
			SetConstantData(theIndex, className);
			itsPoolTypes.Put(theIndex, CONSTANT_Class);
			return (short)theIndex;
		}

		internal short AddFieldRef(string className, string fieldName, string fieldType)
		{
			FieldOrMethodRef @ref = new FieldOrMethodRef(className, fieldName, fieldType);
			int theIndex = itsFieldRefHash.Get(@ref, -1);
			if (theIndex == -1)
			{
				short ntIndex = AddNameAndType(fieldName, fieldType);
				short classIndex = AddClass(className);
				Ensure(5);
				itsPool[itsTop++] = CONSTANT_Fieldref;
				itsTop = ClassFileWriter.PutInt16(classIndex, itsPool, itsTop);
				itsTop = ClassFileWriter.PutInt16(ntIndex, itsPool, itsTop);
				theIndex = itsTopIndex++;
				itsFieldRefHash.Put(@ref, theIndex);
			}
			SetConstantData(theIndex, @ref);
			itsPoolTypes.Put(theIndex, CONSTANT_Fieldref);
			return (short)theIndex;
		}

		internal short AddMethodRef(string className, string methodName, string methodType)
		{
			FieldOrMethodRef @ref = new FieldOrMethodRef(className, methodName, methodType);
			int theIndex = itsMethodRefHash.Get(@ref, -1);
			if (theIndex == -1)
			{
				short ntIndex = AddNameAndType(methodName, methodType);
				short classIndex = AddClass(className);
				Ensure(5);
				itsPool[itsTop++] = CONSTANT_Methodref;
				itsTop = ClassFileWriter.PutInt16(classIndex, itsPool, itsTop);
				itsTop = ClassFileWriter.PutInt16(ntIndex, itsPool, itsTop);
				theIndex = itsTopIndex++;
				itsMethodRefHash.Put(@ref, theIndex);
			}
			SetConstantData(theIndex, @ref);
			itsPoolTypes.Put(theIndex, CONSTANT_Methodref);
			return (short)theIndex;
		}

		internal short AddInterfaceMethodRef(string className, string methodName, string methodType)
		{
			short ntIndex = AddNameAndType(methodName, methodType);
			short classIndex = AddClass(className);
			Ensure(5);
			itsPool[itsTop++] = CONSTANT_InterfaceMethodref;
			itsTop = ClassFileWriter.PutInt16(classIndex, itsPool, itsTop);
			itsTop = ClassFileWriter.PutInt16(ntIndex, itsPool, itsTop);
			FieldOrMethodRef r = new FieldOrMethodRef(className, methodName, methodType);
			SetConstantData(itsTopIndex, r);
			itsPoolTypes.Put(itsTopIndex, CONSTANT_InterfaceMethodref);
			return (short)(itsTopIndex++);
		}

		internal object GetConstantData(int index)
		{
			return itsConstantData.GetObject(index);
		}

		internal void SetConstantData(int index, object data)
		{
			itsConstantData.Put(index, data);
		}

		internal byte GetConstantType(int index)
		{
			return unchecked((byte)itsPoolTypes.GetInt(index, 0));
		}

		internal void Ensure(int howMuch)
		{
			if (itsTop + howMuch > itsPool.Length)
			{
				int newCapacity = itsPool.Length * 2;
				if (itsTop + howMuch > newCapacity)
				{
					newCapacity = itsTop + howMuch;
				}
				byte[] tmp = new byte[newCapacity];
				System.Array.Copy(itsPool, 0, tmp, 0, itsTop);
				itsPool = tmp;
			}
		}

		private ClassFileWriter cfw;

		private const int MAX_UTF_ENCODING_SIZE = 65535;

		private UintMap itsStringConstHash = new UintMap();

		private ObjToIntMap itsUtf8Hash = new ObjToIntMap();

		private ObjToIntMap itsFieldRefHash = new ObjToIntMap();

		private ObjToIntMap itsMethodRefHash = new ObjToIntMap();

		private ObjToIntMap itsClassHash = new ObjToIntMap();

		private int itsTop;

		private int itsTopIndex;

		private UintMap itsConstantData = new UintMap();

		private UintMap itsPoolTypes = new UintMap();

		private byte[] itsPool;
	}

	internal sealed class FieldOrMethodRef
	{
		internal FieldOrMethodRef(string className, string name, string type)
		{
			this.className = className;
			this.name = name;
			this.type = type;
		}

		public string GetClassName()
		{
			return className;
		}

		public string GetName()
		{
			return name;
		}

		public string GetType()
		{
			return type;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Org.Mozilla.Classfile.FieldOrMethodRef))
			{
				return false;
			}
			Org.Mozilla.Classfile.FieldOrMethodRef x = (Org.Mozilla.Classfile.FieldOrMethodRef)obj;
			return className.Equals(x.className) && name.Equals(x.name) && type.Equals(x.type);
		}

		public override int GetHashCode()
		{
			if (hashCode == -1)
			{
				int h1 = className.GetHashCode();
				int h2 = name.GetHashCode();
				int h3 = type.GetHashCode();
				hashCode = h1 ^ h2 ^ h3;
			}
			return hashCode;
		}

		private string className;

		private string name;

		private string type;

		private int hashCode = -1;
	}

	/// <summary>
	/// A super block is defined as a contiguous chunk of code with a single entry
	/// point and multiple exit points (therefore ending in an unconditional jump
	/// or the end of the method).
	/// </summary>
	/// <remarks>
	/// A super block is defined as a contiguous chunk of code with a single entry
	/// point and multiple exit points (therefore ending in an unconditional jump
	/// or the end of the method). This is used to emulate OpenJDK's compiler, which
	/// outputs stack map frames at the start of every super block except the method
	/// start.
	/// </remarks>
	internal sealed class SuperBlock
	{
		internal SuperBlock(int index, int start, int end, int[] initialLocals)
		{
			this.index = index;
			this.start = start;
			this.end = end;
			locals = new int[initialLocals.Length];
			System.Array.Copy(initialLocals, 0, locals, 0, initialLocals.Length);
			stack = new int[0];
			isInitialized = false;
			isInQueue = false;
		}

		internal int GetIndex()
		{
			return index;
		}

		internal int[] GetLocals()
		{
			int[] copy = new int[locals.Length];
			System.Array.Copy(locals, 0, copy, 0, locals.Length);
			return copy;
		}

		/// <summary>Get a copy of the super block's locals without any trailing TOP types.</summary>
		/// <remarks>
		/// Get a copy of the super block's locals without any trailing TOP types.
		/// This is useful for actual writing stack maps; during the computation of
		/// stack map types, all local arrays have the same size; the max locals for
		/// the method. In addition, DOUBLE and LONG types have trailing TOP types
		/// because they occupy two words. For writing purposes, these are not
		/// useful.
		/// </remarks>
		internal int[] GetTrimmedLocals()
		{
			int last = locals.Length - 1;
			// Exclude all of the trailing TOPs not bound to a DOUBLE/LONG
			while (last >= 0 && locals[last] == TypeInfo.TOP && !TypeInfo.IsTwoWords(locals[last - 1]))
			{
				last--;
			}
			last++;
			// Exclude trailing TOPs following a DOUBLE/LONG
			int size = last;
			for (int i = 0; i < last; i++)
			{
				if (TypeInfo.IsTwoWords(locals[i]))
				{
					size--;
				}
			}
			int[] copy = new int[size];
			for (int i_1 = 0, j = 0; i_1 < size; i_1++, j++)
			{
				copy[i_1] = locals[j];
				if (TypeInfo.IsTwoWords(locals[j]))
				{
					j++;
				}
			}
			return copy;
		}

		internal int[] GetStack()
		{
			int[] copy = new int[stack.Length];
			System.Array.Copy(stack, 0, copy, 0, stack.Length);
			return copy;
		}

		internal bool Merge(int[] locals, int localsTop, int[] stack, int stackTop, ConstantPool pool)
		{
			if (!isInitialized)
			{
				System.Array.Copy(locals, 0, this.locals, 0, localsTop);
				this.stack = new int[stackTop];
				System.Array.Copy(stack, 0, this.stack, 0, stackTop);
				isInitialized = true;
				return true;
			}
			else
			{
				if (this.locals.Length == localsTop && this.stack.Length == stackTop)
				{
					bool localsChanged = MergeState(this.locals, locals, localsTop, pool);
					bool stackChanged = MergeState(this.stack, stack, stackTop, pool);
					return localsChanged || stackChanged;
				}
				else
				{
					throw new ArgumentException("bad merge attempt");
				}
			}
		}

		/// <summary>Merge an operand stack or local variable array with incoming state.</summary>
		/// <remarks>
		/// Merge an operand stack or local variable array with incoming state.
		/// They are treated the same way; by this point, it should already be
		/// ensured that the array sizes are the same, which is the only additional
		/// constraint that is imposed on merging operand stacks (the local variable
		/// array is always the same size).
		/// </remarks>
		private bool MergeState(int[] current, int[] incoming, int size, ConstantPool pool)
		{
			bool changed = false;
			for (int i = 0; i < size; i++)
			{
				int currentType = current[i];
				current[i] = TypeInfo.Merge(current[i], incoming[i], pool);
				if (currentType != current[i])
				{
					changed = true;
				}
			}
			return changed;
		}

		internal int GetStart()
		{
			return start;
		}

		internal int GetEnd()
		{
			return end;
		}

		public override string ToString()
		{
			return "sb " + index;
		}

		internal bool IsInitialized()
		{
			return isInitialized;
		}

		internal void SetInitialized(bool b)
		{
			isInitialized = b;
		}

		internal bool IsInQueue()
		{
			return isInQueue;
		}

		internal void SetInQueue(bool b)
		{
			isInQueue = b;
		}

		private int index;

		private int start;

		private int end;

		private int[] locals;

		private int[] stack;

		private bool isInitialized;

		private bool isInQueue;
	}

	/// <summary>Helper class for internal representations of type information.</summary>
	/// <remarks>
	/// Helper class for internal representations of type information. In most
	/// cases, type information can be represented by a constant, but in some
	/// cases, a payload is included. Despite the payload coming after the type
	/// tag in the output, we store it in bits 8-23 for uniformity; the tag is
	/// always in bits 0-7.
	/// </remarks>
	internal sealed class TypeInfo
	{
		private TypeInfo()
		{
		}

		internal const int TOP = 0;

		internal const int INTEGER = 1;

		internal const int FLOAT = 2;

		internal const int DOUBLE = 3;

		internal const int LONG = 4;

		internal const int NULL = 5;

		internal const int UNINITIALIZED_THIS = 6;

		internal const int OBJECT_TAG = 7;

		internal const int UNINITIALIZED_VAR_TAG = 8;

		internal static int OBJECT(int constantPoolIndex)
		{
			return ((constantPoolIndex & unchecked((int)(0xFFFF))) << 8) | OBJECT_TAG;
		}

		internal static int OBJECT(string type, ConstantPool pool)
		{
			return OBJECT(pool.AddClass(type));
		}

		internal static int UNINITIALIZED_VARIABLE(int bytecodeOffset)
		{
			return ((bytecodeOffset & unchecked((int)(0xFFFF))) << 8) | UNINITIALIZED_VAR_TAG;
		}

		internal static int GetTag(int typeInfo)
		{
			return typeInfo & unchecked((int)(0xFF));
		}

		internal static int GetPayload(int typeInfo)
		{
			return (int)(((uint)typeInfo) >> 8);
		}

		/// <summary>
		/// Treat the result of getPayload as a constant pool index and fetch the
		/// corresponding String mapped to it.
		/// </summary>
		/// <remarks>
		/// Treat the result of getPayload as a constant pool index and fetch the
		/// corresponding String mapped to it.
		/// Only works on OBJECT types.
		/// </remarks>
		internal static string GetPayloadAsType(int typeInfo, ConstantPool pool)
		{
			if (GetTag(typeInfo) == OBJECT_TAG)
			{
				return (string)pool.GetConstantData(GetPayload(typeInfo));
			}
			throw new ArgumentException("expecting object type");
		}

		/// <summary>Create type information from an internal type.</summary>
		/// <remarks>Create type information from an internal type.</remarks>
		internal static int FromType(string type, ConstantPool pool)
		{
			if (type.Length == 1)
			{
				switch (type[0])
				{
					case 'B':
					case 'C':
					case 'S':
					case 'Z':
					case 'I':
					{
						// sbyte
						// unicode char
						// short
						// boolean
						// all of the above are verified as integers
						return INTEGER;
					}

					case 'D':
					{
						return DOUBLE;
					}

					case 'F':
					{
						return FLOAT;
					}

					case 'J':
					{
						return LONG;
					}

					default:
					{
						throw new ArgumentException("bad type");
					}
				}
			}
			return Org.Mozilla.Classfile.TypeInfo.OBJECT(type, pool);
		}

		internal static bool IsTwoWords(int type)
		{
			return type == DOUBLE || type == LONG;
		}

		/// <summary>Merge two verification types.</summary>
		/// <remarks>
		/// Merge two verification types.
		/// In most cases, the verification types must be the same. For example,
		/// INTEGER and DOUBLE cannot be merged and an exception will be thrown.
		/// The basic rules are:
		/// - If the types are equal, simply return one.
		/// - If either type is TOP, return TOP.
		/// - If either type is NULL, return the other type.
		/// - If both types are objects, find the lowest common ancestor in the
		/// class hierarchy.
		/// This method uses reflection to traverse the class hierarchy. Therefore,
		/// it is assumed that the current class being generated is never the target
		/// of a full object-object merge, which would need to load the current
		/// class reflectively.
		/// </remarks>
		internal static int Merge(int current, int incoming, ConstantPool pool)
		{
			int currentTag = GetTag(current);
			int incomingTag = GetTag(incoming);
			bool currentIsObject = currentTag == Org.Mozilla.Classfile.TypeInfo.OBJECT_TAG;
			bool incomingIsObject = incomingTag == Org.Mozilla.Classfile.TypeInfo.OBJECT_TAG;
			if (current == incoming || (currentIsObject && incoming == NULL))
			{
				return current;
			}
			else
			{
				if (currentTag == Org.Mozilla.Classfile.TypeInfo.TOP || incomingTag == Org.Mozilla.Classfile.TypeInfo.TOP)
				{
					return Org.Mozilla.Classfile.TypeInfo.TOP;
				}
				else
				{
					if (current == NULL && incomingIsObject)
					{
						return incoming;
					}
					else
					{
						if (currentIsObject && incomingIsObject)
						{
							string currentName = GetPayloadAsType(current, pool);
							string incomingName = GetPayloadAsType(incoming, pool);
							// The class file always has the class and super names in the same
							// spot. The constant order is: class_data, class_name, super_data,
							// super_name.
							string currentlyGeneratedName = (string)pool.GetConstantData(2);
							string currentlyGeneratedSuperName = (string)pool.GetConstantData(4);
							// If any of the merged types are the class that's currently being
							// generated, automatically start at the super class instead. At
							// this point, we already know the classes are different, so we
							// don't need to handle that case.
							if (currentName.Equals(currentlyGeneratedName))
							{
								currentName = currentlyGeneratedSuperName;
							}
							if (incomingName.Equals(currentlyGeneratedName))
							{
								incomingName = currentlyGeneratedSuperName;
							}
							Type currentClass = GetClassFromInternalName(currentName);
							Type incomingClass = GetClassFromInternalName(incomingName);
							if (currentClass.IsAssignableFrom(incomingClass))
							{
								return current;
							}
							else
							{
								if (incomingClass.IsAssignableFrom(currentClass))
								{
									return incoming;
								}
								else
								{
									if (incomingClass.IsInterface || currentClass.IsInterface)
									{
										// For verification purposes, Sun specifies that interfaces are
										// subtypes of Object. Therefore, we know that the merge result
										// involving interfaces where one is not assignable to the
										// other results in Object.
										return OBJECT("java/lang/Object", pool);
									}
									else
									{
										Type commonClass = incomingClass.BaseType;
										while (commonClass != null)
										{
											if (commonClass.IsAssignableFrom(currentClass))
											{
												string name = commonClass.FullName;
												name = ClassFileWriter.GetSlashedForm(name);
												return OBJECT(name, pool);
											}
											commonClass = commonClass.BaseType;
										}
									}
								}
							}
						}
					}
				}
			}
			throw new ArgumentException("bad merge attempt between " + ToString(current, pool) + " and " + ToString(incoming, pool));
		}

		internal static string ToString(int type, ConstantPool pool)
		{
			int tag = GetTag(type);
			switch (tag)
			{
				case Org.Mozilla.Classfile.TypeInfo.TOP:
				{
					return "top";
				}

				case Org.Mozilla.Classfile.TypeInfo.INTEGER:
				{
					return "int";
				}

				case Org.Mozilla.Classfile.TypeInfo.FLOAT:
				{
					return "float";
				}

				case Org.Mozilla.Classfile.TypeInfo.DOUBLE:
				{
					return "double";
				}

				case Org.Mozilla.Classfile.TypeInfo.LONG:
				{
					return "long";
				}

				case Org.Mozilla.Classfile.TypeInfo.NULL:
				{
					return "null";
				}

				case Org.Mozilla.Classfile.TypeInfo.UNINITIALIZED_THIS:
				{
					return "uninitialized_this";
				}

				default:
				{
					if (tag == Org.Mozilla.Classfile.TypeInfo.OBJECT_TAG)
					{
						return GetPayloadAsType(type, pool);
					}
					else
					{
						if (tag == Org.Mozilla.Classfile.TypeInfo.UNINITIALIZED_VAR_TAG)
						{
							return "uninitialized";
						}
						else
						{
							throw new ArgumentException("bad type");
						}
					}
					break;
				}
			}
		}

		/// <summary>
		/// Take an internal name and return a java.lang.Class instance that
		/// represents it.
		/// </summary>
		/// <remarks>
		/// Take an internal name and return a java.lang.Class instance that
		/// represents it.
		/// For example, given "java/lang/Object", returns the equivalent of
		/// Class.forName("java.lang.Object"), but also handles exceptions.
		/// </remarks>
		internal static Type GetClassFromInternalName(string internalName)
		{
			try
			{
				return Sharpen.Runtime.GetType(internalName.Replace('/', '.'));
			}
			catch (TypeLoadException e)
			{
				throw new Exception(e);
			}
		}

		internal static string ToString(int[] types, ConstantPool pool)
		{
			return ToString(types, types.Length, pool);
		}

		internal static string ToString(int[] types, int typesTop, ConstantPool pool)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("[");
			for (int i = 0; i < typesTop; i++)
			{
				if (i > 0)
				{
					sb.Append(", ");
				}
				sb.Append(ToString(types[i], pool));
			}
			sb.Append("]");
			return sb.ToString();
		}

		internal static void Print(int[] locals, int[] stack, ConstantPool pool)
		{
			Print(locals, locals.Length, stack, stack.Length, pool);
		}

		internal static void Print(int[] locals, int localsTop, int[] stack, int stackTop, ConstantPool pool)
		{
			System.Console.Out.Write("locals: ");
			System.Console.Out.WriteLine(ToString(locals, localsTop, pool));
			System.Console.Out.Write("stack: ");
			System.Console.Out.WriteLine(ToString(stack, stackTop, pool));
			System.Console.Out.WriteLine();
		}
	}
}
