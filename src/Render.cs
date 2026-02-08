using System.Diagnostics;

class Render {
	List<int> screenCharCount;
	Buffer buffer;

	public Render(Buffer buffer) {
		this.buffer = buffer;
		screenCharCount = new List<int>();
	}

	public void RedrawLine() {
		List<char> lineToRedraw = buffer.lines[buffer.line];
		int start = 0;

		Console.SetCursorPosition(start, buffer.line);

		for (int i = start; i < lineToRedraw.Count; i++) {
			Console.Write(lineToRedraw[i]);
		}

		int lineCount = lineToRedraw.Count;

		int screenCount = 0;
		if (screenCharCount.Count > buffer.line) {
			screenCount = screenCharCount[buffer.line];
		}

		int remainingChars = (screenCount > lineCount) ? (screenCount - lineCount) : 0;

		for (int i = 0; i < remainingChars; i++) {
			Console.Write(" ");
		}

		if (screenCharCount.Count <= buffer.line) {
			screenCharCount.Add(lineCount);
		} else {
			screenCharCount[buffer.line] = lineCount;
		}

		Console.SetCursorPosition(buffer.coloumn, buffer.line);
	}

	public void RedrawSection() {
		for (int i = Math.Max(buffer.line - 1, 0); i < buffer.lines.Count; i++) {
			RedrawLine();
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
