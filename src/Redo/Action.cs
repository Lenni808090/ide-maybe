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
