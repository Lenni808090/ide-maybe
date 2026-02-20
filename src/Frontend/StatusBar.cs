using System;
using System.IO;

class StatusBar {
	Buffer buffer;
	Func<string> getCurrentFilePath;
	Searcher searcher;
	Replacer replacer;

	public StatusBarMode statusBarMode;

	string search = "";
	bool showReplace;
	string replace = "";
	int line;
	int column;
	FileData fileData;
	string filePath;
	string? cachedFilePath;
	long cachedFileSizeBytes = -1;
	long cachedLastWriteUtcTicks = -1;

	public StatusBar(Buffer buffer, Func<string> getCurrentFilePath, Searcher searcher, Replacer replacer) {
		this.buffer = buffer;
		this.searcher = searcher;
		this.replacer = replacer;
		this.getCurrentFilePath = getCurrentFilePath;
		this.filePath = getCurrentFilePath();
		this.fileData = new FileData("", "0 KB", "Unknown");
	}

	public (string filePath, FileData fileData, int column, int line, StatusBarMode statusBarMode, string searchedChars, string replaceChars, bool showReplace) GetData() {
		UpdateStatusBar();

		return (filePath, fileData, column, line, statusBarMode, search, replace, showReplace);
	}

	public void UpdateStatusBar() {
		filePath = getCurrentFilePath();
		RefreshFileDataIfNeeded();
		column = buffer.column;
		line = buffer.line;

		if (statusBarMode == StatusBarMode.Normal) {
			search = "";
			replace = "";
			showReplace = false;
		}
		else if (statusBarMode == StatusBarMode.Search) {
			search = new string(searcher.searchedChars.ToArray());
			if (replacer.isReplacing) {
				showReplace = true;
				replace = new string(replacer.charsUsedToReplace.ToArray());
			}
			else {
				showReplace = false;
				replace = "";
			}
		}
	}

	private void RefreshFileDataIfNeeded() {
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
			fileData = new FileData("", "0 KB", "Unknown");
			cachedFilePath = null;
			cachedFileSizeBytes = -1;
			cachedLastWriteUtcTicks = -1;
			return;
		}

		FileInfo fileInfo = new FileInfo(filePath);
		long currentSize = fileInfo.Length;
		long currentLastWrite = fileInfo.LastWriteTimeUtc.Ticks;
		bool sameFile = string.Equals(cachedFilePath, filePath, StringComparison.Ordinal);
		bool unchanged = sameFile && cachedFileSizeBytes == currentSize && cachedLastWriteUtcTicks == currentLastWrite;
		if (unchanged) {
			return;
		}

		double sizeInKb = currentSize / 1024.0;
		string fileSize = $"{sizeInKb:F2} KB";
		string extension = Path.GetExtension(filePath);
		string encoding;

		using (var reader = new StreamReader(filePath, true)) {
			reader.Peek();
			encoding = reader.CurrentEncoding.EncodingName;
		}

		cachedFilePath = filePath;
		cachedFileSizeBytes = currentSize;
		cachedLastWriteUtcTicks = currentLastWrite;
		fileData = new FileData(extension, fileSize, encoding);
	}
}

public record FileData(string Extension, string FileSize, string Encoding);

enum StatusBarMode {
	Normal,
	Search,

}

