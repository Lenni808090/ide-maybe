struct Span {
	public int Lenght;
	public int Start;
	public ConsoleColor ForegroundColor;
	public ConsoleColor? BackgroundColor;
	public int Priority;
}
class Converter {


	public List<Span> convertFindlingsToSpans(List<Findling> findlings) {
		List<Span> spans = new();
		foreach (Findling findling in findlings) {
			spans.Add(findlingToSpan(findling));
		}
		return spans;
	}

	public List<Span> convertTokensToSpans(List<Token> tokens) {
		List<Span> spans = new();
		foreach (Token token in tokens) {
			spans.Add(tokenToSpan(token));
		}
		return spans;
	}


	public Span convertSelectionToSpan(int start, int length) {
		return selectionToSpan(start, length);
	}



	Span tokenToSpan(Token t) {
		return new Span {
			Lenght = t.Length,
			Start = t.Start,
			ForegroundColor = getColor(t.tokenKind),
			BackgroundColor = null,
			Priority = 1,
		};
	}

	Span findlingToSpan(Findling f) {
		return new Span {
			Lenght = f.Length,
			Start = f.Start,
			ForegroundColor = ConsoleColor.Black,
			BackgroundColor = ConsoleColor.Yellow,
			Priority = 5,
		};
	}

	Span selectionToSpan(int start, int length) {
		return new Span {
			Start = start,
			Lenght = length,
			ForegroundColor = ConsoleColor.Black,
			BackgroundColor = ConsoleColor.Cyan,
			Priority = 10
		};
	}

	public ConsoleColor getColor(TokenKind tokenKind) {
		switch (tokenKind) {

			case TokenKind.Identifier:
				return ConsoleColor.White;
			case TokenKind.Keyword:
				return ConsoleColor.Red;
			case TokenKind.Number:
				return ConsoleColor.DarkBlue;
			case TokenKind.String:
				return ConsoleColor.DarkYellow;
			case TokenKind.Whitespace:
				return ConsoleColor.White;
			case TokenKind.Unknown:
				return ConsoleColor.White;
			case TokenKind.Operator:
				return ConsoleColor.Red;
			case TokenKind.Comment:
				return ConsoleColor.Gray;
			default:
				return ConsoleColor.White;

		}
	}
}

