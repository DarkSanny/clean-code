using System;
using System.Collections.Generic;
using System.Text;

namespace Markdown
{
	public class Markdown
	{


		public string RenderToHtml(string markdown)
		{
			return Fsm.GetResult(markdown);
		}

	}
}