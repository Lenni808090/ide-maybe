enum TokenKind {
	Identifier,
	Keyword,
	Number,
	String,
	Whitespace,
	Unknown,
	Operator,
	Comment,
}

struct Token {
	public int Start;
	public int Length;
	public TokenKind tokenKind;

}
