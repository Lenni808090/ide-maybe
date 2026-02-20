using System.ComponentModel.DataAnnotations.Schema;

class EditorState : State {

	Buffer buffer;
	Render render;
	StatusBar statusBar;
	Replacer replacer;
	Searcher searcher;
	RedoUndoHandler redoUndoHandler;

	StateManager stateManager;
	SearchInputMode searchInputMode = SearchInputMode.Search;
	bool currInSearchMode = false;
	bool warned;
	List<char> typedSearchedChar;
	List<char> typedReplaceChar;

	public EditorState(StateManager stateManager, Buffer buffer) {
		this.stateManager = stateManager;
		this.buffer = buffer;
		searcher = new Searcher(buffer);
		redoUndoHandler = new RedoUndoHandler(buffer);
		replacer = new Replacer(buffer, searcher);
		statusBar = new StatusBar(buffer, stateManager.GetCurrentFilePath, redoUndoHandler.IsitDirty, searcher, replacer);

		typedSearchedChar = new();
		typedReplaceChar = new();
		render = new Render(buffer, searcher, statusBar);
	}

	int prevTopLine;


	override public async Task handleInput(ConsoleKeyInfo keyInfo) {
		bool isCtrlO = keyInfo.Key == ConsoleKey.O && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control);
		if (warned && !isCtrlO) {
			warned = false;
			statusBar.ClearWarning();
		}

		if (keyInfo.Key == ConsoleKey.Q && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) {
			stateManager.SwitchState(ProgrammState.CLosed);
			return;
		}



		// early returen if searchin
		if (currInSearchMode) {
			HandleSearchModeInput(keyInfo);
			render.DrawStatusBar(searchInputMode);
			return;
		}

		if (Console.KeyAvailable) {
			List<char> bufferedChars = new List<char>();
			bufferedChars.Add(keyInfo.KeyChar);

			while (Console.KeyAvailable) {
				var nextKey = Console.ReadKey(intercept: true);
				bufferedChars.Add(nextKey.KeyChar);
			}

			if (bufferedChars.Count > 1) {
				HandlePaste(bufferedChars);
				render.DrawStatusBar(searchInputMode);
				return;
			}
			else {
				if (buffer.isSelecting) {
					redoUndoHandler.AddActionToUndo(new InsertCharWhileSelecting(buffer, keyInfo.KeyChar));
				}
				else {
					redoUndoHandler.AddActionToUndo(new InsertCharAction(keyInfo.KeyChar, buffer.column, buffer.line));
				}
				bool hadSelection = buffer.isSelecting;
				buffer.InsertChar(keyInfo.KeyChar);
				if (hadSelection) {
					render.ResetView();
					render.PrintScreen();
				}
				else {
					render.PrintLine(buffer.line, false);
				}
				render.DrawStatusBar(searchInputMode);
				return;
			}
		}

		if (keyInfo.Key == ConsoleKey.Enter) {
			if (buffer.isSelecting) {
				redoUndoHandler.AddActionToUndo(new NewLineWhileSelecting(buffer));
			}
			else {
				redoUndoHandler.AddActionToUndo(new NewLineAction(buffer.line, buffer.column, buffer));
			}
			buffer.NewLine();
			render.ResetView();
			render.PrintSection(Math.Max(render.topLine, buffer.line - 1));
		}
		else if (keyInfo.Key == ConsoleKey.Backspace) {
			if (buffer.column == 0 && buffer.line == 0 && !buffer.isSelecting) return;
			if (buffer.isSelecting) {
				redoUndoHandler.AddActionToUndo(new DeleteWhileSelecting(buffer));
			}
			else {
				if (buffer.column == 0) {
					redoUndoHandler.AddActionToUndo(new DeleteLineAction(buffer.line, buffer));
				}
				else {
					if (buffer.IsItTab()) {
						redoUndoHandler.AddActionToUndo(new DeleteTabAction(buffer.column, buffer.line));
					}
					else {
						redoUndoHandler.AddActionToUndo(new DeleteCharAction(buffer.lines[buffer.line][buffer.column - 1], buffer.column - 1, buffer.line));
					}
				}
			}
			bool fullRedraw = buffer.Backspace();
			if (fullRedraw) {
				render.ResetView();
				render.PrintScreen();
			}
			else {
				render.PrintLine(buffer.line, false);
			}
		}
		else if (keyInfo.Key == ConsoleKey.LeftArrow) {
			if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) {
				if (!buffer.isSelecting) buffer.StartSelecting();
				buffer.MoveLeft();
				buffer.UpdateSelection();
				render.PrintScreen();
			}
			else {
				if (buffer.isSelecting) {
					buffer.MoveLeftWhileSelecting();
					buffer.StopSelecting();
					render.ResetView();
					render.PrintScreen();
				}
				else {
					buffer.MoveLeft();
				}
			}

			render.ResetView();
			if (prevTopLine != render.topLine) render.PrintScreen();
			prevTopLine = render.topLine;
			render.SetCursor(buffer.line);
		}
		else if (keyInfo.Key == ConsoleKey.RightArrow) {
			if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) {
				if (!buffer.isSelecting) buffer.StartSelecting();
				buffer.MoveRight();
				buffer.UpdateSelection();
				render.PrintScreen();
			}
			else {
				if (buffer.isSelecting) {
					buffer.MoveRightWhileSelecting();
					buffer.StopSelecting();
					render.ResetView();
					render.PrintScreen();
				}
				else {
					buffer.MoveRight();
				}
			}

			render.ResetView();
			if (prevTopLine != render.topLine) render.PrintScreen();
			prevTopLine = render.topLine;
			render.SetCursor(buffer.line);
		}
		else if (keyInfo.Key == ConsoleKey.UpArrow) {
			if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) {
				if (!buffer.isSelecting) buffer.StartSelecting();
				buffer.MoveUp();
				buffer.UpdateSelection();
				render.PrintScreen();
			}
			else {
				buffer.MoveUp();
				if (buffer.isSelecting) {
					buffer.StopSelecting();
					render.ResetView();
					render.PrintScreen();
				}
			}

			render.ResetView();
			if (prevTopLine != render.topLine) render.PrintScreen();
			prevTopLine = render.topLine;
			render.SetCursor(buffer.line);
		}
		else if (keyInfo.Key == ConsoleKey.DownArrow) {
			if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) {
				if (!buffer.isSelecting) buffer.StartSelecting();
				buffer.MoveDown();
				buffer.UpdateSelection();
				render.PrintScreen();
			}
			else {
				buffer.MoveDown();
				if (buffer.isSelecting) {
					buffer.StopSelecting();
					render.ResetView();
					render.PrintScreen();
				}
			}

			render.ResetView();
			if (prevTopLine != render.topLine) render.PrintScreen();
			prevTopLine = render.topLine;
			render.SetCursor(buffer.line);
		}
		else if (!char.IsControl(keyInfo.KeyChar)) {
			if (buffer.pairs.ContainsKey(keyInfo.KeyChar)) {
				if (buffer.isSelecting) {
					redoUndoHandler.AddActionToUndo(new InsertCharPairActionWhileSelecting(buffer, keyInfo.KeyChar));
				}
				else {
					redoUndoHandler.AddActionToUndo(new InsertCharPairAction(keyInfo.KeyChar, buffer.column, buffer.line));
				}
				bool hadSelection = buffer.isSelecting;
				buffer.InsertCharPair(keyInfo.KeyChar);

				if (hadSelection) {
					render.ResetView();
					render.PrintScreen();
				}
				else {
					render.PrintLine(buffer.line, false);
				}
			}
			else {
				if (buffer.isSelecting) {
					redoUndoHandler.AddActionToUndo(new InsertCharWhileSelecting(buffer, keyInfo.KeyChar));
				}
				else {
					redoUndoHandler.AddActionToUndo(new InsertCharAction(keyInfo.KeyChar, buffer.column, buffer.line));
				}
				bool hadSelection = buffer.isSelecting;
				buffer.InsertChar(keyInfo.KeyChar);
				if (hadSelection) {
					render.ResetView();
					render.PrintScreen();
				}
				else {
					render.PrintLine(buffer.line, false);
				}
			}
		}
		else if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) {
			if (keyInfo.Key == ConsoleKey.C) {
				await buffer.CopyLines();
			}
			else if (keyInfo.Key == ConsoleKey.X) {
				if (buffer.isSelecting) {
					redoUndoHandler.AddActionToUndo(new DeleteWhileSelecting(buffer));
				}
				await buffer.CutLines();
				render.ResetView();
				render.PrintScreen();
			}
			else if (keyInfo.Key == ConsoleKey.A) {
				stateManager.SaveCurrentFile(buffer.lines);
				redoUndoHandler.MarkSaved();
				warned = false;
				statusBar.ClearWarning();
			}
			else if (keyInfo.Key == ConsoleKey.Z) {
				redoUndoHandler.Undo();
				render.ResetView();
				render.PrintScreen();
			}
			else if (keyInfo.Key == ConsoleKey.Y) {
				redoUndoHandler.Redo();
				render.ResetView();
				render.PrintScreen();
			}
			else if (keyInfo.Key == ConsoleKey.F) {
				statusBar.statusBarMode = StatusBarMode.Search;
				currInSearchMode = true;
				searchInputMode = SearchInputMode.Search;
				replacer.ClearReplace();
				typedSearchedChar.Clear();
				typedReplaceChar.Clear();
				searcher.ClearSearch();
				render.ResetView();
				render.PrintScreen();
			}
			else if (keyInfo.Key == ConsoleKey.H) {
				statusBar.statusBarMode = StatusBarMode.Search;
				currInSearchMode = true;
				searchInputMode = SearchInputMode.Search;
				replacer.ClearReplace();
				replacer.isReplacing = true;
				typedSearchedChar.Clear();
				typedReplaceChar.Clear();
				searcher.ClearSearch();
				render.ResetView();
				render.PrintScreen();
			}
			else if (keyInfo.Key == ConsoleKey.O) {
				if (redoUndoHandler.IsitDirty()) {
					if (warned) {
						warned = false;
						statusBar.ClearWarning();
						stateManager.SwitchState(ProgrammState.FileExplorer);
					}
					else {
						statusBar.SetWarning("Changes not Saved. Save before switching file.  [CTRL + A] for Save [CTRL + O] to Lose");
						warned = true;
					}
				}
				else {
					warned = false;
					statusBar.ClearWarning();
					stateManager.SwitchState(ProgrammState.FileExplorer);
				}
			}
		}
		else if (keyInfo.Key == ConsoleKey.Tab) {
			redoUndoHandler.AddActionToUndo(new InsertTabAction(buffer.column, buffer.line));
			buffer.InsertTab(4);
			render.PrintLine(buffer.line, false);
		}

		render.DrawStatusBar(searchInputMode);

	}

	private void HandleSearchModeInput(ConsoleKeyInfo keyInfo) {
		if (keyInfo.Key == ConsoleKey.Escape) {
			currInSearchMode = false;
			searchInputMode = SearchInputMode.Search;
			replacer.ClearReplace();
			typedSearchedChar.Clear();
			typedReplaceChar.Clear();
			searcher.ClearSearch();
			statusBar.statusBarMode = StatusBarMode.Normal;
			render.ResetView();
			render.PrintScreen();
			return;
		}
		else if (keyInfo.Key == ConsoleKey.Backspace) {
			if (searchInputMode == SearchInputMode.Search) {
				if (typedSearchedChar.Count > 0) {
					typedSearchedChar.RemoveAt(typedSearchedChar.Count - 1);
				}

				if (typedSearchedChar.Count == 0) {
					searcher.ClearSearch();
				}
				else {
					searcher.SetSearch(typedSearchedChar);
				}
			}
			else if (searchInputMode == SearchInputMode.Replace) {
				if (typedReplaceChar.Count > 0) {
					typedReplaceChar.RemoveAt(typedReplaceChar.Count - 1);
				}
				replacer.SetCharsUsedToReplace(typedReplaceChar);
			}

			render.ResetView();
			render.PrintScreen();
			return;
		}
		else if (!char.IsControl(keyInfo.KeyChar)) {
			if (searchInputMode == SearchInputMode.Search) {
				if (typedSearchedChar.Count > 25) return;
				typedSearchedChar.Add(keyInfo.KeyChar);
				searcher.SetSearch(typedSearchedChar);
			}
			else if (searchInputMode == SearchInputMode.Replace) {
				if (typedReplaceChar.Count > 25) return;
				typedReplaceChar.Add(keyInfo.KeyChar);
				replacer.SetCharsUsedToReplace(typedReplaceChar);
			}
			render.ResetView();
			render.PrintScreen();
		}
		else if (keyInfo.Key == ConsoleKey.Tab) {
			if (replacer.isReplacing) {
				searchInputMode = searchInputMode == SearchInputMode.Search ? SearchInputMode.Replace : SearchInputMode.Search;
			}
		}
		else if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) {
			if (keyInfo.Key == ConsoleKey.H) {
				if (replacer.isReplacing) {
					searchInputMode = searchInputMode == SearchInputMode.Search ? SearchInputMode.Replace : SearchInputMode.Search;
				}
				else {
					replacer.ClearReplace();
					replacer.isReplacing = true;
					searchInputMode = SearchInputMode.Replace;
				}
			}
		}
		else if (keyInfo.Key == ConsoleKey.Enter) {
			if (searchInputMode == SearchInputMode.Search) {
				if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) {
					searcher.MoveToPrevFind();
				}
				else {
					searcher.MoveToNextFind();
				}
			}
			else if (searchInputMode == SearchInputMode.Replace) {
				if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) {
					if (searcher.totalFinds == 0) return;
					redoUndoHandler.AddActionToUndo(new ReplaceAllWordsAction(searcher.findlings, buffer, replacer.charsUsedToReplace));
					replacer.ReplaceAllFindlings();
				}
				else {
					var findlingData = searcher.GetCurrentFindlingData();
					if (findlingData == null) return;
					redoUndoHandler.AddActionToUndo(new ReplaceWordAction(findlingData.Value.start, findlingData.Value.length, findlingData.Value.line, replacer.charsUsedToReplace, buffer));
					replacer.ReplaceCurrentFindling();
				}
			}

			render.ResetView();
			render.PrintScreen();

		}
	}
	public void MarkSaved() => redoUndoHandler.MarkSaved();
	public void OnFileOpen() {
		redoUndoHandler.ResetRedoHandler();
	}
	private void HandlePaste(List<char> chars) {
		List<List<char>> pastedData = new List<List<char>>();
		pastedData.Add(new List<char>());
		int pastedDataLine = 0;

		for (int i = 0; i < chars.Count; i++) {
			char c = chars[i];

			if (c == '\r') {
				if (i + 1 < chars.Count && chars[i + 1] == '\n') continue;
				pastedData.Add(new List<char>());
				pastedDataLine++;
			}
			else if (c == '\n') {
				pastedData.Add(new List<char>());
				pastedDataLine++;
			}
			else {
				pastedData[pastedDataLine].Add(c);
			}
		}

		if (pastedData.Count > 0 && pastedData[pastedData.Count - 1].Count == 0) {
			pastedData.RemoveAt(pastedData.Count - 1);
		}
		if (buffer.isSelecting) {
			redoUndoHandler.AddActionToUndo(new PasteDataWhileSelecting(pastedData, buffer));
		}
		else {
			redoUndoHandler.AddActionToUndo(new PasteDataAction(pastedData, buffer.column, buffer.line, buffer));
		}
		buffer.PasteData(pastedData);
		render.ResetView();
		render.PrintScreen();
	}

	public override void Render() {
		Console.CursorVisible = true;
		render.ResetView();
		render.PrintScreen();
		statusBar.UpdateStatusBar();
		render.DrawStatusBar(searchInputMode);
	}
}

