/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Tools;
using Rhino.Tools.Idswitch;
using Sharpen;

namespace Rhino.Tools.Idswitch
{
	public class SwitchGenerator
	{
		internal string v_switch_label = "L0";

		internal string v_label = "L";

		internal string v_s = "s";

		internal string v_c = "c";

		internal string v_guess = "X";

		internal string v_id = "id";

		internal string v_length_suffix = "_length";

		internal int use_if_threshold = 3;

		internal int char_tail_test_threshold = 2;

		private IdValuePair[] pairs;

		private string default_value;

		private int[] columns;

		private bool c_was_defined;

		private CodePrinter P;

		private ToolErrorReporter R;

		private string source_file;

		public virtual CodePrinter GetCodePrinter()
		{
			return P;
		}

		public virtual void SetCodePrinter(CodePrinter value)
		{
			P = value;
		}

		public virtual ToolErrorReporter GetReporter()
		{
			return R;
		}

		public virtual void SetReporter(ToolErrorReporter value)
		{
			R = value;
		}

		public virtual string GetSourceFileName()
		{
			return source_file;
		}

		public virtual void SetSourceFileName(string value)
		{
			source_file = value;
		}

		public virtual void GenerateSwitch(string[] pairs, string default_value)
		{
			int N = pairs.Length / 2;
			IdValuePair[] id_pairs = new IdValuePair[N];
			for (int i = 0; i != N; ++i)
			{
				id_pairs[i] = new IdValuePair(pairs[2 * i], pairs[2 * i + 1]);
			}
			GenerateSwitch(id_pairs, default_value);
		}

		public virtual void GenerateSwitch(IdValuePair[] pairs, string default_value)
		{
			int begin = 0;
			int end = pairs.Length;
			if (begin == end)
			{
				return;
			}
			this.pairs = pairs;
			this.default_value = default_value;
			Generate_body(begin, end, 2);
		}

		private void Generate_body(int begin, int end, int indent_level)
		{
			P.Indent(indent_level);
			P.P(v_switch_label);
			P.P(": { ");
			P.P(v_id);
			P.P(" = ");
			P.P(default_value);
			P.P("; String ");
			P.P(v_guess);
			P.P(" = null;");
			c_was_defined = false;
			int c_def_begin = P.GetOffset();
			P.P(" int ");
			P.P(v_c);
			P.P(';');
			int c_def_end = P.GetOffset();
			P.Nl();
			Generate_length_switch(begin, end, indent_level + 1);
			if (!c_was_defined)
			{
				P.Erase(c_def_begin, c_def_end);
			}
			P.Indent(indent_level + 1);
			P.P("if (");
			P.P(v_guess);
			P.P("!=null && ");
			P.P(v_guess);
			P.P("!=");
			P.P(v_s);
			P.P(" && !");
			P.P(v_guess);
			P.P(".equals(");
			P.P(v_s);
			P.P(")) ");
			P.P(v_id);
			P.P(" = ");
			P.P(default_value);
			P.P(";");
			P.Nl();
			// Add break at end of block to suppress warning for unused label
			P.Indent(indent_level + 1);
			P.P("break ");
			P.P(v_switch_label);
			P.P(";");
			P.Nl();
			P.Line(indent_level, "}");
		}

		private void Generate_length_switch(int begin, int end, int indent_level)
		{
			Sort_pairs(begin, end, -1);
			Check_all_is_different(begin, end);
			int lengths_count = Count_different_lengths(begin, end);
			columns = new int[pairs[end - 1].idLength];
			bool use_if;
			if (lengths_count <= use_if_threshold)
			{
				use_if = true;
				if (lengths_count != 1)
				{
					P.Indent(indent_level);
					P.P("int ");
					P.P(v_s);
					P.P(v_length_suffix);
					P.P(" = ");
					P.P(v_s);
					P.P(".length();");
					P.Nl();
				}
			}
			else
			{
				use_if = false;
				P.Indent(indent_level);
				P.P(v_label);
				P.P(": switch (");
				P.P(v_s);
				P.P(".length()) {");
				P.Nl();
			}
			int same_length_begin = begin;
			int cur_l = pairs[begin].idLength;
			int l = 0;
			for (int i = begin; ; )
			{
				++i;
				if (i == end || (l = pairs[i].idLength) != cur_l)
				{
					int next_indent;
					if (use_if)
					{
						P.Indent(indent_level);
						if (same_length_begin != begin)
						{
							P.P("else ");
						}
						P.P("if (");
						if (lengths_count == 1)
						{
							P.P(v_s);
							P.P(".length()==");
						}
						else
						{
							P.P(v_s);
							P.P(v_length_suffix);
							P.P("==");
						}
						P.P(cur_l);
						P.P(") {");
						next_indent = indent_level + 1;
					}
					else
					{
						P.Indent(indent_level);
						P.P("case ");
						P.P(cur_l);
						P.P(":");
						next_indent = indent_level + 1;
					}
					Generate_letter_switch(same_length_begin, i, next_indent, !use_if, use_if);
					if (use_if)
					{
						P.P("}");
						P.Nl();
					}
					else
					{
						P.P("break ");
						P.P(v_label);
						P.P(";");
						P.Nl();
					}
					if (i == end)
					{
						break;
					}
					same_length_begin = i;
					cur_l = l;
				}
			}
			if (!use_if)
			{
				P.Indent(indent_level);
				P.P("}");
				P.Nl();
			}
		}

		private void Generate_letter_switch(int begin, int end, int indent_level, bool label_was_defined, bool inside_if)
		{
			int L = pairs[begin].idLength;
			for (int i = 0; i != L; ++i)
			{
				columns[i] = i;
			}
			Generate_letter_switch_r(begin, end, L, indent_level, label_was_defined, inside_if);
		}

		private bool Generate_letter_switch_r(int begin, int end, int L, int indent_level, bool label_was_defined, bool inside_if)
		{
			bool next_is_unreachable = false;
			if (begin + 1 == end)
			{
				P.P(' ');
				IdValuePair pair = pairs[begin];
				if (L > char_tail_test_threshold)
				{
					P.P(v_guess);
					P.P("=");
					P.Qstring(pair.id);
					P.P(";");
					P.P(v_id);
					P.P("=");
					P.P(pair.value);
					P.P(";");
				}
				else
				{
					if (L == 0)
					{
						next_is_unreachable = true;
						P.P(v_id);
						P.P("=");
						P.P(pair.value);
						P.P("; break ");
						P.P(v_switch_label);
						P.P(";");
					}
					else
					{
						P.P("if (");
						int column = columns[0];
						P.P(v_s);
						P.P(".charAt(");
						P.P(column);
						P.P(")==");
						P.Qchar(pair.id[column]);
						for (int i = 1; i != L; ++i)
						{
							P.P(" && ");
							column = columns[i];
							P.P(v_s);
							P.P(".charAt(");
							P.P(column);
							P.P(")==");
							P.Qchar(pair.id[column]);
						}
						P.P(") {");
						P.P(v_id);
						P.P("=");
						P.P(pair.value);
						P.P("; break ");
						P.P(v_switch_label);
						P.P(";}");
					}
				}
				P.P(' ');
				return next_is_unreachable;
			}
			int max_column_index = Find_max_different_column(begin, end, L);
			int max_column = columns[max_column_index];
			int count = Count_different_chars(begin, end, max_column);
			columns[max_column_index] = columns[L - 1];
			if (inside_if)
			{
				P.Nl();
				P.Indent(indent_level);
			}
			else
			{
				P.P(' ');
			}
			bool use_if;
			if (count <= use_if_threshold)
			{
				use_if = true;
				c_was_defined = true;
				P.P(v_c);
				P.P("=");
				P.P(v_s);
				P.P(".charAt(");
				P.P(max_column);
				P.P(");");
			}
			else
			{
				use_if = false;
				if (!label_was_defined)
				{
					label_was_defined = true;
					P.P(v_label);
					P.P(": ");
				}
				P.P("switch (");
				P.P(v_s);
				P.P(".charAt(");
				P.P(max_column);
				P.P(")) {");
			}
			int same_char_begin = begin;
			int cur_ch = pairs[begin].id[max_column];
			int ch = 0;
			for (int i_1 = begin; ; )
			{
				++i_1;
				if (i_1 == end || (ch = pairs[i_1].id[max_column]) != cur_ch)
				{
					int next_indent;
					if (use_if)
					{
						P.Nl();
						P.Indent(indent_level);
						if (same_char_begin != begin)
						{
							P.P("else ");
						}
						P.P("if (");
						P.P(v_c);
						P.P("==");
						P.Qchar(cur_ch);
						P.P(") {");
						next_indent = indent_level + 1;
					}
					else
					{
						P.Nl();
						P.Indent(indent_level);
						P.P("case ");
						P.Qchar(cur_ch);
						P.P(":");
						next_indent = indent_level + 1;
					}
					bool after_unreachable = Generate_letter_switch_r(same_char_begin, i_1, L - 1, next_indent, label_was_defined, use_if);
					if (use_if)
					{
						P.P("}");
					}
					else
					{
						if (!after_unreachable)
						{
							P.P("break ");
							P.P(v_label);
							P.P(";");
						}
					}
					if (i_1 == end)
					{
						break;
					}
					same_char_begin = i_1;
					cur_ch = ch;
				}
			}
			if (use_if)
			{
				P.Nl();
				if (inside_if)
				{
					P.Indent(indent_level - 1);
				}
				else
				{
					P.Indent(indent_level);
				}
			}
			else
			{
				P.Nl();
				P.Indent(indent_level);
				P.P("}");
				if (inside_if)
				{
					P.Nl();
					P.Indent(indent_level - 1);
				}
				else
				{
					P.P(' ');
				}
			}
			columns[max_column_index] = max_column;
			return next_is_unreachable;
		}

		private int Count_different_lengths(int begin, int end)
		{
			int lengths_count = 0;
			int cur_l = -1;
			for (; begin != end; ++begin)
			{
				int l = pairs[begin].idLength;
				if (cur_l != l)
				{
					++lengths_count;
					cur_l = l;
				}
			}
			return lengths_count;
		}

		private int Find_max_different_column(int begin, int end, int L)
		{
			int max_count = 0;
			int max_index = 0;
			for (int i = 0; i != L; ++i)
			{
				int column = columns[i];
				Sort_pairs(begin, end, column);
				int count = Count_different_chars(begin, end, column);
				if (count == end - begin)
				{
					return i;
				}
				if (max_count < count)
				{
					max_count = count;
					max_index = i;
				}
			}
			if (max_index != L - 1)
			{
				Sort_pairs(begin, end, columns[max_index]);
			}
			return max_index;
		}

		private int Count_different_chars(int begin, int end, int column)
		{
			int chars_count = 0;
			int cur_ch = -1;
			for (; begin != end; ++begin)
			{
				int ch = pairs[begin].id[column];
				if (ch != cur_ch)
				{
					++chars_count;
					cur_ch = ch;
				}
			}
			return chars_count;
		}

		private void Check_all_is_different(int begin, int end)
		{
			if (begin != end)
			{
				IdValuePair prev = pairs[begin];
				while (++begin != end)
				{
					IdValuePair current = pairs[begin];
					if (prev.id.Equals(current.id))
					{
						throw On_same_pair_fail(prev, current);
					}
					prev = current;
				}
			}
		}

		private EvaluatorException On_same_pair_fail(IdValuePair a, IdValuePair b)
		{
			int line1 = a.GetLineNumber();
			int line2 = b.GetLineNumber();
			if (line2 > line1)
			{
				int tmp = line1;
				line1 = line2;
				line2 = tmp;
			}
			string error_text = ToolErrorReporter.GetMessage("msg.idswitch.same_string", a.id, line2);
			return R.RuntimeError(error_text, source_file, line1, null, 0);
		}

		private void Sort_pairs(int begin, int end, int comparator)
		{
			Heap4Sort(pairs, begin, end - begin, comparator);
		}

		private static bool Bigger(IdValuePair a, IdValuePair b, int comparator)
		{
			if (comparator < 0)
			{
				// For length selection switch it is enough to compare just length,
				// but to detect same strings full comparison is essential
				//return a.idLength > b.idLength;
				int diff = a.idLength - b.idLength;
				if (diff != 0)
				{
					return diff > 0;
				}
				return string.CompareOrdinal(a.id, b.id) > 0;
			}
			else
			{
				return a.id[comparator] > b.id[comparator];
			}
		}

		private static void Heap4Sort(IdValuePair[] array, int offset, int size, int comparator)
		{
			if (size <= 1)
			{
				return;
			}
			MakeHeap4(array, offset, size, comparator);
			while (size > 1)
			{
				--size;
				IdValuePair v1 = array[offset + size];
				IdValuePair v2 = array[offset + 0];
				array[offset + size] = v2;
				array[offset + 0] = v1;
				Heapify4(array, offset, size, 0, comparator);
			}
		}

		private static void MakeHeap4(IdValuePair[] array, int offset, int size, int comparator)
		{
			for (int i = ((size + 2) >> 2); i != 0; )
			{
				--i;
				Heapify4(array, offset, size, i, comparator);
			}
		}

		private static void Heapify4(IdValuePair[] array, int offset, int size, int i, int comparator)
		{
			int new_i1;
			int new_i2;
			int new_i3;
			IdValuePair i_val = array[offset + i];
			for (; ; )
			{
				int @base = (i << 2);
				new_i1 = @base | 1;
				new_i2 = @base | 2;
				new_i3 = @base | 3;
				int new_i4 = @base + 4;
				if (new_i4 >= size)
				{
					break;
				}
				IdValuePair val1 = array[offset + new_i1];
				IdValuePair val2 = array[offset + new_i2];
				IdValuePair val3 = array[offset + new_i3];
				IdValuePair val4 = array[offset + new_i4];
				if (Bigger(val2, val1, comparator))
				{
					val1 = val2;
					new_i1 = new_i2;
				}
				if (Bigger(val4, val3, comparator))
				{
					val3 = val4;
					new_i3 = new_i4;
				}
				if (Bigger(val3, val1, comparator))
				{
					val1 = val3;
					new_i1 = new_i3;
				}
				if (Bigger(i_val, val1, comparator))
				{
					return;
				}
				array[offset + i] = val1;
				array[offset + new_i1] = i_val;
				i = new_i1;
			}
			if (new_i1 < size)
			{
				IdValuePair val1 = array[offset + new_i1];
				if (new_i2 != size)
				{
					IdValuePair val2 = array[offset + new_i2];
					if (Bigger(val2, val1, comparator))
					{
						val1 = val2;
						new_i1 = new_i2;
					}
					if (new_i3 != size)
					{
						IdValuePair val3 = array[offset + new_i3];
						if (Bigger(val3, val1, comparator))
						{
							val1 = val3;
							new_i1 = new_i3;
						}
					}
				}
				if (Bigger(val1, i_val, comparator))
				{
					array[offset + i] = val1;
					array[offset + new_i1] = i_val;
				}
			}
		}
	}
}
