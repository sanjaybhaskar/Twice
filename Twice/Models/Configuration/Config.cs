﻿using System.IO;
using Newtonsoft.Json;

namespace Twice.Models.Configuration
{
	internal interface IConfig
	{
		void Save();

		GeneralConfig General { get; }
		VisualConfig Visual { get; }
	}

	internal class Config : IConfig
	{
		public Config( string fileName )
		{
			FileName = fileName;

			if( File.Exists( FileName ) )
			{
				string json = File.ReadAllText( FileName );

				Config tmp = JsonConvert.DeserializeObject<Config>( json );
				Visual = tmp.Visual;
				General = tmp.General;
			}
			else
			{
				General = new GeneralConfig();
				Visual = new VisualConfig();
			}
		}

		public void Save()
		{
			string json = JsonConvert.SerializeObject( this );
			File.WriteAllText( FileName, json );
		}

		public GeneralConfig General { get; }
		public VisualConfig Visual { get; }

		private readonly string FileName;
	}
}