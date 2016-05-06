using System.Collections.Generic;
using System.Threading.Tasks;
using LinqToTwitter;

namespace Twice.Models.Twitter.Repositories
{
	internal interface ITwitterUserRepository
	{
		Task<List<User>> LookupUsers( string userList );

		Task<List<User>> Search( string query );

		Task<User> ShowUser( ulong userId, bool includeEntities );
	}
}