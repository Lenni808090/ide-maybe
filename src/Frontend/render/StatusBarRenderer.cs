class StatusBarSegment {
	public string Text { get; }
	public ConsoleColor BackgroundColor { get; }
	public ConsoleColor ForegroundColor { get; }

	public StatusBarSegment(
		string text,
		ConsoleColor backgroundColor,
		ConsoleColor foregroundColor = ConsoleColor.White
	) {
		Text = text;
		BackgroundColor = backgroundColor;
		ForegroundColor = foregroundColor;
	}
}

class StatusBarRenderer {
	private static string Fit(string text, int maxLength) {
		if (maxLength <= 0) return "";
		if (text.Length <= maxLength) return text;
		return text.Substring(0, maxLength);
	}

	public void Draw(
		int statusBarLine,
		int width,
		ConsoleColor baseBackgroundColor,
		List<StatusBarSegment> leftSegments,
		StatusBarSegment rightSegment
	) {
		if (width <= 0) return;

		string rightText = Fit(rightSegment.Text, width);
		int rightStart = Math.Max(0, width - rightText.Length);

		Console.SetCursorPosition(0, statusBarLine);
		Console.BackgroundColor = baseBackgroundColor;
		Console.ForegroundColor = ConsoleColor.White;
		Console.Write("\x1b[K");

		int currentLeftPos = 0;
		int maxLeftLength = rightStart;

		foreach (StatusBarSegment segment in leftSegments) {
			int available = maxLeftLength - currentLeftPos;
			if (available <= 0) {
				break;
			}

			string segmentText = Fit(segment.Text, available);
			if (string.IsNullOrEmpty(segmentText)) {
				continue;
			}

			Console.SetCursorPosition(currentLeftPos, statusBarLine);
			Console.BackgroundColor = segment.BackgroundColor;
			Console.ForegroundColor = segment.ForegroundColor;
			Console.Write(segmentText);
			currentLeftPos += segmentText.Length;
		}

		if (!string.IsNullOrEmpty(rightText)) {
			Console.SetCursorPosition(rightStart, statusBarLine);
			Console.BackgroundColor = rightSegment.BackgroundColor;
			Console.ForegroundColor = rightSegment.ForegroundColor;
			Console.Write(rightText);
		}

		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.White;
	}
}

