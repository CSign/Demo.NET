using CSign.Integration.Example.Client;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CSign.Integration.Example.Models
{


    public class LoginViewModel
    {
        public  ScenarioLoginRequest    Request     { get; set; }
        public  ScenarioLoginResponse   Response    { get; set; }

    }


}
