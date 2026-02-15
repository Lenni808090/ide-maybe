class Editor {

	Buffer buffer;
	Render render;
	FileExplorer fileExplorer;

	List<List<char>> pastedData;
	int pastedDataLine = 0;

	public Editor() {
		buffer = new Buffer();
		render = new Render(buffer);
		fileExplorer = new FileExplorer();
		pastedData = new List<List<char>>();

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

		while (true) {
			ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

			if (keyInfo.Key == ConsoleKey.Q && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) {
				Console.Clear();
				Console.Write("\x1b[?2004l");
				Console.CursorVisible = true;
				Console.WriteLine("till next time");
				break;
			}

			if (Console.KeyAvailable && !char.IsControl(keyInfo.KeyChar)) {
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
					buffer.insertChar(keyInfo.KeyChar);
					render.printLine(buffer.line);
					continue;
				}
			}

			if (keyInfo.Key == ConsoleKey.Enter) {
				buffer.newLine();
				render.resetView();
				render.printScreen();
			}
			else if (keyInfo.Key == ConsoleKey.Backspace) {
				bool fullRedraw = buffer.backspace();
				if (fullRedraw) {
					render.resetView();
					render.printScreen();
				}
				else {
					render.printLine(buffer.line);
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
					buffer.moveLeft();
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
			else if (keyInfo.Key == ConsoleKey.RightArrow) {
				if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) {
					if (!buffer.isSelecting) buffer.startSelecting();
					buffer.moveRight();
					buffer.updateSelection();
					render.printScreen();
				}
				else {
					buffer.moveRight();
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
				buffer.insertChar(keyInfo.KeyChar);
				render.printLine(buffer.line);
			}
			else if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) {
				if (keyInfo.Key == ConsoleKey.C) {
					await buffer.copyLines();
				}
			}
			else if (keyInfo.Key == ConsoleKey.Tab) {
				buffer.insertTab(4);
				render.printLine(buffer.line);
			}
		}
	}

	private void handlePaste(List<char> chars) {
		pastedData.Clear();
		pastedData.Add(new List<char>());
		pastedDataLine = 0;

		foreach (char c in chars) {
			if (c == '\n' || c == '\r') {
				if (!(c == '\r' && chars.IndexOf(c) + 1 < chars.Count && chars[chars.IndexOf(c) + 1] == '\n')) {
					pastedData.Add(new List<char>());
					pastedDataLine++;
				}
			}
			else if (!char.IsControl(c)) {
				pastedData[pastedDataLine].Add(c);
			}
		}

		if (pastedData[pastedData.Count - 1].Count == 0) {
			pastedData.RemoveAt(pastedData.Count - 1);
		}

		buffer.pasteData(pastedData);
		render.resetView();
		render.printScreen();
	}
}
