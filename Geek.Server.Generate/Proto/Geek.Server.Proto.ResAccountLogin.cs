//auto generated, do not modify it

using MessagePack;

namespace Geek.Server.Proto
{
	[MessagePackObject(true)]
	public class ResAccountLogin : Message
	{
		[IgnoreMember]
		public const int Sid = -1342419148;

		[IgnoreMember]
		public const int MsgID = Sid;
		[IgnoreMember]
		public override int MsgId => MsgID;

		/// <summary>
		/// 登陆结果，0成功，其他时候为错误码
		/// </summary>
		public int Code { get; set; }

		public UserInfo UserInfo { get; set; }
		public string CustomToken { get; set; }
	}
}
