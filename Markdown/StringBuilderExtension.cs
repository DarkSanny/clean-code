using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markdown
{
	public static class StringBuilderExtension
	{

		public static string ToHtmlContainer(this StringBuilder builder, string tag)
		{
			return $"<{tag}>{builder}</{tag}>";
		}

	}

}
