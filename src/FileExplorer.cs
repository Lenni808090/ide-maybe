using System;
using System.IO;
using System.Collections.Generic;
class FileExplorer {

	public List<List<char>> readFile(string filePath) {
		string[] lines = File.ReadAllLines(filePath);
		List<List<char>> readFile = new List<List<char>>();
		foreach (string line in lines) {
			readFile.Add([.. line]);
		}
		if (readFile.Count == 0) {
			readFile.Add(new List<char>());
		}
		return readFile;
	}
}
