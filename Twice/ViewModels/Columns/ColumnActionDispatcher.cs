using System;

namespace Twice.ViewModels.Columns
{
	internal class ColumnActionDispatcher : IColumnActionDispatcher
	{
		public event EventHandler BottomReached;

		public event EventHandler HeaderClicked;

		public void OnBottomReached()
		{
			BottomReached?.Invoke( this, EventArgs.Empty );
		}

		public void OnHeaderClicked()
		{
			HeaderClicked?.Invoke( this, EventArgs.Empty );
		}
	}
}