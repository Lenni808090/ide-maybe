class RedoUndoHandler {
	public List<Action> redoStack;
	public List<Action> undoStack;

	Buffer buffer;
	public RedoUndoHandler(Buffer buffer) {
		redoStack = new List<Action>();
		undoStack = new List<Action>();
		this.buffer = buffer;
	}

	public void AddActionToUndo(Action action) {
		undoStack.Add(action);
		redoStack.Clear();
	}

	public void Redo() {
		if (redoStack.Count == 0) return;

		Action actionToRedo = redoStack[redoStack.Count - 1];
		buffer.StopSelecting();
		actionToRedo.Redo(buffer);
		redoStack.RemoveAt(redoStack.Count - 1);
		undoStack.Add(actionToRedo);
	}

	public void Undo() {
		if (undoStack.Count == 0) return;

		Action actionToUndo = undoStack[undoStack.Count - 1];
		buffer.StopSelecting();
		actionToUndo.Undo(buffer);
		undoStack.RemoveAt(undoStack.Count - 1);
		redoStack.Add(actionToUndo);
	}
}

