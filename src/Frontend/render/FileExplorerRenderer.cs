class FileExplorerRenderer {
	FileExplorer fileExplorer;
	int topDirectoryInd;
	int bottomDirectoryInd;
	(List<DirectoryInfo> directoryInfos, List<FileInfo> fileInfos) currentDirInfo;

	public FileExplorerRenderer(FileExplorer fileExplorer) {
		this.fileExplorer = fileExplorer;
		UpdateCurrentDirInfo();
	}
	public void ResetDirectoryView() {
		int h = Console.WindowHeight - 1;

		int margin = Math.Min(5, h / 3);

		if (fileExplorer.currentHoveredFile < topDirectoryInd + margin) {
			topDirectoryInd = fileExplorer.currentHoveredFile - margin;
		}
		else if (fileExplorer.currentHoveredFile >= topDirectoryInd + h - margin) {
			topDirectoryInd = fileExplorer.currentHoveredFile - (h - margin - 1);
		}

		if (topDirectoryInd < 0) {
			topDirectoryInd = 0;
		}
		int maxTop = Math.Max(0, fileExplorer.GetTotalEntriesCount(currentDirInfo.directoryInfos, currentDirInfo.fileInfos) - h);
		if (topDirectoryInd > maxTop) {
			topDirectoryInd = maxTop;
		}

		bottomDirectoryInd = Math.Min(topDirectoryInd + h, fileExplorer.GetTotalEntriesCount(currentDirInfo.directoryInfos, currentDirInfo.fileInfos));
	}

	public int GetScreenLine(int i) {
		return i - topDirectoryInd;
	}

	public void UpdateCurrentDirInfo() {
		currentDirInfo = fileExplorer.GetInfoAboutCurrentDir();
	}

	public void RenderDirectroys() {
		if (bottomDirectoryInd == 0) {
			Console.SetCursorPosition(0, 0);
			Console.Write("Empty");
			for (int i = 1; i < Console.WindowHeight; i++) {
				Console.SetCursorPosition(0, GetScreenLine(i));
				Console.Write("\x1b[K");
			}
		}
		else {
			for (int i = topDirectoryInd; i < bottomDirectoryInd; i++) {
				Console.SetCursorPosition(0, GetScreenLine(i));
				if (fileExplorer.currentHoveredFile == i) {
					Console.BackgroundColor = ConsoleColor.Cyan;
				}
				else {
					Console.BackgroundColor = ConsoleColor.Black;
				}
				var entry = fileExplorer.GetEntryByInd(i);
				if (!entry.isFile) {
					Console.ForegroundColor = ConsoleColor.DarkRed;
					Console.Write(entry.systemInfo.Name + "/");
				}
				else {
					Console.ForegroundColor = ConsoleColor.White;
					Console.Write("-" + entry.systemInfo.Name);
				}
				Console.ForegroundColor = ConsoleColor.White;
				Console.BackgroundColor = ConsoleColor.Black;
				Console.Write("\x1b[K");
			}
			if (bottomDirectoryInd < Console.WindowHeight - 1) {
				for (int i = bottomDirectoryInd; i < Console.WindowHeight; i++) {
					Console.SetCursorPosition(0, GetScreenLine(i));
					Console.Write("\x1b[K");
				}
			}
		}
	}

}

