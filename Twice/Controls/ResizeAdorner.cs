﻿using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Twice.Controls
{
	internal class ResizeAdorner : Adorner
	{
		public ResizeAdorner( ContentControl adornedElement )
			   : base( adornedElement )
		{
			Visuals = new VisualCollection( this );
		}

		protected override Visual GetVisualChild( int index )
		{
			return Visuals[index];
		}

		protected override int VisualChildrenCount => Visuals.Count;

		private readonly VisualCollection Visuals;
	}
}