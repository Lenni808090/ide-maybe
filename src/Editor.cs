class Editor {

	Buffer buffer;
	Render render;

	FileExplorer fileExplorer;

	public Editor() {
		buffer = new Buffer();
		render = new Render(buffer);
		fileExplorer = new FileExplorer();
	}

	int prevTopLine;
	public void startEditor() {
		Console.Clear();

		buffer.lines = fileExplorer.readFile(@"C:\Users\leona\source\repos\ide-maybe\test.txt");

		render.resetView();

		while (true) {
			ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);


			if (keyInfo.Key == ConsoleKey.C && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) {
				Console.Clear();
				Console.CursorVisible = true;
				Console.WriteLine("till next time");
				break;
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
					if (!buffer.isSelecting) {
						buffer.startSelecting();
						buffer.moveLeft();
						buffer.updateSelection();
					}
					else {
						buffer.moveLeft();
						buffer.updateSelection();
					}
				}
				else {
					buffer.moveLeft();
					buffer.stopSelecting();
				}
				render.resetView();
				if (prevTopLine != render.topLine) {
					render.printScreen();
				}
				prevTopLine = render.topLine;
				render.setCursor(buffer.line);
			}
			else if (keyInfo.Key == ConsoleKey.RightArrow) {
				if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) {
					if (!buffer.isSelecting) {
						buffer.startSelecting();
						buffer.moveRight();
						buffer.updateSelection();
					}
					else {
						buffer.moveRight();
						buffer.updateSelection();
					}
				}
				else {
					buffer.moveRight();
					buffer.stopSelecting();
				}
				render.resetView();
				if (prevTopLine != render.topLine) {
					render.printScreen();
				}
				prevTopLine = render.topLine;
				render.setCursor(buffer.line);
			}
			else if (keyInfo.Key == ConsoleKey.UpArrow) {
				if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) {
					if (!buffer.isSelecting) {
						buffer.startSelecting();
						buffer.moveUp();
						buffer.updateSelection();
					}
					else {
						buffer.moveUp();
						buffer.updateSelection();
					}
				}
				else {
					buffer.moveUp();
					buffer.stopSelecting();
				}
				render.resetView();
				if (prevTopLine != render.topLine) {
					render.printScreen();
				}
				prevTopLine = render.topLine;
				render.setCursor(buffer.line);
			}
			else if (keyInfo.Key == ConsoleKey.DownArrow) {
				if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) {
					if (!buffer.isSelecting) {
						buffer.startSelecting();
						buffer.moveDown();
						buffer.updateSelection();
					}
					else {
						buffer.moveDown();
						buffer.updateSelection();
					}
				}
				else {
					buffer.moveDown();
					buffer.stopSelecting();
				}
				render.resetView();
				if (prevTopLine != render.topLine) {
					render.printScreen();
				}
				prevTopLine = render.topLine;
				render.setCursor(buffer.line);
			}
			else if (!char.IsControl(keyInfo.KeyChar)) {
				buffer.insertChar(keyInfo.KeyChar);
				render.printLine(buffer.line);
			}
			else if (keyInfo.Key == ConsoleKey.Tab) {
				buffer.insertTab(4);
				render.printLine(buffer.line);
			}
		}
	}
}
