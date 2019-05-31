﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Valigator.Core.ValueDescriptors
{
	public class InvertValueDescriptor : IValueDescriptor
	{
		public IValueDescriptor InvertedDescriptor { get; }

		public InvertValueDescriptor(IValueDescriptor invertedDescriptor) 
			=> InvertedDescriptor = invertedDescriptor;
	}
}