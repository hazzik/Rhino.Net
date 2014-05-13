using System.Reflection.Emit;

namespace Org.Mozilla.Classfile
{
	/// <summary>This class provides opcode values expected by the JVM in Java class files.</summary>
	/// <remarks>
	/// This class provides opcode values expected by the JVM in Java class files.
	/// It also provides tables for internal use by the ClassFileWriter.
	/// </remarks>
	/// <author>Roger Lawrence</author>
	public static class ByteCode
	{
		public static OpCode DUP2_X1
		{
			get { return OpCodes.Nop; }
		}

		public static OpCode SWAP
		{
			get { return OpCodes.Nop; }
		}

		public static OpCode IF_ACMPNE
		{
			get { return OpCodes.Nop; }
		}
	}
}
