using Geek.Server.App.Logic.Account;
using Geek.Server.Core.Hotfix.Agent;

namespace Server.Logic.Logic.Account
{
	public class AccountCompAgent : StateCompAgent<AccountComp, AccountState>
	{
		private static readonly Logger Log = LogManager.GetCurrentClassLogger();
	}
}