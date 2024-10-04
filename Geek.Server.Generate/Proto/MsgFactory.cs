//auto generated, do not modify it

using System;
namespace Geek.Server.Proto
{
	public class MsgFactory
	{
		private static readonly System.Collections.Generic.Dictionary<int, Type> lookup;

        static MsgFactory()
        {
            lookup = new System.Collections.Generic.Dictionary<int, Type>(12)
            {
			    { 667869091, typeof(ClientProto.NetConnectMessage) },
			    { 1245418514, typeof(ClientProto.NetDisConnectMessage) },
			    { -593677237, typeof(Geek.Server.Proto.UserInfo) },
			    { 1267074761, typeof(Geek.Server.Proto.ReqLogin) },
			    { 785960738, typeof(Geek.Server.Proto.ResLogin) },
			    { 1587576546, typeof(Geek.Server.Proto.ResLevelUp) },
			    { 1575482382, typeof(Geek.Server.Proto.HearBeat) },
			    { 1179199001, typeof(Geek.Server.Proto.ResErrorCode) },
			    { 537499886, typeof(Geek.Server.Proto.ResPrompt) },
			    { 572437947, typeof(Geek.Server.Proto.ReqAccountLogin) },
			    { 893720281, typeof(Geek.Server.Proto.ReqAccountCusLogin) },
			    { -1342419148, typeof(Geek.Server.Proto.ResAccountLogin) },
            };
        }

        public static Type GetType(int msgId)
		{
			if (lookup.TryGetValue(msgId, out Type res))
				return res;
			else
				throw new Exception($"can not find msg type :{msgId}");
		}

	}
}
