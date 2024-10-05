using Geek.Server.App.Common;
using Geek.Server.Core.Net;
using Server.Logic.Common.Handler;

namespace Server.Logic.Logic.Account
{
	public interface ILoginValidator
	{
		bool Validate(ReqAccountLogin reqSdkLogin, NetChannel channel);
		bool Validate(ReqAccountCusLogin reqCusLogin, NetChannel channel);
	}

	public class PlatformValidator : ILoginValidator
	{
		public bool Validate(ReqAccountLogin reqSdkLogin, NetChannel channel)
		{
			if (!Platform.IsNotSpecifiedPlatform(reqSdkLogin.Platform)) return true;
			channel.Write(null, reqSdkLogin.UniId, StateCode.UnknownPlatform);
			return false;
		}

		public bool Validate(ReqAccountCusLogin reqCusLogin, NetChannel channel)
		{
			if (!Platform.IsNotSpecifiedPlatform(reqCusLogin.Platform)) return true;
			channel.Write(null, reqCusLogin.UniId, StateCode.UnknownPlatform);
			return false;
		}
	}

	public class SdkTypeValidator : ILoginValidator
	{
		public bool Validate(ReqAccountLogin reqSdkLogin, NetChannel channel)
		{
			if (Enum.IsDefined(typeof(SdkType), reqSdkLogin.SdkType)) return true;
			channel.Write(null, reqSdkLogin.UniId, StateCode.UnknownSdkType);
			return false;
		}

		public bool Validate(ReqAccountCusLogin reqCusLogin, NetChannel channel)
		{
			if (Enum.IsDefined(typeof(SdkType), reqCusLogin.SdkType)) return true;
			channel.Write(null, reqCusLogin.UniId, StateCode.UnknownSdkType);
			return false;
		}
	}

	public class DeviceIdValidator : ILoginValidator
	{
		public bool Validate(ReqAccountLogin reqSdkLogin, NetChannel channel)
		{
			if (!string.IsNullOrEmpty(reqSdkLogin.Device)) return true;
			channel.Write(null, reqSdkLogin.UniId, StateCode.DeviceCannotBeNull);
			return false;
		}

		public bool Validate(ReqAccountCusLogin reqCusLogin, NetChannel channel)
		{
			if (!string.IsNullOrEmpty(reqCusLogin.Device)) return true;
			channel.Write(null, reqCusLogin.UniId, StateCode.DeviceCannotBeNull);
			return false;
		}
	}

	public class UserIdValidator : ILoginValidator
	{
		public bool Validate(ReqAccountLogin reqSdkLogin, NetChannel channel)
		{
			if (!string.IsNullOrEmpty(reqSdkLogin.UserId) || reqSdkLogin.Platform == Platform.UNITY) return true;
			channel.Write(null, reqSdkLogin.UniId, StateCode.AccountCannotBeNull);
			return false;
		}

		public bool Validate(ReqAccountCusLogin reqCusLogin, NetChannel channel)
		{
			if (!string.IsNullOrEmpty(reqCusLogin.UserName) || reqCusLogin.Platform == Platform.UNITY) return true;
			channel.Write(null, reqCusLogin.UniId, StateCode.AccountCannotBeNull);
			return false;
		}
	}

	public class LoginValidationChain
	{
		private readonly List<ILoginValidator> _validators = new();

		public LoginValidationChain AddValidator(ILoginValidator validator)
		{
			_validators.Add(validator);
			return this;
		}

		public bool Validate(ReqAccountLogin reqSdkLogin, NetChannel channel)
		{
			return _validators.All(validator => validator.Validate(reqSdkLogin, channel));
		}

		public bool Validate(ReqAccountCusLogin reqCusLogin, NetChannel channel)
		{
			return _validators.All(validator => validator.Validate(reqCusLogin, channel));
		}
	}
}