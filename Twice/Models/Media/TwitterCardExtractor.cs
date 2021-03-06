﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Twice.Models.Twitter;

namespace Twice.Models.Media
{
	internal class TwitterCardExtractor : ITwitterCardExtractor
	{
		internal TwitterCard ExtractCard( string http )
		{
			int headStart = http.IndexOf( "<head>", StringComparison.OrdinalIgnoreCase );
			if( headStart == -1 )
			{
				return null;
			}

			int headEnd = http.IndexOf( "</head>", StringComparison.OrdinalIgnoreCase );
			if( headEnd == -1 )
			{
				return null;
			}

			string html = http.Substring( 0, headEnd ) + "</html>";
			html = WebUtility.HtmlDecode( html );

			var doc = new HtmlDocument();
			doc.LoadHtml( html );

			var metaNodes = doc.DocumentNode?.SelectSingleNode( "html/head" )?.SelectNodes( "meta" );
			if( metaNodes == null )
			{
				return null;
			}

			Dictionary<string, string> twitterMetaInfo = new Dictionary<string, string>();

			foreach( var meta in metaNodes )
			{
				var name = meta.Attributes["name"]?.Value;
				var value = meta.Attributes["content"]?.Value;

				if( name?.StartsWith( "twitter:", StringComparison.OrdinalIgnoreCase ) == true && value != null )
				{
					if( !twitterMetaInfo.ContainsKey( name ) )
					{
						twitterMetaInfo.Add( name, value );
					}
				}
			}

			return new TwitterCard( twitterMetaInfo );
		}

		public async Task<TwitterCard> ExtractCard( Uri url )
		{
			if( url.Host.Equals( "twitter.com", StringComparison.OrdinalIgnoreCase ) ||
			    url.Host.Equals( "www.twitter.com", StringComparison.OrdinalIgnoreCase ) )
			{
				if( TwitterHelper.IsExtendedTweetUrl( url.AbsolutePath ) )
				{
					return null;
				}
			}

			using( var client = new HttpClient() )
			{
				var response = await client.GetAsync( url );
				var str = await response.Content.ReadAsStringAsync();

				return ExtractCard( str );
			}
		}

		internal static ITwitterCardExtractor Default { get; } = new TwitterCardExtractor();
	}
}