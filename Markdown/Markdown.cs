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
			return fsm.GetResult(markdown);
		}

	}
}