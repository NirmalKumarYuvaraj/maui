using System;
using Microsoft.Maui.Handlers;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
	class LabelHandlerStub : ViewHandler<ILabel, object>
	{
		public LabelHandlerStub() : base(new PropertyMapper<ILabel, LabelHandlerStub>())
		{
		}

		protected override object CreatePlatformView() => throw new NotImplementedException();
	}
}
