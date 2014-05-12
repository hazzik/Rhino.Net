using System.Reflection.Emit;

namespace Rhino
{
	internal static class ILGeneratorEx
	{
		public static void EmitLoadLocal(this ILGenerator il, int local)
		{
			if (local == 0)
			{
				il.Emit(OpCodes.Ldloc_0);
			}
			else if (local == 1)
			{
				il.Emit(OpCodes.Ldloc_1);
			}
			else if (local == 2)
			{
				il.Emit(OpCodes.Ldloc_2);
			}
			else if (local == 3)
			{
				il.Emit(OpCodes.Ldloc_3);
			}
			else if (local < 256)
			{
				il.Emit(OpCodes.Ldloc_S, (byte) local);
			}
			else
			{
				il.Emit(OpCodes.Ldloc, local);
			}
		}

		public static void EmitLoadArgument(this ILGenerator il, int argument)
		{
			if (argument == 0)
			{
				il.Emit(OpCodes.Ldarg_0);
			}
			else if (argument == 1)
			{
				il.Emit(OpCodes.Ldarg_1);
			}
			else if (argument == 2)
			{
				il.Emit(OpCodes.Ldarg_2);
			}
			else if (argument == 3)
			{
				il.Emit(OpCodes.Ldarg_3);
			}
			else if (argument < 256)
			{
				il.Emit(OpCodes.Ldarg_S, (byte)argument);
			}
			else
			{
				il.Emit(OpCodes.Ldarg, argument);
			}
		}

		public static void EmitStoreArgument(this ILGenerator il, int argument)
		{
			if (argument < 256)
			{
				il.Emit(OpCodes.Starg_S, (byte) argument);
			}
			else
			{
				il.Emit(OpCodes.Starg, argument);
			}
		}

		public static void EmitStoreLocal(this ILGenerator il, int local)
		{
			if (local == 0)
			{
				il.Emit(OpCodes.Stloc_0);
			}
			else if (local == 1)
			{
				il.Emit(OpCodes.Stloc_1);
			}
			else if (local == 2)
			{
				il.Emit(OpCodes.Stloc_2);
			}
			else if (local == 3)
			{
				il.Emit(OpCodes.Stloc_3);
			}
			else if (local < 256)
			{
				il.Emit(OpCodes.Stloc_S, (byte) local);
			}
			else
			{
				il.Emit(OpCodes.Stloc, (short) local);
			}
		}

		public static void EmitLoadConstant(this ILGenerator il, string value)
		{
			il.Emit(OpCodes.Ldstr, value);
		}

		public static void EmitLoadConstant(this ILGenerator il, int value)
		{
			if (value == -1)
			{
				il.Emit(OpCodes.Ldc_I4_M1);
			}
			else if (value == 0)
			{
				il.Emit(OpCodes.Ldc_I4_0);
			}
			else if (value == 1)
			{
				il.Emit(OpCodes.Ldc_I4_1);
			}
			else if (value == 2)
			{
				il.Emit(OpCodes.Ldc_I4_2);
			}
			else if (value == 3)
			{
				il.Emit(OpCodes.Ldc_I4_3);
			}
			else if (value == 4)
			{
				il.Emit(OpCodes.Ldc_I4_4);
			}
			else if (value == 5)
			{
				il.Emit(OpCodes.Ldc_I4_5);
			}
			else if (value == 6)
			{
				il.Emit(OpCodes.Ldc_I4_6);
			}
			else if (value == 7)
			{
				il.Emit(OpCodes.Ldc_I4_7);
			}
			else if (value == 8)
			{
				il.Emit(OpCodes.Ldc_I4_8);
			}
			else
			{
				il.Emit(OpCodes.Ldc_I4, value);
			}
		}

		public static void EmitLoadConstant(this ILGenerator il, long value)
		{
			il.Emit(OpCodes.Ldc_I8, value);
		}

		public static void EmitLoadConstant(this ILGenerator il, double value)
		{
			il.Emit(OpCodes.Ldc_R8, value);
		}

		public static void EmitLoadConstant(this ILGenerator il, bool value)
		{
			il.Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
		}

		public static void EmitStoreLocal(this ILGenerator il, LocalBuilder local)
		{
			il.Emit(OpCodes.Stloc, local);
		}

		public static void EmitLoadLocal(this ILGenerator il, LocalBuilder local)
		{
			il.Emit(OpCodes.Ldloc, local);
		}

		public static Label[] DefineSwitchTable(this ILGenerator il, int count)
		{
			var switchTable = new Label[count];
			for (int i = 0; i < count; i++)
			{
				switchTable[i] = il.DefineLabel();
			}
			return switchTable;
		}

		public static void EmitDupX1(this ILGenerator il)
		{
			//il.Emit(ByteCode.DUP_X1);
			var value1 = il.DeclareLocal(typeof (object));
			var value2 = il.DeclareLocal(typeof (object));
			il.Emit(OpCodes.Stloc, value1);
			il.Emit(OpCodes.Stloc, value2);
			il.Emit(OpCodes.Ldloc, value1);
			il.Emit(OpCodes.Ldloc, value2);
			il.Emit(OpCodes.Ldloc, value1);
		}
	}
}