using System;
using System.Collections.Generic;
using System.Text;

namespace Markdown
{
	public class Markdown
	{

		private Fsm fsm;

		public Markdown()
		{
			fsm = new Fsm();
		}

		public string RenderToHtml(string markdown)
		{
			fsm.SetUp(markdown);
			while (fsm.Result == null)
				fsm.Updete();
			return fsm.Result;
		}

	}
}