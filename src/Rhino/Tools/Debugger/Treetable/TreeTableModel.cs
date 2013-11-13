/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Javax.Swing.Tree;
using Rhino.Tools.Debugger.Treetable;
using Sharpen;

namespace Rhino.Tools.Debugger.Treetable
{
	/// <summary>TreeTableModel is the model used by a JTreeTable.</summary>
	/// <remarks>
	/// TreeTableModel is the model used by a JTreeTable. It extends TreeModel
	/// to add methods for getting inforamtion about the set of columns each
	/// node in the TreeTableModel may have. Each column, like a column in
	/// a TableModel, has a name and a type associated with it. Each node in
	/// the TreeTableModel can return a value for each of the columns and
	/// set that value if isCellEditable() returns true.
	/// </remarks>
	/// <author>Philip Milne</author>
	/// <author>Scott Violet</author>
	public interface TreeTableModel : TreeModel
	{
		/// <summary>Returns the number ofs availible column.</summary>
		/// <remarks>Returns the number ofs availible column.</remarks>
		int GetColumnCount();

		/// <summary>Returns the name for column number <code>column</code>.</summary>
		/// <remarks>Returns the name for column number <code>column</code>.</remarks>
		string GetColumnName(int column);

		/// <summary>Returns the type for column number <code>column</code>.</summary>
		/// <remarks>Returns the type for column number <code>column</code>.</remarks>
		Type GetColumnClass(int column);

		/// <summary>
		/// Returns the value to be displayed for node <code>node</code>,
		/// at column number <code>column</code>.
		/// </summary>
		/// <remarks>
		/// Returns the value to be displayed for node <code>node</code>,
		/// at column number <code>column</code>.
		/// </remarks>
		object GetValueAt(object node, int column);

		/// <summary>
		/// Indicates whether the the value for node <code>node</code>,
		/// at column number <code>column</code> is editable.
		/// </summary>
		/// <remarks>
		/// Indicates whether the the value for node <code>node</code>,
		/// at column number <code>column</code> is editable.
		/// </remarks>
		bool IsCellEditable(object node, int column);

		/// <summary>
		/// Sets the value for node <code>node</code>,
		/// at column number <code>column</code>.
		/// </summary>
		/// <remarks>
		/// Sets the value for node <code>node</code>,
		/// at column number <code>column</code>.
		/// </remarks>
		void SetValueAt(object aValue, object node, int column);
	}
}
