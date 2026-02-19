class FileExplorerRenderer {
	FileExplorer fileExplorer;
	int topDirectoryInd;
	int bottomDirectoryInd;
	(List<DirectoryInfo> directoryInfos, List<FileInfo> fileInfos) currentDirInfo;
	public void resetDirectoryView() {
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
		updateCurrentDirInfo();
		int maxTop = Math.Max(0, fileExplorer.getTotalEntriesCount(currentDirInfo.directoryInfos, currentDirInfo.fileInfos) - h);
		if (topDirectoryInd > maxTop) {
			topDirectoryInd = maxTop;
		}

		bottomDirectoryInd = Math.Min(topDirectoryInd + h, fileExplorer.getTotalEntriesCount(currentDirInfo.directoryInfos, currentDirInfo.fileInfos));
	}

	public int getScreenLine(int i) {
		return i - topDirectoryInd;
	}

	public void updateCurrentDirInfo() {
		currentDirInfo = fileExplorer.getInfoAboutCurrentDir();
	}
	public FileExplorerRenderer(FileExplorer fileExplorer) {
		this.fileExplorer = fileExplorer;
	}

	public void renderDirectroys() {
		for (int i = topDirectoryInd; i < bottomDirectoryInd; i++) {
			Console.SetCursorPosition(0, getScreenLine(i));
			if (fileExplorer.currentHoveredFile == i) {
				Console.BackgroundColor = ConsoleColor.Cyan;
			}
			else {
				Console.BackgroundColor = ConsoleColor.Black;
			}
			if (i < currentDirInfo.directoryInfos.Count) {
				Console.Write(currentDirInfo.directoryInfos[i].Name);
			}
			else {
				Console.Write("--" + currentDirInfo.fileInfos[i - currentDirInfo.directoryInfos.Count]);
			}
		}

	}

}
