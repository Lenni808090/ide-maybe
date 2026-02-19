class Replacer {

	Buffer buffer;
	Searcher searcher;
	List<char> charsUsedToReplace = new List<char>();

	public Replacer(Buffer buffer, Searcher searcher) {
		this.buffer = buffer;
		this.searcher = searcher;
	}

	public void replaceCurrentFindling() {
		if (searcher.currentFindInd == null) return;
		Findling? currentFindling = searcher.getFindlingByInd(searcher.currentFindInd.Value);
		if (currentFindling == null) return;
		buffer.removeArea(currentFindling.Value.line, currentFindling.Value.line, currentFindling.Value.Start, currentFindling.Value.Start + currentFindling.Value.Length);
		buffer.insertCharsAtPos(currentFindling.Value.line, currentFindling.Value.Start, charsUsedToReplace);
		buffer.clampCursor();
	}

	public void replaceAllFindilngs() {
		Findling? currentFindling;
		for (int i = searcher.findlings.Count - 1; i >= 0; i--) {
			currentFindling = searcher.getFindlingByInd(i);
			if (currentFindling == null) return;
			buffer.removeArea(currentFindling.Value.line, currentFindling.Value.line, currentFindling.Value.Start, currentFindling.Value.Start + currentFindling.Value.Length);
			buffer.insertCharsAtPos(currentFindling.Value.line, currentFindling.Value.Start, charsUsedToReplace);
		}
		buffer.clampCursor();
	}

	public void setCharsUsedToReplace(List<char> chars) {
		charsUsedToReplace = chars;
	}
}
