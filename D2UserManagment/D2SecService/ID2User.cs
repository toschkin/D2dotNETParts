using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace D2SecService
{
    [ServiceContract(CallbackContract = typeof(IPasswordChangeEvent))]
    public interface ID2User
    {
        [OperationContract]
        byte[] GetD2UserPassword();

        [OperationContract]
        void SubscribeOnPasswordChange();

        [OperationContract]
        void UnsubscribeFromPasswordChange();
    }

    public interface IPasswordChangeEvent
    {
        [OperationContract(IsOneWay = true)]
        void NotifyPasswordChange(byte[] newPassword);
    }
}