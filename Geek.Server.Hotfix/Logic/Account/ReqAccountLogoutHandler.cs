using Geek.Server.Core.Net.BaseHandler;

namespace Server.Logic.Logic.Account
{
	[MsgMapping(typeof(ReqAccountLogout))]
	public class ReqAccountLogoutHandler : RoleCompHandler<AccountCompAgent>
	{
		public override async Task ActionAsync()
		{
			await Comp.OnLogout();
		}
	}
}