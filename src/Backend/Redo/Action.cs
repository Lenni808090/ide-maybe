using Microsoft.VisualBasic;

abstract class Action {

	public abstract void Undo(Buffer buffer);

	public abstract void Redo(Buffer buffer);
}

class InsertCharAction : Action {
	char insertedChar;
	int columnPos;
	int linePos;

	public InsertCharAction(char insertedChar, int columnPos, int linePos) {
		this.insertedChar = insertedChar;
		this.columnPos = columnPos;
		this.linePos = linePos;
	}
	public override void Redo(Buffer buffer) {
		buffer.insertCharAtPos(insertedChar, columnPos, linePos);
	}

	public override void Undo(Buffer buffer) {
		buffer.removeCharAtPos(columnPos, linePos);
	}
}

class InsertCharPairAction : Action {
	char insertedChar;
	int columnPos;
	int linePos;

	public InsertCharPairAction(char insertedChar, int columnPos, int linePos) {
		this.insertedChar = insertedChar;
		this.columnPos = columnPos;
		this.linePos = linePos;
	}
	public override void Redo(Buffer buffer) {
		buffer.insertCharPairAtPos(insertedChar, columnPos, linePos);
	}

	public override void Undo(Buffer buffer) {
		buffer.removeCharAtPos(columnPos, linePos);
		buffer.removeCharAtPos(columnPos + 1, linePos);
	}
}

class InsertCharPairActionWhileSelecting : Action {
	char insertedChar;

	(int startLine, int endLine, int startColumn, int endColumn) before;

	public InsertCharPairActionWhileSelecting(Buffer buffer, char insertedChar) {
		this.insertedChar = insertedChar;
		before = buffer.getSelectedArea();
	}

	public override void Redo(Buffer buffer) {
		buffer.insertCharPairArroundSelection(
			insertedChar,
			before.startColumn,
			before.startLine,
			before.endLine,
			before.endColumn
		);

	}

	public override void Undo(Buffer buffer) {
		int openPos = before.startColumn;
		int closePos = before.startLine == before.endLine
			? before.endColumn + 1
			: before.endColumn;

		buffer.removeCharAtPos(openPos, before.startLine);
		buffer.removeCharAtPos(closePos, before.endLine);
		buffer.stopSelecting();
	}
}


class InsertTabAction : Action {
	int columnPos;
	int linePos;

	public InsertTabAction(int columnPos, int linePos) {
		this.columnPos = columnPos;
		this.linePos = linePos;
	}
	public override void Redo(Buffer buffer) {
		buffer.insertTabAtPos(columnPos, linePos, 4);
	}

	public override void Undo(Buffer buffer) {
		buffer.removeTabAtPos(columnPos + 4, linePos);
	}
}

class DeleteCharAction : Action {

	char deletedChar;
	int columnPos;
	int linePos;

	public DeleteCharAction(char deletedChar, int columnPos, int linePos) {
		this.deletedChar = deletedChar;
		this.columnPos = columnPos;
		this.linePos = linePos;
	}
	public override void Redo(Buffer buffer) {
		buffer.removeCharAtPos(columnPos, linePos);
	}

	public override void Undo(Buffer buffer) {
		buffer.insertCharAtPos(deletedChar, columnPos, linePos);
	}
}

class DeleteTabAction : Action {
	int columnPos;
	int linePos;

	public DeleteTabAction(int columnPos, int linePos) {
		this.columnPos = columnPos;
		this.linePos = linePos;
	}
	public override void Redo(Buffer buffer) {
		buffer.removeTabAtPos(columnPos, linePos);
	}

	public override void Undo(Buffer buffer) {
		buffer.insertTabAtPos(columnPos - 4, linePos, 4);
	}
}

class NewLineAction : Action {
	int linePos;
	int columnPos;
	int insertedIndentCount;

	public NewLineAction(int linePos, int columnPos, Buffer buffer) {
		this.linePos = linePos;
		this.columnPos = columnPos;
		insertedIndentCount = Math.Min(buffer.getPrevWhiteSpaces(linePos), columnPos);
	}
	public override void Redo(Buffer buffer) {
		buffer.newLineAtPos(linePos, columnPos);
	}

	public override void Undo(Buffer buffer) {
		buffer.mergeLinesAtPos(linePos, insertedIndentCount);
	}
}

class DeleteLineAction : Action {
	int linePos;
	int deletedIntoLineLength;
	public DeleteLineAction(int linePos, Buffer buffer) {
		this.linePos = linePos;
		deletedIntoLineLength = buffer.lines[linePos - 1].Count;
	}
	public override void Redo(Buffer buffer) {
		buffer.mergeLinesAtPos(linePos - 1);
	}

	public override void Undo(Buffer buffer) {
		buffer.newLineAtPosRaw(linePos - 1, deletedIntoLineLength);
	}
}

abstract class SelectionAction : Action {
	protected (int startLine, int endLine, int startColumn, int endColumn) selectedArea;
	protected List<List<char>> deletedData;

	protected SelectionAction(Buffer buffer) {
		selectedArea = buffer.getSelectedArea();
		deletedData = new List<List<char>>();
		getDeletedData(selectedArea, buffer);
	}

	protected void getDeletedData((int startLine, int endLine, int startColumn, int endColumn) selectedArea, Buffer buffer) {
		for (int i = selectedArea.startLine; i <= selectedArea.endLine; i++) {
			if (i == selectedArea.startLine) {
				List<char> firstCopiedLine = new List<char>();
				if (selectedArea.startLine == selectedArea.endLine) {
					firstCopiedLine = buffer.lines[selectedArea.startLine]
						.GetRange(selectedArea.startColumn, selectedArea.endColumn - selectedArea.startColumn);
				}
				else {
					firstCopiedLine = buffer.lines[selectedArea.startLine]
						.GetRange(selectedArea.startColumn, buffer.lines[selectedArea.startLine].Count - selectedArea.startColumn);
				}
				deletedData.Add(firstCopiedLine);
			}
			else if (i == selectedArea.endLine) {
				List<char> lastLineCopied = buffer.lines[selectedArea.endLine].GetRange(0, selectedArea.endColumn);
				deletedData.Add(lastLineCopied);
			}
			else {
				deletedData.Add(new List<char>(buffer.lines[i]));
			}
		}
	}
}


class DeleteWhileSelecting : SelectionAction {
	public DeleteWhileSelecting(Buffer buffer) : base(buffer) { }

	public override void Redo(Buffer buffer) {
		buffer.setSelectedArea(selectedArea.startLine, selectedArea.endLine, selectedArea.startColumn, selectedArea.endColumn);
		buffer.removeSelectedArea();
		buffer.stopSelecting();
	}

	public override void Undo(Buffer buffer) {
		buffer.insertLinesAtPos(selectedArea.startLine, selectedArea.startColumn, deletedData);
		buffer.setSelectedArea(selectedArea.startLine, selectedArea.endLine, selectedArea.startColumn, selectedArea.endColumn);
	}
}

class NewLineWhileSelecting : SelectionAction {

	int insertedIndentCount;
	public NewLineWhileSelecting(Buffer buffer) : base(buffer) {
		insertedIndentCount = Math.Min(buffer.getPrevWhiteSpaces(selectedArea.endLine), selectedArea.endColumn);
	}

	public override void Redo(Buffer buffer) {
		buffer.setSelectedArea(selectedArea.startLine, selectedArea.endLine, selectedArea.startColumn, selectedArea.endColumn);
		buffer.newLineAtPos(selectedArea.endLine, selectedArea.endColumn);
		buffer.removeSelectedArea();
		buffer.stopSelecting();
	}

	public override void Undo(Buffer buffer) {
		buffer.mergeLinesAtPos(selectedArea.endLine, insertedIndentCount);
		buffer.insertLinesAtPos(selectedArea.startLine, selectedArea.startColumn, deletedData);
		buffer.setSelectedArea(selectedArea.startLine, selectedArea.endLine, selectedArea.startColumn, selectedArea.endColumn);
	}
}

class InsertCharWhileSelecting : SelectionAction {
	char insertedChar;
	int columnPos;
	int linePos;

	public InsertCharWhileSelecting(Buffer buffer, char insertedChar) : base(buffer) {
		this.insertedChar = insertedChar;
		columnPos = selectedArea.startColumn;
		linePos = selectedArea.startLine;
	}

	public override void Redo(Buffer buffer) {
		buffer.setSelectedArea(selectedArea.startLine, selectedArea.endLine, selectedArea.startColumn, selectedArea.endColumn);
		buffer.removeSelectedArea();
		buffer.stopSelecting();
		buffer.insertCharAtPos(insertedChar, columnPos, linePos);
	}

	public override void Undo(Buffer buffer) {
		buffer.removeCharAtPos(columnPos, linePos);
		buffer.insertLinesAtPos(selectedArea.startLine, selectedArea.startColumn, deletedData);
		buffer.setSelectedArea(selectedArea.startLine, selectedArea.endLine, selectedArea.startColumn, selectedArea.endColumn);
	}
}

class PasteDataWhileSelecting : SelectionAction {

	int columnPos;
	int linePos;
	(int startLine, int endLine, int startColumn, int endColumn) pastedArea;
	List<List<char>> pastedData;
	public PasteDataWhileSelecting(List<List<char>> pastedData, Buffer buffer) : base(buffer) {

		this.pastedData = pastedData;
		columnPos = selectedArea.startColumn;
		linePos = selectedArea.startLine;
		getPastedArea(buffer);
	}

	public void getPastedArea(Buffer buffer) {
		var pastedDataNoTab = buffer.convertTabsToSpace(pastedData);
		pastedArea.startLine = linePos;
		pastedArea.startColumn = columnPos;
		pastedArea.endLine = linePos + pastedDataNoTab.Count - 1;
		if (pastedArea.startLine == pastedArea.endLine) {
			pastedArea.endColumn = pastedDataNoTab[pastedDataNoTab.Count - 1].Count + pastedArea.startColumn;
		}
		else {
			pastedArea.endColumn = pastedDataNoTab[pastedDataNoTab.Count - 1].Count;
		}
	}

	public override void Redo(Buffer buffer) {
		buffer.setSelectedArea(selectedArea.startLine, selectedArea.endLine, selectedArea.startColumn, selectedArea.endColumn);
		buffer.removeSelectedArea();
		buffer.stopSelecting();
		buffer.insertLinesAtPos(linePos, columnPos, pastedData);
	}

	public override void Undo(Buffer buffer) {
		buffer.setSelectedArea(pastedArea.startLine, pastedArea.endLine, pastedArea.startColumn, pastedArea.endColumn);
		buffer.removeSelectedArea();
		buffer.stopSelecting();
		buffer.insertLinesAtPos(selectedArea.startLine, selectedArea.startColumn, deletedData);
		buffer.setSelectedArea(selectedArea.startLine, selectedArea.endLine, selectedArea.startColumn, selectedArea.endColumn);
	}
}


class PasteDataAction : Action {
	int columnPos;
	int linePos;
	(int startLine, int endLine, int startColumn, int endColumn) pastedArea;
	List<List<char>> pastedData;

	public PasteDataAction(List<List<char>> pastedData, int columnPos, int linePos, Buffer buffer) {
		this.pastedData = pastedData;
		this.columnPos = columnPos;
		this.linePos = linePos;
		getPastedArea(buffer);
	}

	public void getPastedArea(Buffer buffer) {
		var pastedDataNoTab = buffer.convertTabsToSpace(pastedData);
		pastedArea.startLine = linePos;
		pastedArea.startColumn = columnPos;
		pastedArea.endLine = linePos + pastedDataNoTab.Count - 1;
		if (pastedArea.startLine == pastedArea.endLine) {
			pastedArea.endColumn = pastedDataNoTab[pastedDataNoTab.Count - 1].Count + pastedArea.startColumn;
		}
		else {
			pastedArea.endColumn = pastedDataNoTab[pastedDataNoTab.Count - 1].Count;
		}
	}

	public override void Redo(Buffer buffer) {
		buffer.insertLinesAtPos(linePos, columnPos, pastedData);
	}

	public override void Undo(Buffer buffer) {
		buffer.setSelectedArea(pastedArea.startLine, pastedArea.endLine, pastedArea.startColumn, pastedArea.endColumn);
		buffer.removeSelectedArea();
		buffer.stopSelecting();
	}
}

