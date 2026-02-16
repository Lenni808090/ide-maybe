abstract class Action {

	public abstract void Undo(Buffer buffer);

	public abstract void Redo(Buffer buffer);
}

class InsertCharAction : Action {
	char insertedChar;
	int pos;

	public InsertCharAction(char insertedChar) {
		this.insertedChar = insertedChar;
	}
	public override void Redo(Buffer buffer) {
		buffer.insertCharAtPos(insertedChar, pos);
	}

	public override void Undo(Buffer buffer) {
	}
}
s
