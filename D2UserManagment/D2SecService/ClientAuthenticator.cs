using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Linq;
using System.ServiceModel.Security;
using System.Text;

namespace D2SecService
{
    public class ClientAuthenticator : UserNamePasswordValidator
    {
        public override void Validate(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                throw new SecurityTokenException("Empty or null Username/Password");
            if(!(userName == "D2_User" && password == new string(userName.ToCharArray().Reverse().ToArray())))
                throw new SecurityTokenException("Unknown Username or Incorrect Password");
        }
    }
}
