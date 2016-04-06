﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Twice.Models.Configuration;
using Twice.ViewModels.Settings;

namespace Twice.Tests.ViewModels.Settings
{
	[TestClass]
	public class PopupNotificationSettingsTests
	{
		[TestMethod, TestCategory( "ViewModel.Settings" )]
		public void SavedValuesAreAppliedDuringConstruction()
		{
			// Arrange
			var notify = new NotificationConfig
			{
				PopupEnabled = true,
				PopupDisplayCorner = Corner.TopLeft,
				PopupDisplay = "TestDisplay"
			};
			var cfg = new Mock<IConfig>();
			cfg.SetupGet( c => c.Notifications ).Returns( notify );

			// Act
			var vm = new PopupNotificationSettings( cfg.Object );

			// Assert
			Assert.AreEqual( notify.PopupEnabled, vm.Enabled );
			Assert.AreEqual( notify.PopupDisplayCorner, vm.SelectedCorner );
			Assert.AreEqual( notify.PopupDisplay, vm.SelectedDisplay );
		}
	}
}