class FileExplorerState : State {
	FileExplorer fileExplorer;
	FileExplorerRenderer fileExplorerRenderer;

	StateManager stateManager;
	public FileExplorerState(StateManager stateManager) {
		this.stateManager = stateManager;
		fileExplorer = new();
		fileExplorerRenderer = new(fileExplorer);
	}
	override public async Task handleInput(ConsoleKeyInfo keyInfo) {
		Console.Clear();
		Console.CursorVisible = false;
		fileExplorerRenderer.ResetDirectoryView();
		fileExplorerRenderer.RenderDirectroys();


		if (keyInfo.Key == ConsoleKey.Q && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) {
			stateManager.SwitchState(ProgrammState.CLosed);
			return;
		}
		else if (keyInfo.Key == ConsoleKey.Enter) {
			fileExplorer.MoveIntoEntry();
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
			return;
		}


	}

	public override void Render() {
		fileExplorerRenderer.ResetDirectoryView();
		fileExplorerRenderer.RenderDirectroys();
	}
}
