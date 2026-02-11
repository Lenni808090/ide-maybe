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

			} else if (keyInfo.Key == ConsoleKey.Backspace) {

			} else if (keyInfo.Key == ConsoleKey.LeftArrow) {
			} else if (keyInfo.Key == ConsoleKey.RightArrow) {
			} else if (keyInfo.Key == ConsoleKey.UpArrow) {
			} else if (keyInfo.Key == ConsoleKey.DownArrow) {
			} else if (!char.IsControl(keyInfo.KeyChar)) {
			} else if (keyInfo.Key == ConsoleKey.Tab) {
			}
		}
	}
}
