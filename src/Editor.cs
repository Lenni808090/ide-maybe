class Editor {

	Buffer buffer;
	Render render;

	FileExplorer fileExplorer;

	public Editor() {
		buffer = new Buffer();
		render = new Render(buffer);
		fileExplorer = new FileExplorer();
	}
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
				buffer.moveLeft();
				render.setCursor(buffer.line);
			}
			else if (keyInfo.Key == ConsoleKey.RightArrow) {
				buffer.moveRight();
				render.setCursor(buffer.line);
			}
			else if (keyInfo.Key == ConsoleKey.UpArrow) {
				buffer.moveUp();
				render.setCursor(buffer.line);
			}
			else if (keyInfo.Key == ConsoleKey.DownArrow) {
				buffer.moveDown();
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
