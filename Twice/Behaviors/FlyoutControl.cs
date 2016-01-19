﻿using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Interactivity;
using Twice.Messages;
using Twice.ViewModels;

namespace Twice.Behaviors
{
	internal class FlyoutControl : Behavior<Flyout>
	{
		protected override void OnAttached()
		{
			base.OnAttached();

			Messenger.Default.Register<FlyoutMessage>( this, OnFlyoutMessage );
		}

		private void OnFlyoutMessage( FlyoutMessage message )
		{
			if( !message.Name.Equals( Name ) )
			{
				return;
			}

			switch( message.Action )
			{
			case FlyoutAction.Open:
				AssociatedObject.IsOpen = true;
				break;

			case FlyoutAction.Close:
				AssociatedObject.IsOpen = false;
				break;

			case FlyoutAction.Toggle:
				AssociatedObject.IsOpen = !AssociatedObject.IsOpen;
				break;
			}

			if( AssociatedObject.IsOpen )
			{
				if( message.DataContext != null )
				{
					AssociatedObject.DataContext = message.DataContext;
				}

				var resetable = AssociatedObject.DataContext as IResetable;
				resetable?.Reset();
			}
		}

		public string Name
		{
			get { return (string)GetValue( NameProperty ); }
			set { SetValue( NameProperty, value ); }
		}

		public static readonly DependencyProperty NameProperty = DependencyProperty.Register( "Name", typeof( string ), typeof( FlyoutControl ), new PropertyMetadata( null ) );
	}
}