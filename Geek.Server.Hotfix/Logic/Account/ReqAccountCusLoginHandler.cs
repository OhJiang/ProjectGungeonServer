using Geek.Server.Core.Net.BaseHandler;
using Geek.Server.Core.Net.Tcp.Handler;

namespace Server.Logic.Logic.Account
{
	[MsgMapping(typeof(ReqAccountCusLogin))]
	public class ReqAccountCusLoginHandler : BaseMessageHandler
	{
		public override async Task ActionAsync()
		{
			await AccountManager.Instance.OnCustomLogin(Channel, Msg as ReqAccountCusLogin);
		}
	}
}