using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class FileManager {

	public List<List<char>> ReadFile(string filePath) {
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

	public void SaveFile(string path, List<List<char>> contentList) {
		string tempPath = path + ".tmp";
		var sb = new StringBuilder();

		for (int i = 0; i < contentList.Count; i++) {
			sb.Append(contentList[i].ToArray());
			if (i < contentList.Count - 1)
				sb.Append(Environment.NewLine);
		}

		string text = sb.ToString();

		File.WriteAllText(tempPath, text);
		File.Replace(tempPath, path, null);
	}
}

