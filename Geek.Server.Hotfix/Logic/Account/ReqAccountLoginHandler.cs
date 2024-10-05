using Geek.Server.Core.Net.BaseHandler;
using Geek.Server.Core.Net.Tcp.Handler;

namespace Server.Logic.Logic.Account
{
	[MsgMapping(typeof(ReqAccountLogin))]
	public class ReqAccountLoginHandler : BaseMessageHandler
	{
		public override async Task ActionAsync()
		{
			await AccountManager.Instance.OnSDKLogin(Channel, Msg as ReqAccountLogin);
		}
	}
}