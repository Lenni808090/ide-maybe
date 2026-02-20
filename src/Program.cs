using System;
using System.Collections.Generic;
using System.ComponentModel;

class Program {
	static async Task Main() {
		Editor editor = new Editor();
		await editor.StartEditor();
	}

}

