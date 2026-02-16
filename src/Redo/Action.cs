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

	public NewLineAction(int linePos, int columnPos) {
		this.linePos = linePos;
		this.columnPos = columnPos;
	}
	public override void Redo(Buffer buffer) {
		buffer.newLineAtPos(linePos, columnPos);
	}

	public override void Undo(Buffer buffer) {
		buffer.mergeLinesAtPos(linePos);
	}
}

class DeleteLineAction : Action {
	int linePos;

	public DeleteLineAction(int linePos) {

		this.linePos = linePos;
	}
	public override void Redo(Buffer buffer) {
		buffer.mergeLinesAtPos(linePos - 1);
	}

	public override void Undo(Buffer buffer) {
		buffer.newLineAtPos(linePos - 1, buffer.lines[linePos - 1].Count);
	}
}

class DeleteWhileSelecting : Action {
	(int startLine, int endLine, int startColumn, int endColumn) selectedArea;
	List<List<char>> deletedData;
	public DeleteWhileSelecting(Buffer buffer) {
		selectedArea = buffer.getSelectedArea();
		deletedData = new List<List<char>>();
		getDeletedData(selectedArea, buffer);
	}

	public void getDeletedData((int startLine, int endLine, int startColumn, int endColumn) selectedArea, Buffer buffer) {
		for (int i = selectedArea.startLine; i <= selectedArea.endLine; i++) {
			if (i == selectedArea.startLine) {
				List<char> firstCopiedLine = new List<char>();
				if (selectedArea.startLine == selectedArea.endLine) {
					firstCopiedLine = buffer.lines[selectedArea.startLine].GetRange(selectedArea.startColumn, selectedArea.endColumn - selectedArea.startColumn);
				}
				else {
					firstCopiedLine = buffer.lines[selectedArea.startLine].GetRange(selectedArea.startColumn, buffer.lines[selectedArea.startLine].Count - selectedArea.startColumn);
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

	public override void Redo(Buffer buffer) {
		buffer.setSelectedArea(selectedArea.startLine, selectedArea.endLine, selectedArea.startColumn, selectedArea.endColumn);
		buffer.removeSelectedArea();
	}

	public override void Undo(Buffer buffer) {
		buffer.insertLinesAtPos(selectedArea.startLine, selectedArea.startColumn, deletedData);
		buffer.setSelectedArea(selectedArea.startLine, selectedArea.endLine, selectedArea.startColumn, selectedArea.endColumn);
	}
}


