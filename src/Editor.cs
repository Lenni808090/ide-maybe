using System.ComponentModel.DataAnnotations.Schema;

class Editor {

	Buffer buffer;
	Render render;
	StatusBar statusBar;

	Searcher searcher;
	FileExplorer fileExplorer;
	RedoUndoHandler redoUndoHandler;

	(string filePath, FileData fileData, int column, int line) prevStatusBar = default;
	public Editor() {
		buffer = new Buffer();
		searcher = new Searcher(buffer);
		render = new Render(buffer, searcher);
		fileExplorer = new FileExplorer();
		redoUndoHandler = new RedoUndoHandler(buffer);
		statusBar = new StatusBar(buffer, fileExplorer);

		Console.CancelKeyPress += async (s, e) => {
			e.Cancel = true;
			await buffer.copyLines();
		};
	}

	int prevTopLine;

	public async Task startEditor() {
		Console.Clear();
		Console.Write("\x1b[?2004h");

		buffer.lines = fileExplorer.readFile(@"C:\Users\leona\source\repos\ide-maybe\test.txt");


		render.resetView();
		render.printScreen();
		render.drawStatusBar(statusBar.UpdateStatusBar());
		startResizeWatcher();

		while (true) {
			ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

			if (keyInfo.Key == ConsoleKey.Q && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) {
				Console.Clear();
				Console.Write("\x1b[?2004l");
				Console.CursorVisible = true;
				Console.WriteLine("till next time");
				break;
			}

			if (Console.KeyAvailable) {
				List<char> bufferedChars = new List<char>();
				bufferedChars.Add(keyInfo.KeyChar);

				while (Console.KeyAvailable) {
					var nextKey = Console.ReadKey(intercept: true);
					bufferedChars.Add(nextKey.KeyChar);
				}

				if (bufferedChars.Count > 1) {
					handlePaste(bufferedChars);
					continue;
				}
				else {
					if (buffer.isSelecting) {
						redoUndoHandler.addActionToUndo(new InsertCharWhileSelecting(buffer, keyInfo.KeyChar));
					}
					else {
						redoUndoHandler.addActionToUndo(new InsertCharAction(keyInfo.KeyChar, buffer.column, buffer.line));
					}
					buffer.insertChar(keyInfo.KeyChar);
					render.resetView();
					render.printScreen();
					continue;
				}
			}

			if (keyInfo.Key == ConsoleKey.Enter) {
				if (buffer.isSelecting) {
					redoUndoHandler.addActionToUndo(new NewLineWhileSelecting(buffer));
				}
				else {
					redoUndoHandler.addActionToUndo(new NewLineAction(buffer.line, buffer.column, buffer));
				}
				buffer.newLine();
				render.resetView();
				render.printScreen();
			}
			else if (keyInfo.Key == ConsoleKey.Backspace) {
				if (buffer.column == 0 && buffer.line == 0 && !buffer.isSelecting) continue;
				if (buffer.isSelecting) {
					redoUndoHandler.addActionToUndo(new DeleteWhileSelecting(buffer));
				}
				else {
					if (buffer.column == 0) {
						redoUndoHandler.addActionToUndo(new DeleteLineAction(buffer.line, buffer));
					}
					else {
						if (buffer.isItTab()) {
							redoUndoHandler.addActionToUndo(new DeleteTabAction(buffer.column, buffer.line));
						}
						else {
							redoUndoHandler.addActionToUndo(new DeleteCharAction(buffer.lines[buffer.line][buffer.column - 1], buffer.column - 1, buffer.line));
						}
					}
				}
				bool fullRedraw = buffer.backspace();
				if (fullRedraw) {
					render.resetView();
					render.printScreen();
				}
				else {
					render.printLine(buffer.line, false);
				}
			}
			else if (keyInfo.Key == ConsoleKey.LeftArrow) {
				if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) {
					if (!buffer.isSelecting) buffer.startSelecting();
					buffer.moveLeft();
					buffer.updateSelection();
					render.printScreen();
				}
				else {
					if (buffer.isSelecting) {
						buffer.moveLeftWhileSelecting();
						buffer.stopSelecting();
						render.resetView();
						render.printScreen();
					}
					else {
						buffer.moveLeft();
					}
				}

				render.resetView();
				if (prevTopLine != render.topLine) render.printScreen();
				prevTopLine = render.topLine;
				render.setCursor(buffer.line);
			}
			else if (keyInfo.Key == ConsoleKey.RightArrow) {
				if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) {
					if (!buffer.isSelecting) buffer.startSelecting();
					buffer.moveRight();
					buffer.updateSelection();
					render.printScreen();
				}
				else {
					if (buffer.isSelecting) {
						buffer.moveRightWhileSelecting();
						buffer.stopSelecting();
						render.resetView();
						render.printScreen();
					}
					else {
						buffer.moveRight();
					}
				}

				render.resetView();
				if (prevTopLine != render.topLine) render.printScreen();
				prevTopLine = render.topLine;
				render.setCursor(buffer.line);
			}
			else if (keyInfo.Key == ConsoleKey.UpArrow) {
				if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) {
					if (!buffer.isSelecting) buffer.startSelecting();
					buffer.moveUp();
					buffer.updateSelection();
					render.printScreen();
				}
				else {
					buffer.moveUp();
					if (buffer.isSelecting) {
						buffer.stopSelecting();
						render.resetView();
						render.printScreen();
					}
				}

				render.resetView();
				if (prevTopLine != render.topLine) render.printScreen();
				prevTopLine = render.topLine;
				render.setCursor(buffer.line);
			}
			else if (keyInfo.Key == ConsoleKey.DownArrow) {
				if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) {
					if (!buffer.isSelecting) buffer.startSelecting();
					buffer.moveDown();
					buffer.updateSelection();
					render.printScreen();
				}
				else {
					buffer.moveDown();
					if (buffer.isSelecting) {
						buffer.stopSelecting();
						render.resetView();
						render.printScreen();
					}
				}

				render.resetView();
				if (prevTopLine != render.topLine) render.printScreen();
				prevTopLine = render.topLine;
				render.setCursor(buffer.line);
			}
			else if (!char.IsControl(keyInfo.KeyChar)) {
				if (buffer.pairs.ContainsKey(keyInfo.KeyChar)) {
					if (buffer.isSelecting) {
						redoUndoHandler.addActionToUndo(new InsertCharPairActionWhileSelecting(buffer, keyInfo.KeyChar));
					}
					else {
						redoUndoHandler.addActionToUndo(new InsertCharPairAction(keyInfo.KeyChar, buffer.column, buffer.line));
					}
					buffer.insertCharPair(keyInfo.KeyChar);

					render.resetView();
					render.printScreen();
				}
				else {
					if (buffer.isSelecting) {
						redoUndoHandler.addActionToUndo(new InsertCharWhileSelecting(buffer, keyInfo.KeyChar));
					}
					else {
						redoUndoHandler.addActionToUndo(new InsertCharAction(keyInfo.KeyChar, buffer.column, buffer.line));
					}
					buffer.insertChar(keyInfo.KeyChar);
					render.resetView();
					render.printScreen();
				}
			}
			else if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) {
				if (keyInfo.Key == ConsoleKey.C) {
					await buffer.copyLines();
				}
				else if (keyInfo.Key == ConsoleKey.X) {
					if (buffer.isSelecting) {
						redoUndoHandler.addActionToUndo(new DeleteWhileSelecting(buffer));
					}
					await buffer.cutLines();
					render.resetView();
					render.printScreen();
				}
				else if (keyInfo.Key == ConsoleKey.A) {
					fileExplorer.saveFile(fileExplorer.cuurentFilePath, buffer.lines);
				}
				else if (keyInfo.Key == ConsoleKey.Z) {
					redoUndoHandler.undo();
					render.resetView();
					render.printScreen();
				}
				else if (keyInfo.Key == ConsoleKey.Y) {
					redoUndoHandler.redo();
					render.resetView();
					render.printScreen();
				}

				else if (keyInfo.Key == ConsoleKey.F) {
					searcher.setSearch("hallo".ToList());
					render.resetView();
					render.printScreen();
				}
			}
			else if (keyInfo.Key == ConsoleKey.Tab) {
				redoUndoHandler.addActionToUndo(new InsertTabAction(buffer.column, buffer.line));
				buffer.insertTab(4);
				render.printLine(buffer.line, false);
			}

			var updatedStatusBar = statusBar.UpdateStatusBar();

			if (prevStatusBar == default) {
				render.drawStatusBar(updatedStatusBar);
				prevStatusBar = updatedStatusBar;
			}
			else if (prevStatusBar != updatedStatusBar) {
				render.drawStatusBar(updatedStatusBar);
				prevStatusBar = updatedStatusBar;
			}

		}
	}

	private void handlePaste(List<char> chars) {
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
			redoUndoHandler.addActionToUndo(new PasteDataWhileSelecting(pastedData, buffer));
		}
		else {
			redoUndoHandler.addActionToUndo(new PasteDataAction(pastedData, buffer.column, buffer.line, buffer));
		}
		buffer.pasteData(pastedData);
		render.resetView();
		render.printScreen();
	}

	private void startResizeWatcher() {
		Task.Run(async () => {
			int prevHeight = Console.WindowHeight;
			int prevWidth = Console.WindowWidth;

			while (true) {
				if (Console.WindowHeight != prevHeight || Console.WindowWidth != prevWidth) {
					Console.Clear();
					render.resetView();
					render.printScreen();
					render.drawStatusBar(statusBar.UpdateStatusBar());
					prevHeight = Console.WindowHeight;
					prevWidth = Console.WindowWidth;

				}

				await Task.Delay(50);
			}
		});
	}
}
