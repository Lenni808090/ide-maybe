using System.Reflection.PortableExecutable;
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
		buffer.InsertCharAtPos(insertedChar, columnPos, linePos);
	}

	public override void Undo(Buffer buffer) {
		buffer.RemoveCharAtPos(columnPos, linePos);
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
		buffer.InsertCharPairAtPos(insertedChar, columnPos, linePos);
	}

	public override void Undo(Buffer buffer) {
		buffer.RemoveCharAtPos(columnPos, linePos);
		buffer.RemoveCharAtPos(columnPos + 1, linePos);
	}
}

class InsertCharPairActionWhileSelecting : Action {
	char insertedChar;

	(int startLine, int endLine, int startColumn, int endColumn) before;

	public InsertCharPairActionWhileSelecting(Buffer buffer, char insertedChar) {
		this.insertedChar = insertedChar;
		before = buffer.GetSelectedArea();
	}

	public override void Redo(Buffer buffer) {
		buffer.InsertCharPairAroundSelection(
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

		buffer.RemoveCharAtPos(openPos, before.startLine);
		buffer.RemoveCharAtPos(closePos, before.endLine);
		buffer.StopSelecting();
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
		buffer.InsertTabAtPos(columnPos, linePos, 4);
	}

	public override void Undo(Buffer buffer) {
		buffer.RemoveTabAtPos(columnPos + 4, linePos);
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
		buffer.RemoveCharAtPos(columnPos, linePos);
	}

	public override void Undo(Buffer buffer) {
		buffer.InsertCharAtPos(deletedChar, columnPos, linePos);
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
		buffer.RemoveTabAtPos(columnPos, linePos);
	}

	public override void Undo(Buffer buffer) {
		buffer.InsertTabAtPos(columnPos - 4, linePos, 4);
	}
}

class NewLineAction : Action {
	int linePos;
	int columnPos;
	int insertedIndentCount;

	public NewLineAction(int linePos, int columnPos, Buffer buffer) {
		this.linePos = linePos;
		this.columnPos = columnPos;
		insertedIndentCount = Math.Min(buffer.GetPrevWhiteSpaces(linePos), columnPos);
	}
	public override void Redo(Buffer buffer) {
		buffer.NewLineAtPos(linePos, columnPos);
	}

	public override void Undo(Buffer buffer) {
		buffer.MergeLinesAtPos(linePos, insertedIndentCount);
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
		buffer.MergeLinesAtPos(linePos - 1);
	}

	public override void Undo(Buffer buffer) {
		buffer.NewLineAtPosRaw(linePos - 1, deletedIntoLineLength);
	}
}

abstract class SelectionAction : Action {
	protected (int startLine, int endLine, int startColumn, int endColumn) selectedArea;
	protected List<List<char>> deletedData;

	protected SelectionAction(Buffer buffer) {
		selectedArea = buffer.GetSelectedArea();
		deletedData = buffer.GetAreaData(
			selectedArea.startLine,
			selectedArea.endLine,
			selectedArea.startColumn,
			selectedArea.endColumn
		);
	}

	internal static void RemoveAreaAndSetCursor(
		Buffer buffer,
		(int startLine, int endLine, int startColumn, int endColumn) area
	) {
		buffer.RemoveArea(area.startLine, area.endLine, area.startColumn, area.endColumn);
		buffer.line = area.startLine;
		buffer.column = area.startColumn;
		buffer.prefColumn = buffer.column;
		buffer.ClampCursor();
	}
}


class DeleteWhileSelecting : SelectionAction {
	public DeleteWhileSelecting(Buffer buffer) : base(buffer) { }

	public override void Redo(Buffer buffer) {
		RemoveAreaAndSetCursor(buffer, selectedArea);
		buffer.StopSelecting();
	}

	public override void Undo(Buffer buffer) {
		buffer.InsertLinesAtPos(selectedArea.startLine, selectedArea.startColumn, deletedData);
		buffer.SetSelectedArea(selectedArea.startLine, selectedArea.endLine, selectedArea.startColumn, selectedArea.endColumn);
	}
}

class NewLineWhileSelecting : SelectionAction {

	int insertedIndentCount;
	public NewLineWhileSelecting(Buffer buffer) : base(buffer) {
		insertedIndentCount = Math.Min(buffer.GetPrevWhiteSpaces(selectedArea.endLine), selectedArea.endColumn);
	}

	public override void Redo(Buffer buffer) {
		buffer.NewLineAtPos(selectedArea.endLine, selectedArea.endColumn);
		RemoveAreaAndSetCursor(buffer, selectedArea);
		buffer.StopSelecting();
	}

	public override void Undo(Buffer buffer) {
		buffer.MergeLinesAtPos(selectedArea.endLine, insertedIndentCount);
		buffer.InsertLinesAtPos(selectedArea.startLine, selectedArea.startColumn, deletedData);
		buffer.SetSelectedArea(selectedArea.startLine, selectedArea.endLine, selectedArea.startColumn, selectedArea.endColumn);
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
		RemoveAreaAndSetCursor(buffer, selectedArea);
		buffer.StopSelecting();
		buffer.InsertCharAtPos(insertedChar, columnPos, linePos);
	}

	public override void Undo(Buffer buffer) {
		buffer.RemoveCharAtPos(columnPos, linePos);
		buffer.InsertLinesAtPos(selectedArea.startLine, selectedArea.startColumn, deletedData);
		buffer.SetSelectedArea(selectedArea.startLine, selectedArea.endLine, selectedArea.startColumn, selectedArea.endColumn);
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
		GetPastedArea(buffer);
	}

	public void GetPastedArea(Buffer buffer) {
		var pastedDataNoTab = buffer.ConvertTabsToSpace(pastedData);
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
		RemoveAreaAndSetCursor(buffer, selectedArea);
		buffer.StopSelecting();
		buffer.InsertLinesAtPos(linePos, columnPos, pastedData);
	}

	public override void Undo(Buffer buffer) {
		RemoveAreaAndSetCursor(buffer, pastedArea);
		buffer.StopSelecting();
		buffer.InsertLinesAtPos(selectedArea.startLine, selectedArea.startColumn, deletedData);
		buffer.SetSelectedArea(selectedArea.startLine, selectedArea.endLine, selectedArea.startColumn, selectedArea.endColumn);
	}
}



class ReplaceWordAction : Action {

	public int line;
	public int start;
	int length;

	List<char> oldChars;
	List<char> newChars;

	public ReplaceWordAction(int start, int length, int line, List<char> newChars, Buffer buffer) {
		this.start = start;
		this.length = length;
		this.line = line;
		this.newChars = new List<char>(newChars);
		oldChars = new();
		GetReplacedWord(buffer);
	}


	public void GetReplacedWord(Buffer buffer) {
		var oldCharsList = buffer.GetAreaData(line, line, start, start + length);
		oldChars = new List<char>(oldCharsList[0]);
	}
	public override void Redo(Buffer buffer) {
		buffer.RemoveArea(line, line, start, start + oldChars.Count);
		buffer.InsertCharsAtPos(line, start, newChars);
		buffer.line = line;
		buffer.column = start;
		buffer.ClampCursor();
	}

	public override void Undo(Buffer buffer) {
		buffer.RemoveArea(line, line, start, start + newChars.Count);
		buffer.InsertCharsAtPos(line, start, oldChars);
		buffer.line = line;
		buffer.column = start;
		buffer.ClampCursor();
	}
}


class ReplaceAllWordsAction : Action {

	List<List<Findling>> findlings;
	List<ReplaceWordAction> replaceWordsActions;
	List<char> newChars;
	public ReplaceAllWordsAction(List<List<Findling>> findlings, Buffer buffer, List<char> newChars) {
		this.findlings = new List<List<Findling>>(findlings);
		replaceWordsActions = new();
		this.newChars = new List<char>(newChars);
		GetReplacedWordActionList(buffer);
	}


	public void GetReplacedWordActionList(Buffer buffer) {
		List<Findling> flattenedFindlingList = new();
		flattenedFindlingList.AddRange(findlings.SelectMany(f => f).ToList());
		foreach (Findling findling in flattenedFindlingList) {
			replaceWordsActions.Add(new ReplaceWordAction(findling.Start, findling.Length, findling.line, newChars, buffer));
		}
	}

	public override void Redo(Buffer buffer) {
		for (int i = replaceWordsActions.Count - 1; i >= 0; i--) {
			replaceWordsActions[i].Redo(buffer);
		}
	}

	public override void Undo(Buffer buffer) {
		if (replaceWordsActions.Count == 0) {
			buffer.ClampCursor();
			return;
		}

		for (int i = 0; i < replaceWordsActions.Count; i++) {
			replaceWordsActions[i].Undo(buffer);
		}
		buffer.line = replaceWordsActions[0].line;
		buffer.column = replaceWordsActions[0].start;

		buffer.ClampCursor();
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
		GetPastedArea(buffer);
	}

	public void GetPastedArea(Buffer buffer) {
		var pastedDataNoTab = buffer.ConvertTabsToSpace(pastedData);
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
		buffer.InsertLinesAtPos(linePos, columnPos, pastedData);
	}

	public override void Undo(Buffer buffer) {
		SelectionAction.RemoveAreaAndSetCursor(buffer, pastedArea);
		buffer.StopSelecting();
	}
}


