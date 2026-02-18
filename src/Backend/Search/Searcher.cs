using System.Runtime.InteropServices;

class Searcher {
	Buffer buffer;
	public bool isSearching;
	public List<char> searchedChars;

	int? currentFindInd = null;
	public List<List<Findling>> findlings;

	public Searcher(Buffer buffer) {
		this.buffer = buffer;
		findlings = new();
		searchedChars = new List<char>();
	}

	public void moveToNextFind() {
		Findling? nextFindling = null;

		int totalFinds = 0;
		foreach (var list in findlings) {
			totalFinds += list.Count;
		}

		//wrap arrround forward;
		currentFindInd++;
		currentFindInd = (currentFindInd ?? 0) % totalFinds;

		int test = currentFindInd ?? 0;

		for (int i = 0; i < findlings.Count; i++) {
			if (test < findlings[i].Count) {
				nextFindling = findlings[i][test];
				break;
			}
			else {
				test -= findlings[i].Count;
			}
		}


		if (nextFindling != null) {
			buffer.column = nextFindling.Value.Start;
			buffer.line = nextFindling.Value.line;
			buffer.clampCursor();
		}

	}

	public void moveToPrevFind() {
		int totalFinds = 0;
		foreach (var list in findlings) totalFinds += list.Count;
		if (totalFinds == 0) return;

		//wrap arorund backwards;
		currentFindInd = (currentFindInd ?? 0) - 1;
		currentFindInd = (currentFindInd + totalFinds) % totalFinds;

		int test = currentFindInd ?? 0;
		Findling? prevFindling = null;
		for (int i = 0; i < findlings.Count; i++) {
			if (test < findlings[i].Count) {
				prevFindling = findlings[i][test];
				break;
			}
			else {
				test -= findlings[i].Count;
			}
		}

		if (prevFindling != null) {
			buffer.column = prevFindling.Value.Start;
			buffer.line = prevFindling.Value.line;
			buffer.clampCursor();
		}
	}


	public void setSearch(List<char> chars) {
		isSearching = true;
		searchedChars = chars;
		searchFile();
	}

	public void clearSearch() {
		isSearching = false;
		searchedChars.Clear();
	}
	public void searchFile() {
		if (searchedChars.Count == 0) {
			return;
		}

		findlings.Clear();

		for (int i = 0; i < buffer.lines.Count; i++) {
			var lineFindlings = searchLine(i);

			if (lineFindlings.Count > 0) {
				findlings.Add(lineFindlings);
			}
			else {
				findlings.Add(new List<Findling>());
			}
		}
	}
	public List<Findling> searchLine(int lineInd) {
		if (searchedChars.Count == 0) {
			return new List<Findling>();
		}
		List<Findling> findlings = new List<Findling>();

		List<char> line = buffer.lines[lineInd];
		for (int i = 0; i <= line.Count - searchedChars.Count; i++) {

			if (line[i] == searchedChars[0]) {
				int y = 1;
				int length = 1;
				bool match = true;

				while (y < searchedChars.Count) {
					if (line[i + y] == searchedChars[y]) {
						length++;
						y++;
					}
					else {
						match = false;
						break;
					}
				}

				if (match) {
					findlings.Add(new Findling {
						Start = i,
						Length = length,
						line = lineInd,
					});
				}
			}
		}

		return findlings;
	}
}
