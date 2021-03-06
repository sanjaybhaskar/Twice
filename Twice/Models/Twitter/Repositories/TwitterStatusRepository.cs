using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Anotar.NLog;
using Fody;
using LinqToTwitter;
using Twice.Models.Cache;
using Twice.Models.Twitter.Comparers;

namespace Twice.Models.Twitter.Repositories
{
	[ExcludeFromCodeCoverage]
	[ConfigureAwait( false )]
	internal class TwitterStatusRepository : TwitterRepositoryBase, ITwitterStatusRepository
	{
		public TwitterStatusRepository( TwitterContext context, ICache cache )
			: base( context, cache )
		{
		}

		public Task<Status> DeleteTweetAsync( ulong statusId )
		{
			return Context.DeleteTweetAsync( statusId );
		}

		public async Task<List<Status>> Filter( params Expression<Func<Status, bool>>[] filterExpressions )
		{
			IQueryable<Status> query = Queryable;

			foreach( var filter in filterExpressions )
			{
				query = query.Where( filter );
			}

			var statusList = await query.ToListAsync();
			await Cache.AddStatuses( statusList );
			return statusList;
		}

		public async Task<List<ulong>> FindRetweeters( ulong statusId, int count )
		{
			Status response;
			try
			{
				response = await Queryable.Where( s => s.Type == StatusType.Retweeters && s.ID == statusId )
					.SingleOrDefaultAsync();
			}
			catch( Exception ex )
			{
				LogTo.WarnException( "Exception while searching retweeters:", ex );
				return new List<ulong>();
			}

			List<ulong> result = response?.Users ?? new List<ulong>();
			return result.Take( count ).ToList();
		}

		public async Task<Status> GetTweet( ulong statusId, bool includeEntities )
		{
			var cached = await Cache.GetStatus( statusId );
			if( cached != null )
			{
				return cached;
			}

			try
			{
				var status = await Queryable.Where( s => s.Type == StatusType.Show && s.ID == statusId
				                                         && s.IncludeEntities == includeEntities ).FirstOrDefaultAsync();

				await Cache.AddStatuses( new[] {status} );
				return status;
			}
			catch( Exception ex )
			{
				LogTo.ErrorException( $"Failed to retrieve status with id {statusId}", ex );
				return null;
			}
		}

		public async Task<List<Status>> GetUserTweets( ulong userId, ulong since = 0, ulong max = 0 )
		{
			Debug.Assert( since == 0 || max == 0 );

			IEnumerable<Status> cached = await Cache.GetStatusesForUser( userId );
			if( since != 0 )
			{
				var since1 = since;
				cached = cached.Where( c => c.GetStatusId() > since1 );
			}
			if( max != 0 )
			{
				var max1 = max;
				cached = cached.Where( c => c.GetStatusId() < max1 );
			}

			var cachedList = cached.ToList();
			var query = Queryable.Where( s => s.Type == StatusType.User && s.UserID == userId && s.Count == 20 );

			if( since != 0 )
			{
				if( cachedList.Any() )
				{
					since = Math.Max( cachedList.Max( c => c.GetStatusId() ), since );
				}
				query = query.Where( s => s.SinceID == since );
			}
			if( max != 0 )
			{
				if( cachedList.Any() )
				{
					max = Math.Min( cachedList.Min( c => c.GetStatusId() ), max );
				}
				query = query.Where( s => s.MaxID == max );
			}

			List<Status> statusList;
			try
			{
				statusList = await query.ToListAsync();
			}
			catch( Exception ex )
			{
				LogTo.ErrorException( $"Failed to load tweets for user {userId} (since: {since}, max: {max})", ex );
				return cachedList;
			}

			var newStatuses = statusList.Except( cachedList, TwitterComparers.StatusComparer ).ToList();
			await Cache.AddStatuses( newStatuses );

			cachedList.AddRange( newStatuses );
			return cachedList;
		}

		public async Task<List<Status>> List( ulong[] statusIds )
		{
			string idList = string.Join( ",", statusIds );
			return await Context.Status.Where( s => s.Type == StatusType.Lookup && s.TweetIDs == idList ).ToListAsync();
		}

		public Task<Status> RetweetAsync( ulong statusId )
		{
			return Context.RetweetAsync( statusId );
		}

		public Task<Status> TweetAsync( string text, IEnumerable<ulong> medias, ulong inReplyTo )
		{
			return inReplyTo != 0
				? Context.ReplyAsync( inReplyTo, text, medias )
				: Context.TweetAsync( text, medias );
		}

		private TwitterQueryable<Status> Queryable => Context.Status;
	}
}