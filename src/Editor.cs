using System.ComponentModel.DataAnnotations.Schema;

class Editor {

	Buffer buffer;
	Render render;
	StatusBar statusBar;
	Replacer replacer;
	Searcher searcher;
	FileManager fileManager;
	FileExplorer fileExplorer;
	FileExplorerRenderer fileExplorerRenderer;
	RedoUndoHandler redoUndoHandler;

	SearchInputMode searchInputMode = SearchInputMode.Search;
	bool currInSearchMode = false;
	bool resizeWatcherStarted = false;
	List<char> typedSearchedChar;
	List<char> typedReplaceChar;

	public Editor() {
		buffer = new Buffer();
		searcher = new Searcher(buffer);
		fileExplorer = new FileExplorer();
		fileExplorerRenderer = new FileExplorerRenderer(fileExplorer);
		fileManager = new FileManager();
		redoUndoHandler = new RedoUndoHandler(buffer);
		replacer = new Replacer(buffer, searcher);
		statusBar = new StatusBar(buffer, fileManager, searcher, replacer);

		typedSearchedChar = new();
		typedReplaceChar = new();
		render = new Render(buffer, searcher, statusBar);

		Console.CancelKeyPress += async (s, e) => {
			e.Cancel = true;
			await buffer.CopyLines();
		};
	}

	int prevTopLine;


	public async Task<bool> StartFileExploring() {
		Console.Clear();
		Console.CursorVisible = false;
		fileExplorerRenderer.ResetDirectoryView();
		fileExplorerRenderer.RenderDirectroys();


		while (true) {
			ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

			if (keyInfo.Key == ConsoleKey.Q && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) {
				return true;
			}
			else if (keyInfo.Key == ConsoleKey.DownArrow) {
				fileExplorer.MoveToNextEntry();
				fileExplorerRenderer.ResetDirectoryView();
				fileExplorerRenderer.RenderDirectroys();
			}
			else if (keyInfo.Key == ConsoleKey.UpArrow) {
				fileExplorer.MoveToPrevEntry();
				fileExplorerRenderer.ResetDirectoryView();
				fileExplorerRenderer.RenderDirectroys();
			}
			else if (keyInfo.Key == ConsoleKey.Escape) {
				return false;
			}

		}
	}


	public async Task StartEditor() {
		Console.Clear();
		Console.Write("\x1b[?2004h");

		buffer.lines = fileManager.ReadFile(@"C:\Users\leona\source\repos\ide-maybe\test.txt");


		render.ResetView();
		render.PrintScreen();
		render.DrawStatusBar(searchInputMode);

		if (!resizeWatcherStarted) {
			resizeWatcherStarted = true;
			StartResizeWatcher();
		}

		while (true) {
			ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);


			if (keyInfo.Key == ConsoleKey.Q && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) {
				Console.Clear();
				Console.Write("\x1b[?2004l");
				Console.CursorVisible = true;
				Console.WriteLine("till next time");
				break;
			}


			// early returen if searchin
			if (currInSearchMode) {
				HandleSearchModeInput(keyInfo);
				render.DrawStatusBar(searchInputMode);
				continue;
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
					continue;
				}
				else {
					if (buffer.isSelecting) {
						redoUndoHandler.AddActionToUndo(new InsertCharWhileSelecting(buffer, keyInfo.KeyChar));
					}
					else {
						redoUndoHandler.AddActionToUndo(new InsertCharAction(keyInfo.KeyChar, buffer.column, buffer.line));
					}
					buffer.InsertChar(keyInfo.KeyChar);
					render.ResetView();
					render.PrintScreen();
					continue;
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
				render.PrintScreen();
			}
			else if (keyInfo.Key == ConsoleKey.Backspace) {
				if (buffer.column == 0 && buffer.line == 0 && !buffer.isSelecting) continue;
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
					buffer.InsertCharPair(keyInfo.KeyChar);

					render.ResetView();
					render.PrintScreen();
				}
				else {
					if (buffer.isSelecting) {
						redoUndoHandler.AddActionToUndo(new InsertCharWhileSelecting(buffer, keyInfo.KeyChar));
					}
					else {
						redoUndoHandler.AddActionToUndo(new InsertCharAction(keyInfo.KeyChar, buffer.column, buffer.line));
					}
					buffer.InsertChar(keyInfo.KeyChar);
					render.ResetView();
					render.PrintScreen();
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
					fileManager.SaveFile(fileManager.currentFilePath, buffer.lines);
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
					var shouldQuit = await StartFileExploring();
					if (shouldQuit) {
						Console.Clear();
						Console.Write("\x1b[?2004l");
						Console.CursorVisible = true;
						Console.WriteLine("till next time");
						break;
					}
					render.ResetView();
					render.PrintScreen();
				}
			}
			else if (keyInfo.Key == ConsoleKey.Tab) {
				redoUndoHandler.AddActionToUndo(new InsertTabAction(buffer.column, buffer.line));
				buffer.InsertTab(4);
				render.PrintLine(buffer.line, false);
			}

			render.DrawStatusBar(searchInputMode);
		}
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

	private void StartResizeWatcher() {
		Task.Run(async () => {
			int prevHeight = Console.WindowHeight;
			int prevWidth = Console.WindowWidth;
			while (true) {
				if (Console.WindowHeight != prevHeight || Console.WindowWidth != prevWidth) {
					Console.Clear();
					render.ResetView();
					render.PrintScreen();
					render.DrawStatusBar(searchInputMode);
					prevHeight = Console.WindowHeight;
					prevWidth = Console.WindowWidth;

				}

				await Task.Delay(50);
			}
		});
	}
}

