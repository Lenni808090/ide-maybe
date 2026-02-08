using System.Diagnostics;

class Render {
	List<int> screenCharCount;
	Buffer buffer;

	public Render(Buffer buffer) {
		this.buffer = buffer;
		screenCharCount = new List<int>();
	}

	public void RedrawLine(int indLineToRedraw) {
		Console.CursorVisible = false;
		List<char> lineToRedraw = buffer.lines[indLineToRedraw];
		int start = Math.Max(buffer.coloumn - 1, 0);

		Console.SetCursorPosition(start, indLineToRedraw);

		for (int i = start; i < lineToRedraw.Count; i++) {
			Console.Write(lineToRedraw[i]);
		}

		int lineCount = lineToRedraw.Count;

		int screenCount = 0;
		if (screenCharCount.Count > indLineToRedraw) {
			screenCount = screenCharCount[indLineToRedraw];
		}

		int remainingChars = (screenCount > lineCount) ? (screenCount - lineCount) : 0;

		Console.Write(new string(' ', remainingChars));

		if (screenCharCount.Count <= indLineToRedraw) {
			screenCharCount.Add(lineCount);
		} else {
			screenCharCount[indLineToRedraw] = lineCount;
		}
		Console.SetCursorPosition(buffer.coloumn, indLineToRedraw);

		Console.CursorVisible = true;
	}

	public void RedrawSection(int startLine) {
		for (int i = startLine; i < buffer.lines.Count; i++) {
			RedrawLine(i);
		}

		if (screenCharCount.Count > buffer.lines.Count) {
			for (int i = buffer.lines.Count; i < screenCharCount.Count; i++) {
				Console.SetCursorPosition(0, i);
				clearLine();
			}

			screenCharCount.RemoveRange(buffer.lines.Count, screenCharCount.Count - buffer.lines.Count);
		}
	}

	public void setCursor() {
		Console.SetCursorPosition(buffer.coloumn, buffer.line);
	}

	public void clearLine() {
		Console.Write(new string(' ', Console.WindowWidth));
	}
}
