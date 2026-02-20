using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

class Program {
	static async Task Main() {
		StateManager stateManager = new(@"C:\");
		await stateManager.StartStateManager();
	}

}

