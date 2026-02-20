using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class FileExplorer {
	string baseDir;
	string currentDir;

	public int currentHoveredFile;
	public FileExplorer() {
		baseDir = @"C:\";
		currentDir = @"C:\";
	}

	public void moveToNextEntry() {
		var dirInfo = getInfoAboutCurrentDir();
		int totalEntryCount = getTotalEntriesCount(dirInfo.directoryInfos, dirInfo.fileInfos);
		if (currentHoveredFile == totalEntryCount - 1) {
			return;
		}
		currentHoveredFile++;
	}


	public void moveToPrevEntry() {
		var dirInfo = getInfoAboutCurrentDir();
		int totalEntryCount = getTotalEntriesCount(dirInfo.directoryInfos, dirInfo.fileInfos);
		if (currentHoveredFile == 0) {
			return;
		}
		currentHoveredFile--;
	}
	public (List<DirectoryInfo> directoryInfos, List<FileInfo> fileInfos) getInfoAboutDir(string currentDir) {
		var currentDirInfo = new DirectoryInfo(currentDir);
		List<DirectoryInfo> directoryInfos = new();
		List<FileInfo> fileInfos = new();

		foreach (FileSystemInfo entry in currentDirInfo.EnumerateFileSystemInfos()) {
			if (entry is DirectoryInfo dir) {
				directoryInfos.Add(dir);
			}
			else if (entry is FileInfo file) {
				fileInfos.Add(file);
			}
		}

		return (directoryInfos, fileInfos);
	}

	public (List<DirectoryInfo> directoryInfos, List<FileInfo> fileInfos) getInfoAboutCurrentDir() {
		return getInfoAboutDir(currentDir);
	}

	public int getTotalEntriesCount(List<DirectoryInfo> directoryInfos, List<FileInfo> fileInfos) {
		return directoryInfos.Count + fileInfos.Count;
	}
	public void setBaseDir(string baseDir) {
		this.baseDir = baseDir;
	}

	public void setCurrentDir(string currentDir) {
		this.currentDir = currentDir;
	}

}
