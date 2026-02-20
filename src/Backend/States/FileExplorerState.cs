class FileExplorerState : State {
	FileExplorer fileExplorer;
	FileExplorerRenderer fileExplorerRenderer;

	StateManager stateManager;
	public FileExplorerState(StateManager stateManager, string baseDir) {
		this.stateManager = stateManager;
		fileExplorer = new(baseDir);
		fileExplorerRenderer = new(fileExplorer);
	}
	override public async Task handleInput(ConsoleKeyInfo keyInfo) {
		Console.CursorVisible = false;
		fileExplorerRenderer.ResetDirectoryView();
		fileExplorerRenderer.RenderDirectroys();


		if (keyInfo.Key == ConsoleKey.Q && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) {
			stateManager.SwitchState(ProgrammState.CLosed);
			return;
		}
		else if (keyInfo.Key == ConsoleKey.Enter) {
			string? filePath = fileExplorer.MoveIntoEntry();
			if (filePath is not null) {
				stateManager.OpenFileInEditor(filePath);
				return;
			}
			fileExplorerRenderer.UpdateCurrentDirInfo();
			fileExplorerRenderer.ResetDirectoryView();
			fileExplorerRenderer.RenderDirectroys();
		}
		else if (keyInfo.Key == ConsoleKey.Backspace) {
			fileExplorer.MoveOutOfEntry();
			fileExplorerRenderer.UpdateCurrentDirInfo();
			fileExplorerRenderer.ResetDirectoryView();
			fileExplorerRenderer.RenderDirectroys();
		}
		else if (keyInfo.Key == ConsoleKey.DownArrow) {
			fileExplorer.MoveToNextEntry();
			fileExplorerRenderer.ResetDirectoryView();
			fileExplorerRenderer.RenderDirectroys();
		}
		else if (keyInfo.Key == ConsoleKey.UpArrow) {
			fileExplorer.MoveToPrevEntry();
			fileExplorerRenderer.ResetDirectoryView();
			fileExplorerRenderer.RenderDirectroys();
		}
		else if (keyInfo.Key == ConsoleKey.Escape) {
			stateManager.SwitchState(ProgrammState.Editor);
			return;
		}


	}

	public override void Render() {
		Console.CursorVisible = false;
		fileExplorerRenderer.ResetDirectoryView();
		fileExplorerRenderer.RenderDirectroys();
	}
}
