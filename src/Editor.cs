class Editor {

	Buffer buffer;
	Render render;


	public Editor() {
		buffer = new Buffer();
		render = new Render(buffer);
	}
	public void startEditor() {
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
				render.RedrawSection();
			} else if (keyInfo.Key == ConsoleKey.Backspace) {
				bool fullRedraw = buffer.backspace();
				if (fullRedraw) {
					render.RedrawSection();
				} else {
					render.RedrawLine();
				}
			} else if (keyInfo.Key == ConsoleKey.LeftArrow) {
				buffer.moveLeft();
				render.setCursor();
			} else if (keyInfo.Key == ConsoleKey.RightArrow) {
				buffer.moveRight();
				render.setCursor();
			} else if (keyInfo.Key == ConsoleKey.UpArrow) {
				buffer.moveUp();
				render.setCursor();
			} else if (keyInfo.Key == ConsoleKey.DownArrow) {
				buffer.moveDown();
				render.setCursor();
			} else if (!char.IsControl(keyInfo.KeyChar)) {
				buffer.insertChar(keyInfo.KeyChar);
				render.RedrawLine();
			}
		}
	}
}
