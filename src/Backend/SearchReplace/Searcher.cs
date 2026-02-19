using System.Runtime.InteropServices;

class Searcher {
	Buffer buffer;
	public bool isSearching;
	public List<char> searchedChars;
	public int totalFinds = 0;

	public int? currentFindInd = null;
	public List<List<Findling>> findlings;

	public Searcher(Buffer buffer) {
		this.buffer = buffer;
		findlings = new();
		searchedChars = new List<char>();
	}

	private static int normalizeIndex(int index, int count) {
		return ((index % count) + count) % count;
	}


	public void clampCurrentFindInd() {
		if (totalFinds == 0) {
			currentFindInd = null;
			return;
		}

		currentFindInd = normalizeIndex(currentFindInd ?? 0, totalFinds);
	}
	public Findling? getFindlingByInd(int findlingInd) {
		if (totalFinds == 0) {
			return null;
		}

		findlingInd = normalizeIndex(findlingInd, totalFinds);

		int test = findlingInd;
		Findling? findling = null;
		for (int i = 0; i < findlings.Count; i++) {
			if (test < findlings[i].Count) {
				findling = findlings[i][test];
				break;
			}
			else {
				test -= findlings[i].Count;
			}
		}

		return findling;
	}

	public void calculateTotalFindlings() {
		totalFinds = 0;
		foreach (var list in findlings) {
			totalFinds += list.Count;
		}
	}

	public void removeFindlingByInd(int findlingInd) {

		findlingInd = normalizeIndex(findlingInd, totalFinds);

		int test = findlingInd;

		for (int i = 0; i < findlings.Count; i++) {
			if (test < findlings[i].Count) {
				findlings[i].RemoveAt(test);
				break;
			}
			else {
				test -= findlings[i].Count;
			}
		}

		calculateTotalFindlings();
	}
	public void moveToNextFind() {
		if (totalFinds == 0) {
			return;
		}

		int nextIndex = currentFindInd.HasValue ? currentFindInd.Value + 1 : 0;
		Findling? nextFindling = getFindlingByInd(nextIndex);

		if (nextFindling != null) {
			currentFindInd = nextIndex;
			clampCurrentFindInd();
			buffer.column = nextFindling.Value.Start;
			buffer.line = nextFindling.Value.line;
			buffer.clampCursor();
		}

	}
	public void moveToPrevFind() {
		if (totalFinds == 0) {
			return;
		}

		int prevIndex = currentFindInd.HasValue ? currentFindInd.Value - 1 : totalFinds - 1;
		Findling? prevFindling = getFindlingByInd(prevIndex);

		if (prevFindling != null) {
			currentFindInd = prevIndex;
			clampCurrentFindInd();
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
		totalFinds = 0;
		currentFindInd = null;
		findlings.Clear();
	}
	public void searchFile() {
		if (searchedChars.Count == 0) {
			totalFinds = 0;
			currentFindInd = null;
			findlings.Clear();
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
		calculateTotalFindlings();
		clampCurrentFindInd();

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
