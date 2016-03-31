﻿namespace Twice.ViewModels.Columns.Definitions
{
	internal class ColumnDefinition
	{
		public ColumnDefinition( ColumnType type )
		{
			Type = type;
			Width = 300;
		}

		public ColumnNotifications Notifications { get; set; }
		public ulong[] SourceAccounts { get; set; }
		public ulong[] TargetAccounts { get; set; }
		public ColumnType Type { get; set; }
		public int Width { get; set; }
	}
}