using Geek.Server.App.Common.Event;
using Geek.Server.App.Common.Session;
using Geek.Server.App.Logic.Account;
using Geek.Server.Core.Actors;
using Geek.Server.Core.Events;
using Geek.Server.Core.Hotfix.Agent;
using Geek.Server.Core.Storage;
using Geek.Server.Core.Timer;
using Server.Logic.Logic.Server;

namespace Server.Logic.Logic.Account
{
	public class AccountCompAgent : StateCompAgent<AccountComp, AccountState>
	{
		private static readonly Logger Log = LogManager.GetCurrentClassLogger();

		public Task OnLogout()
		{
			SessionManager.Remove(ActorId);
			return Task.CompletedTask;
		}

		[Event(EventID.SessionRemove)]
		private class SessionRemoveEl : EventListener<AccountCompAgent>
		{
			protected override Task HandleEvent(AccountCompAgent agent, Event evt)
			{
				return agent.OnSessionRemove();
			}
		}

		private async Task OnSessionRemove()
		{
			//移除在线玩家
			Log.Debug($"玩家下线:{ActorId}");
			State.OfflineTime = DateTime.Now;
			var serverComp = await ActorMgr.GetCompAgent<ServerCompAgent>();
			await serverComp.RemoveOnlineRole(ActorId);
			SetAutoRecycle(true); // 下线后会被自动回收
			QuartzTimer.Unschedule(ScheduleIdSet);
		}
	}
}