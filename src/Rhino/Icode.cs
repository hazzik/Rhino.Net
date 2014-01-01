/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>Additional interpreter-specific codes</summary>
	public abstract class Icode
	{
		internal const int Icode_DELNAME = 0;

		internal const int Icode_DUP = -1;

		internal const int Icode_DUP2 = -2;

		internal const int Icode_SWAP = -3;

		internal const int Icode_POP = -4;

		internal const int Icode_POP_RESULT = -5;

		internal const int Icode_IFEQ_POP = -6;

		internal const int Icode_VAR_INC_DEC = -7;

		internal const int Icode_NAME_INC_DEC = -8;

		internal const int Icode_PROP_INC_DEC = -9;

		internal const int Icode_ELEM_INC_DEC = -10;

		internal const int Icode_REF_INC_DEC = -11;

		internal const int Icode_SCOPE_LOAD = -12;

		internal const int Icode_SCOPE_SAVE = -13;

		internal const int Icode_TYPEOFNAME = -14;

		internal const int Icode_NAME_AND_THIS = -15;

		internal const int Icode_PROP_AND_THIS = -16;

		internal const int Icode_ELEM_AND_THIS = -17;

		internal const int Icode_VALUE_AND_THIS = -18;

		internal const int Icode_CLOSURE_EXPR = -19;

		internal const int Icode_CLOSURE_STMT = -20;

		internal const int Icode_CALLSPECIAL = -21;

		internal const int Icode_RETUNDEF = -22;

		internal const int Icode_GOSUB = -23;

		internal const int Icode_STARTSUB = -24;

		internal const int Icode_RETSUB = -25;

		internal const int Icode_LINE = -26;

		internal const int Icode_SHORTNUMBER = -27;

		internal const int Icode_INTNUMBER = -28;

		internal const int Icode_LITERAL_NEW = -29;

		internal const int Icode_LITERAL_SET = -30;

		internal const int Icode_SPARE_ARRAYLIT = -31;

		internal const int Icode_REG_IND_C0 = -32;

		internal const int Icode_REG_IND_C1 = -33;

		internal const int Icode_REG_IND_C2 = -34;

		internal const int Icode_REG_IND_C3 = -35;

		internal const int Icode_REG_IND_C4 = -36;

		internal const int Icode_REG_IND_C5 = -37;

		internal const int Icode_REG_IND1 = -38;

		internal const int Icode_REG_IND2 = -39;

		internal const int Icode_REG_IND4 = -40;

		internal const int Icode_REG_STR_C0 = -41;

		internal const int Icode_REG_STR_C1 = -42;

		internal const int Icode_REG_STR_C2 = -43;

		internal const int Icode_REG_STR_C3 = -44;

		internal const int Icode_REG_STR1 = -45;

		internal const int Icode_REG_STR2 = -46;

		internal const int Icode_REG_STR4 = -47;

		internal const int Icode_GETVAR1 = -48;

		internal const int Icode_SETVAR1 = -49;

		internal const int Icode_UNDEF = -50;

		internal const int Icode_ZERO = -51;

		internal const int Icode_ONE = -52;

		internal const int Icode_ENTERDQ = -53;

		internal const int Icode_LEAVEDQ = -54;

		internal const int Icode_TAIL_CALL = -55;

		internal const int Icode_LOCAL_CLEAR = -56;

		internal const int Icode_LITERAL_GETTER = -57;

		internal const int Icode_LITERAL_SETTER = -58;

		internal const int Icode_SETCONST = -59;

		internal const int Icode_SETCONSTVAR = -60;

		internal const int Icode_SETCONSTVAR1 = -61;

		internal const int Icode_GENERATOR = -62;

		internal const int Icode_GENERATOR_END = -63;

		internal const int Icode_DEBUGGER = -64;

		internal const int MIN_ICODE = -64;

		// delete operator used on a name
		// Stack: ... value1 -> ... value1 value1
		// Stack: ... value2 value1 -> ... value2 value1 value2 value1
		// Stack: ... value2 value1 -> ... value1 value2
		// Stack: ... value1 -> ...
		// Store stack top into return register and then pop it
		// To jump conditionally and pop additional stack value
		// various types of ++/--
		// load/save scope from/to local
		// helper for function calls
		// Create closure object for nested functions
		// Special calls
		// To return undefined value
		// Exception handling implementation
		// To indicating a line number change in icodes.
		// To store shorts and ints inline
		// To create and populate array to hold values for [] and {} literals
		// Array literal with skipped index like [1,,2]
		// Load index register to prepare for the following index operation
		// Load string register to prepare for the following string operation
		// Version of getvar/setvar that read var index directly from bytecode
		// Load undefined
		// entrance and exit from .()
		// Clear local to allow GC its context
		// Literal get/set
		// const
		// Generator opcodes (along with Token.YIELD)
		// Last icode
		internal static string BytecodeName(int bytecode)
		{
			if (!ValidBytecode(bytecode))
			{
				throw new ArgumentException(bytecode.ToString());
			}
			return bytecode.ToString();
			if (ValidTokenCode(bytecode))
			{
				return Token.Name(bytecode);
			}
			switch (bytecode)
			{
				case Icode_DUP:
				{
					return "DUP";
				}

				case Icode_DUP2:
				{
					return "DUP2";
				}

				case Icode_SWAP:
				{
					return "SWAP";
				}

				case Icode_POP:
				{
					return "POP";
				}

				case Icode_POP_RESULT:
				{
					return "POP_RESULT";
				}

				case Icode_IFEQ_POP:
				{
					return "IFEQ_POP";
				}

				case Icode_VAR_INC_DEC:
				{
					return "VAR_INC_DEC";
				}

				case Icode_NAME_INC_DEC:
				{
					return "NAME_INC_DEC";
				}

				case Icode_PROP_INC_DEC:
				{
					return "PROP_INC_DEC";
				}

				case Icode_ELEM_INC_DEC:
				{
					return "ELEM_INC_DEC";
				}

				case Icode_REF_INC_DEC:
				{
					return "REF_INC_DEC";
				}

				case Icode_SCOPE_LOAD:
				{
					return "SCOPE_LOAD";
				}

				case Icode_SCOPE_SAVE:
				{
					return "SCOPE_SAVE";
				}

				case Icode_TYPEOFNAME:
				{
					return "TYPEOFNAME";
				}

				case Icode_NAME_AND_THIS:
				{
					return "NAME_AND_THIS";
				}

				case Icode_PROP_AND_THIS:
				{
					return "PROP_AND_THIS";
				}

				case Icode_ELEM_AND_THIS:
				{
					return "ELEM_AND_THIS";
				}

				case Icode_VALUE_AND_THIS:
				{
					return "VALUE_AND_THIS";
				}

				case Icode_CLOSURE_EXPR:
				{
					return "CLOSURE_EXPR";
				}

				case Icode_CLOSURE_STMT:
				{
					return "CLOSURE_STMT";
				}

				case Icode_CALLSPECIAL:
				{
					return "CALLSPECIAL";
				}

				case Icode_RETUNDEF:
				{
					return "RETUNDEF";
				}

				case Icode_GOSUB:
				{
					return "GOSUB";
				}

				case Icode_STARTSUB:
				{
					return "STARTSUB";
				}

				case Icode_RETSUB:
				{
					return "RETSUB";
				}

				case Icode_LINE:
				{
					return "LINE";
				}

				case Icode_SHORTNUMBER:
				{
					return "SHORTNUMBER";
				}

				case Icode_INTNUMBER:
				{
					return "INTNUMBER";
				}

				case Icode_LITERAL_NEW:
				{
					return "LITERAL_NEW";
				}

				case Icode_LITERAL_SET:
				{
					return "LITERAL_SET";
				}

				case Icode_SPARE_ARRAYLIT:
				{
					return "SPARE_ARRAYLIT";
				}

				case Icode_REG_IND_C0:
				{
					return "REG_IND_C0";
				}

				case Icode_REG_IND_C1:
				{
					return "REG_IND_C1";
				}

				case Icode_REG_IND_C2:
				{
					return "REG_IND_C2";
				}

				case Icode_REG_IND_C3:
				{
					return "REG_IND_C3";
				}

				case Icode_REG_IND_C4:
				{
					return "REG_IND_C4";
				}

				case Icode_REG_IND_C5:
				{
					return "REG_IND_C5";
				}

				case Icode_REG_IND1:
				{
					return "LOAD_IND1";
				}

				case Icode_REG_IND2:
				{
					return "LOAD_IND2";
				}

				case Icode_REG_IND4:
				{
					return "LOAD_IND4";
				}

				case Icode_REG_STR_C0:
				{
					return "REG_STR_C0";
				}

				case Icode_REG_STR_C1:
				{
					return "REG_STR_C1";
				}

				case Icode_REG_STR_C2:
				{
					return "REG_STR_C2";
				}

				case Icode_REG_STR_C3:
				{
					return "REG_STR_C3";
				}

				case Icode_REG_STR1:
				{
					return "LOAD_STR1";
				}

				case Icode_REG_STR2:
				{
					return "LOAD_STR2";
				}

				case Icode_REG_STR4:
				{
					return "LOAD_STR4";
				}

				case Icode_GETVAR1:
				{
					return "GETVAR1";
				}

				case Icode_SETVAR1:
				{
					return "SETVAR1";
				}

				case Icode_UNDEF:
				{
					return "UNDEF";
				}

				case Icode_ZERO:
				{
					return "ZERO";
				}

				case Icode_ONE:
				{
					return "ONE";
				}

				case Icode_ENTERDQ:
				{
					return "ENTERDQ";
				}

				case Icode_LEAVEDQ:
				{
					return "LEAVEDQ";
				}

				case Icode_TAIL_CALL:
				{
					return "TAIL_CALL";
				}

				case Icode_LOCAL_CLEAR:
				{
					return "LOCAL_CLEAR";
				}

				case Icode_LITERAL_GETTER:
				{
					return "LITERAL_GETTER";
				}

				case Icode_LITERAL_SETTER:
				{
					return "LITERAL_SETTER";
				}

				case Icode_SETCONST:
				{
					return "SETCONST";
				}

				case Icode_SETCONSTVAR:
				{
					return "SETCONSTVAR";
				}

				case Icode_SETCONSTVAR1:
				{
					return "SETCONSTVAR1";
				}

				case Icode_GENERATOR:
				{
					return "GENERATOR";
				}

				case Icode_GENERATOR_END:
				{
					return "GENERATOR_END";
				}

				case Icode_DEBUGGER:
				{
					return "DEBUGGER";
				}
			}
			// icode without name
			throw new InvalidOperationException(bytecode.ToString());
		}

		internal static bool ValidIcode(int icode)
		{
			return MIN_ICODE <= icode && icode <= 0;
		}

		internal static bool ValidTokenCode(int token)
		{
			return Token.FIRST_BYTECODE_TOKEN <= token && token <= Token.LAST_BYTECODE_TOKEN;
		}

		internal static bool ValidBytecode(int bytecode)
		{
			return ValidIcode(bytecode) || ValidTokenCode(bytecode);
		}
	}
}
