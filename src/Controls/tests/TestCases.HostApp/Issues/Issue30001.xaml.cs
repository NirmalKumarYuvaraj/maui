using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample.Issues
{
	[Issue(IssueTracker.Github, 30001, "Rectangle inside grid is not properly aligned and does not appear on Android", PlatformAffected.Android)]
	public partial class Issue30001 : ContentPage
	{
		public Issue30001()
		{
			InitializeComponent();
		}
	}
}