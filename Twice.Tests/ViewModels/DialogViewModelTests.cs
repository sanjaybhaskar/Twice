﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Twice.ViewModels;
using Twice.ViewModels.Validation;

namespace Twice.Tests.ViewModels
{
	[TestClass]
	public class DialogViewModelTests
	{
		[TestMethod, TestCategory( "ViewModels" )]
		public void CancelCommandClosesDialog()
		{
			// Arrange
			var vm = new DialogViewModel();

			bool? result = null;
			vm.CloseRequested += ( s, e ) => result = e.Result;

			// Act
			vm.CancelCommand.Execute( null );

			// Assert
			Assert.AreEqual( false, result );
		}

		[TestMethod, TestCategory( "ViewModels" )]
		public void CommandInvocationWithoutListenersDoesNotCrash()
		{
			// Arrange
			var vm = new DialogViewModel();

			// Act
			vm.CancelCommand.Execute( null );
			vm.OkCommand.Execute( null );

			// Assert
		}

		[TestMethod, TestCategory( "ViewModels" )]
		public void OkCommandClosesDialog()
		{
			// Arrange
			var vm = new DialogViewModel();

			bool? result = null;
			vm.CloseRequested += ( s, e ) => result = e.Result;

			// Act
			vm.OkCommand.Execute( null );

			// Assert
			Assert.AreEqual( true, result );
		}

		[TestMethod, TestCategory( "ViewModels" )]
		public void OkCommandClosesWithValidData()
		{
			// Arrange
			var vm = new TestDialog();

			bool? result = null;
			vm.CloseRequested += ( s, e ) => result = e.Result;

			// Act
			vm.Data = "test";
			vm.OkCommand.Execute( null );

			// Assert
			Assert.AreEqual( true, result );
		}

		[TestMethod, TestCategory( "ViewModels" )]
		public void OkCommandDoesNotCloseWithInvalidData()
		{
			// Arrange
			var vm = new TestDialog();

			bool? result = null;
			vm.CloseRequested += ( s, e ) => result = e.Result;

			// Act
			vm.OkCommand.Execute( null );

			// Assert
			Assert.AreNotEqual( true, result );
		}

		private class TestDialog : DialogViewModel
		{
			public TestDialog()
			{
				Validate( () => Data ).NotEmpty();
			}

			public string Data { get; set; }
		}
	}
}