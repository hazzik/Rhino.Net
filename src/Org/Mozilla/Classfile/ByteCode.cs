/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Org.Mozilla.Classfile;
using Sharpen;

namespace Org.Mozilla.Classfile
{
	/// <summary>This class provides opcode values expected by the JVM in Java class files.</summary>
	/// <remarks>
	/// This class provides opcode values expected by the JVM in Java class files.
	/// It also provides tables for internal use by the ClassFileWriter.
	/// </remarks>
	/// <author>Roger Lawrence</author>
	public class ByteCode
	{
		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int NOP = unchecked((int)(0x00));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ACONST_NULL = unchecked((int)(0x01));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ICONST_M1 = unchecked((int)(0x02));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ICONST_0 = unchecked((int)(0x03));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ICONST_1 = unchecked((int)(0x04));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ICONST_2 = unchecked((int)(0x05));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ICONST_3 = unchecked((int)(0x06));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ICONST_4 = unchecked((int)(0x07));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ICONST_5 = unchecked((int)(0x08));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LCONST_0 = unchecked((int)(0x09));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LCONST_1 = unchecked((int)(0x0A));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FCONST_0 = unchecked((int)(0x0B));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FCONST_1 = unchecked((int)(0x0C));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FCONST_2 = unchecked((int)(0x0D));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DCONST_0 = unchecked((int)(0x0E));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DCONST_1 = unchecked((int)(0x0F));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int BIPUSH = unchecked((int)(0x10));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int SIPUSH = unchecked((int)(0x11));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LDC = unchecked((int)(0x12));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LDC_W = unchecked((int)(0x13));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LDC2_W = unchecked((int)(0x14));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ILOAD = unchecked((int)(0x15));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LLOAD = unchecked((int)(0x16));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FLOAD = unchecked((int)(0x17));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DLOAD = unchecked((int)(0x18));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ALOAD = unchecked((int)(0x19));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ILOAD_0 = unchecked((int)(0x1A));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ILOAD_1 = unchecked((int)(0x1B));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ILOAD_2 = unchecked((int)(0x1C));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ILOAD_3 = unchecked((int)(0x1D));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LLOAD_0 = unchecked((int)(0x1E));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LLOAD_1 = unchecked((int)(0x1F));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LLOAD_2 = unchecked((int)(0x20));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LLOAD_3 = unchecked((int)(0x21));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FLOAD_0 = unchecked((int)(0x22));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FLOAD_1 = unchecked((int)(0x23));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FLOAD_2 = unchecked((int)(0x24));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FLOAD_3 = unchecked((int)(0x25));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DLOAD_0 = unchecked((int)(0x26));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DLOAD_1 = unchecked((int)(0x27));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DLOAD_2 = unchecked((int)(0x28));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DLOAD_3 = unchecked((int)(0x29));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ALOAD_0 = unchecked((int)(0x2A));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ALOAD_1 = unchecked((int)(0x2B));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ALOAD_2 = unchecked((int)(0x2C));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ALOAD_3 = unchecked((int)(0x2D));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IALOAD = unchecked((int)(0x2E));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LALOAD = unchecked((int)(0x2F));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FALOAD = unchecked((int)(0x30));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DALOAD = unchecked((int)(0x31));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int AALOAD = unchecked((int)(0x32));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int BALOAD = unchecked((int)(0x33));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int CALOAD = unchecked((int)(0x34));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int SALOAD = unchecked((int)(0x35));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ISTORE = unchecked((int)(0x36));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LSTORE = unchecked((int)(0x37));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FSTORE = unchecked((int)(0x38));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DSTORE = unchecked((int)(0x39));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ASTORE = unchecked((int)(0x3A));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ISTORE_0 = unchecked((int)(0x3B));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ISTORE_1 = unchecked((int)(0x3C));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ISTORE_2 = unchecked((int)(0x3D));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ISTORE_3 = unchecked((int)(0x3E));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LSTORE_0 = unchecked((int)(0x3F));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LSTORE_1 = unchecked((int)(0x40));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LSTORE_2 = unchecked((int)(0x41));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LSTORE_3 = unchecked((int)(0x42));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FSTORE_0 = unchecked((int)(0x43));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FSTORE_1 = unchecked((int)(0x44));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FSTORE_2 = unchecked((int)(0x45));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FSTORE_3 = unchecked((int)(0x46));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DSTORE_0 = unchecked((int)(0x47));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DSTORE_1 = unchecked((int)(0x48));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DSTORE_2 = unchecked((int)(0x49));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DSTORE_3 = unchecked((int)(0x4A));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ASTORE_0 = unchecked((int)(0x4B));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ASTORE_1 = unchecked((int)(0x4C));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ASTORE_2 = unchecked((int)(0x4D));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ASTORE_3 = unchecked((int)(0x4E));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IASTORE = unchecked((int)(0x4F));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LASTORE = unchecked((int)(0x50));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FASTORE = unchecked((int)(0x51));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DASTORE = unchecked((int)(0x52));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int AASTORE = unchecked((int)(0x53));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int BASTORE = unchecked((int)(0x54));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int CASTORE = unchecked((int)(0x55));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int SASTORE = unchecked((int)(0x56));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int POP = unchecked((int)(0x57));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int POP2 = unchecked((int)(0x58));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DUP = unchecked((int)(0x59));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DUP_X1 = unchecked((int)(0x5A));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DUP_X2 = unchecked((int)(0x5B));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DUP2 = unchecked((int)(0x5C));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DUP2_X1 = unchecked((int)(0x5D));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DUP2_X2 = unchecked((int)(0x5E));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int SWAP = unchecked((int)(0x5F));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IADD = unchecked((int)(0x60));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LADD = unchecked((int)(0x61));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FADD = unchecked((int)(0x62));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DADD = unchecked((int)(0x63));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ISUB = unchecked((int)(0x64));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LSUB = unchecked((int)(0x65));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FSUB = unchecked((int)(0x66));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DSUB = unchecked((int)(0x67));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IMUL = unchecked((int)(0x68));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LMUL = unchecked((int)(0x69));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FMUL = unchecked((int)(0x6A));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DMUL = unchecked((int)(0x6B));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IDIV = unchecked((int)(0x6C));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LDIV = unchecked((int)(0x6D));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FDIV = unchecked((int)(0x6E));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DDIV = unchecked((int)(0x6F));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IREM = unchecked((int)(0x70));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LREM = unchecked((int)(0x71));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FREM = unchecked((int)(0x72));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DREM = unchecked((int)(0x73));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int INEG = unchecked((int)(0x74));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LNEG = unchecked((int)(0x75));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FNEG = unchecked((int)(0x76));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DNEG = unchecked((int)(0x77));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ISHL = unchecked((int)(0x78));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LSHL = unchecked((int)(0x79));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ISHR = unchecked((int)(0x7A));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LSHR = unchecked((int)(0x7B));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IUSHR = unchecked((int)(0x7C));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LUSHR = unchecked((int)(0x7D));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IAND = unchecked((int)(0x7E));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LAND = unchecked((int)(0x7F));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IOR = unchecked((int)(0x80));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LOR = unchecked((int)(0x81));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IXOR = unchecked((int)(0x82));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LXOR = unchecked((int)(0x83));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IINC = unchecked((int)(0x84));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int I2L = unchecked((int)(0x85));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int I2F = unchecked((int)(0x86));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int I2D = unchecked((int)(0x87));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int L2I = unchecked((int)(0x88));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int L2F = unchecked((int)(0x89));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int L2D = unchecked((int)(0x8A));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int F2I = unchecked((int)(0x8B));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int F2L = unchecked((int)(0x8C));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int F2D = unchecked((int)(0x8D));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int D2I = unchecked((int)(0x8E));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int D2L = unchecked((int)(0x8F));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int D2F = unchecked((int)(0x90));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int I2B = unchecked((int)(0x91));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int I2C = unchecked((int)(0x92));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int I2S = unchecked((int)(0x93));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LCMP = unchecked((int)(0x94));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FCMPL = unchecked((int)(0x95));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FCMPG = unchecked((int)(0x96));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DCMPL = unchecked((int)(0x97));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DCMPG = unchecked((int)(0x98));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IFEQ = unchecked((int)(0x99));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IFNE = unchecked((int)(0x9A));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IFLT = unchecked((int)(0x9B));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IFGE = unchecked((int)(0x9C));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IFGT = unchecked((int)(0x9D));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IFLE = unchecked((int)(0x9E));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IF_ICMPEQ = unchecked((int)(0x9F));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IF_ICMPNE = unchecked((int)(0xA0));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IF_ICMPLT = unchecked((int)(0xA1));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IF_ICMPGE = unchecked((int)(0xA2));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IF_ICMPGT = unchecked((int)(0xA3));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IF_ICMPLE = unchecked((int)(0xA4));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IF_ACMPEQ = unchecked((int)(0xA5));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IF_ACMPNE = unchecked((int)(0xA6));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int GOTO = unchecked((int)(0xA7));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int JSR = unchecked((int)(0xA8));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int RET = unchecked((int)(0xA9));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int TABLESWITCH = unchecked((int)(0xAA));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LOOKUPSWITCH = unchecked((int)(0xAB));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IRETURN = unchecked((int)(0xAC));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int LRETURN = unchecked((int)(0xAD));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int FRETURN = unchecked((int)(0xAE));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int DRETURN = unchecked((int)(0xAF));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ARETURN = unchecked((int)(0xB0));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int RETURN = unchecked((int)(0xB1));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int GETSTATIC = unchecked((int)(0xB2));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int PUTSTATIC = unchecked((int)(0xB3));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int GETFIELD = unchecked((int)(0xB4));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int PUTFIELD = unchecked((int)(0xB5));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int INVOKEVIRTUAL = unchecked((int)(0xB6));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int INVOKESPECIAL = unchecked((int)(0xB7));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int INVOKESTATIC = unchecked((int)(0xB8));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int INVOKEINTERFACE = unchecked((int)(0xB9));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int NEW = unchecked((int)(0xBB));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int NEWARRAY = unchecked((int)(0xBC));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ANEWARRAY = unchecked((int)(0xBD));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ARRAYLENGTH = unchecked((int)(0xBE));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int ATHROW = unchecked((int)(0xBF));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int CHECKCAST = unchecked((int)(0xC0));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int INSTANCEOF = unchecked((int)(0xC1));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int MONITORENTER = unchecked((int)(0xC2));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int MONITOREXIT = unchecked((int)(0xC3));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int WIDE = unchecked((int)(0xC4));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int MULTIANEWARRAY = unchecked((int)(0xC5));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IFNULL = unchecked((int)(0xC6));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IFNONNULL = unchecked((int)(0xC7));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int GOTO_W = unchecked((int)(0xC8));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int JSR_W = unchecked((int)(0xC9));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int BREAKPOINT = unchecked((int)(0xCA));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IMPDEP1 = unchecked((int)(0xFE));

		/// <summary>The byte opcodes defined by the Java Virtual Machine.</summary>
		/// <remarks>The byte opcodes defined by the Java Virtual Machine.</remarks>
		public const int IMPDEP2 = unchecked((int)(0xFF));

		/// <summary>Types for the NEWARRAY opcode.</summary>
		/// <remarks>Types for the NEWARRAY opcode.</remarks>
		public const byte T_BOOLEAN = 4;

		/// <summary>Types for the NEWARRAY opcode.</summary>
		/// <remarks>Types for the NEWARRAY opcode.</remarks>
		public const byte T_CHAR = 5;

		/// <summary>Types for the NEWARRAY opcode.</summary>
		/// <remarks>Types for the NEWARRAY opcode.</remarks>
		public const byte T_FLOAT = 6;

		/// <summary>Types for the NEWARRAY opcode.</summary>
		/// <remarks>Types for the NEWARRAY opcode.</remarks>
		public const byte T_DOUBLE = 7;

		/// <summary>Types for the NEWARRAY opcode.</summary>
		/// <remarks>Types for the NEWARRAY opcode.</remarks>
		public const byte T_BYTE = 8;

		/// <summary>Types for the NEWARRAY opcode.</summary>
		/// <remarks>Types for the NEWARRAY opcode.</remarks>
		public const byte T_SHORT = 9;

		/// <summary>Types for the NEWARRAY opcode.</summary>
		/// <remarks>Types for the NEWARRAY opcode.</remarks>
		public const byte T_INT = 10;

		/// <summary>Types for the NEWARRAY opcode.</summary>
		/// <remarks>Types for the NEWARRAY opcode.</remarks>
		public const byte T_LONG = 11;
	}
}
