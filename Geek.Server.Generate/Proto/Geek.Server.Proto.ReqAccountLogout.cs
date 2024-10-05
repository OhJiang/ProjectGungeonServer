//auto generated, do not modify it

using MessagePack;

namespace Geek.Server.Proto
{
	[MessagePackObject(true)]
	public class ReqAccountLogout : Message
	{
		[IgnoreMember]
		public const int Sid = 1566781524;

		[IgnoreMember]
		public const int MsgID = Sid;
		[IgnoreMember]
		public override int MsgId => MsgID;

		public long AccountId { get; set; }
	}
}
