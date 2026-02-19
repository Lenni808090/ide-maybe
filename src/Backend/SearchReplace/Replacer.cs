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
	}

	public void replaceAllFindilngs() {

	}

	public void setCharsUsedToReplace(List<char> chars) {
		charsUsedToReplace = chars;
	}
}
